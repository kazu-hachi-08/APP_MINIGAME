using System;
using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// BattleWorld を固定ステップで進め、状態を View に流す橋渡し役。
    /// 固定ステップにするのは、フレームレートが違う端末でも同じ結果にするため（オンラインのずれ・テストとの差を防ぐ）。
    /// オンラインのゲストは World を進めず、ホストから届いた状態とイベントを流す（InitializeGuest 以降）
    /// </summary>
    public class BattleRunner : MonoBehaviour
    {
        private const float FixedStep = 1f / 30f;
        // 重い端末でフレームが詰まったとき、追いつこうとして更に重くなるのを防ぐ
        private const int MaxStepsPerFrame = 5;

        [SerializeField] private PenguinWarsBalance _balance;
        [SerializeField] private PenguinUnitCatalog _catalog;
        [SerializeField] private UnitViewPool _unitViews;
        [SerializeField] private CastleView _leftCastle;
        [SerializeField] private CastleView _rightCastle;
        [Tooltip("ゲストが前回の位置から届いた位置まで動かす秒数。ホストの送信間隔（PenguinWarsOnlineLink）と同じにする")]
        [SerializeField] private float _guestInterpolationTime = 1f / 15f;

        private BattleWorld _world;
        private float _accumulator;
        private readonly List<BattleEvent> _events = new List<BattleEvent>();
        // ゲストのみ
        private GuestWorldMirror _mirror;
        private ICommandSink _remoteSink;

        public bool IsRunning { get; private set; }
        public bool IsGuest => _mirror != null;

        /// <summary>UI が状態（さかな・再生産など）を読むためだけに公開する。書き換えは Enqueue 経由で行う</summary>
        public BattleWorld World => _world;

        /// <summary>出撃・ヒット・撃破・城崩壊。演出・音・進行はこれを見て動く</summary>
        public event Action<BattleEvent> EventRaised;

        /// <summary>エンドレス: 自分はランダム10体、右は無敵の出現ゲートから敵が湧く（仕様書 §2.1）</summary>
        public void InitializeEndless()
        {
            // 毎回違う編成・違う湧き方・違う能力の当たり方にする（Battle は UnityEngine.Random を使わないので、ここでシードを決める）
            var random = new System.Random(Environment.TickCount);
            _world = new BattleWorld(CreateSettings(false, random.Next()));

            List<UnitStats> allUnits = CollectAllUnits();
            _world.SetDeck(Side.Left, PickRandomDeck(allUnits, random));
            // 敵は味方と同じデータから湧く（仕様書 §8.1）
            _world.SetEnemyWaves(new EnemyWaveDirector(_balance.CreateEnemyWaveSettings(), allUnits, random.Next()));
            RefreshViews();
        }

        /// <summary>オンラインのホスト: ドラフトで決まった編成で城を攻め合う。戦闘はすべてここで計算する（仕様書 §10.2）</summary>
        public void InitializeVersusHost(IReadOnlyList<int> leftDeckNos, IReadOnlyList<int> rightDeckNos)
        {
            _world = new BattleWorld(CreateSettings(true, Environment.TickCount));
            Dictionary<int, UnitStats> statsByNo = CollectStatsByNo();
            _world.SetDeck(Side.Left, ToDeck(leftDeckNos, statsByNo));
            _world.SetDeck(Side.Right, ToDeck(rightDeckNos, statsByNo));
            RefreshViews();
        }

        /// <summary>ドラフトの候補にする全キャラの No</summary>
        public List<int> CollectAllUnitNos()
        {
            var unitNos = new List<int>();
            foreach (UnitStats stats in CollectAllUnits()) unitNos.Add(stats.UnitNo);
            return unitNos;
        }

        /// <summary>
        /// オンラインのゲスト: 表示用の World を作るだけで進めない。編成はホストが決めて送ってくる。
        /// 左右反転後の向きで持つので、自分の編成が Side.Left になる
        /// </summary>
        public void InitializeGuest(IReadOnlyList<int> myDeckNos, IReadOnlyList<int> opponentDeckNos, ICommandSink remoteSink)
        {
            _world = new BattleWorld(CreateSettings(true, 0));
            Dictionary<int, UnitStats> statsByNo = CollectStatsByNo();
            _world.SetDeck(Side.Left, ToDeck(myDeckNos, statsByNo));
            _world.SetDeck(Side.Right, ToDeck(opponentDeckNos, statsByNo));
            _mirror = new GuestWorldMirror(_world, no => statsByNo.TryGetValue(no, out UnitStats stats) ? stats : null);
            _remoteSink = remoteSink;
            RefreshViews();
        }

        /// <summary>Playing 中だけ進める。ポーズ中に押したキーで出撃が溜まらないよう、止めている間は操作も受け付けない</summary>
        public void SetRunning(bool running)
        {
            IsRunning = running && !_world.IsFinished;
        }

        public void Enqueue(BattleCommand command)
        {
            if (!IsRunning) return;

            if (IsGuest) _remoteSink.Submit(command);
            else _world.Enqueue(command);
        }

        /// <summary>ゲストのみ。ホストから届いた最新の状態</summary>
        public void ApplyRemoteSnapshot(BattleSnapshot snapshot)
        {
            _mirror.Apply(snapshot);
        }

        /// <summary>ゲストのみ。ホストで起きたイベント（ホスト基準の向き）。次の Update で演出に流す</summary>
        public void PushRemoteEvent(BattleEvent battleEvent)
        {
            _events.Add(_mirror.ToLocal(battleEvent));
        }

        private void Update()
        {
            if (IsGuest)
            {
                UpdateGuest();
                return;
            }
            if (!IsRunning) return;

            StepFixed(Time.deltaTime);
            RefreshViews();
            _events.Clear();
            _world.DrainEvents(_events);
            DispatchEvents();
            if (_world.IsFinished) IsRunning = false;
        }

        /// <summary>
        /// ゲストは IsRunning に関係なく表示を続ける。編成発表の長さが通信の遅れぶんホストとずれるので、
        /// 止めているとホストが先に始めた分の出撃やヒットを取りこぼすため
        /// </summary>
        private void UpdateGuest()
        {
            _mirror.Advance(Time.deltaTime, _guestInterpolationTime);
            RefreshViews();
            DispatchEvents();
            _events.Clear();
        }

        private void StepFixed(float deltaTime)
        {
            _accumulator += deltaTime;
            int steps = 0;
            while (_accumulator >= FixedStep && steps < MaxStepsPerFrame)
            {
                _world.Step(FixedStep);
                _accumulator -= FixedStep;
                steps++;
            }
            // 追いつけなかった分は捨てる（一瞬ゆっくりになるだけで、次のフレームにまとめて進まないように）
            _accumulator = Mathf.Min(_accumulator, FixedStep);
        }

        private void RefreshViews()
        {
            _unitViews.Sync(_world.Units);
            ApplyCastle(_leftCastle, _world.GetCastle(Side.Left));
            ApplyCastle(_rightCastle, _world.GetCastle(Side.Right));
        }

        private static void ApplyCastle(CastleView view, CastleState castle)
        {
            // 無敵の出現ゲートは HP を持たないものとして見せる（仕様書 §2.1）
            if (castle.IsInvincible) view.ShowAsGate();
            else view.SetHp(castle.Hp, castle.MaxHp);
        }

        private void DispatchEvents()
        {
            foreach (BattleEvent battleEvent in _events)
            {
                EventRaised?.Invoke(battleEvent);
            }
        }

        private BattleSettings CreateSettings(bool versus, int seed)
        {
            int castleHp = versus ? _balance.CastleHpVersus : _balance.CastleHpEndless;
            return new BattleSettings
            {
                FieldLength = _balance.FieldLength,
                LeftCastleHp = castleHp,
                RightCastleHp = castleHp,
                // エンドレスの右端は無敵の出現ゲート、敵はお金を持たない（仕様書 §2.1・§8）。対戦は両者が同じルール
                RightCastleInvincible = !versus,
                RightSpawnsFree = !versus,
                TimeLimit = versus ? _balance.VersusTimeLimit : 0f,
                SpawnOffset = _balance.SpawnOffset,
                MaxUnitsPerSide = _balance.MaxUnitsPerSide,
                WalletTable = _balance.CreateWalletTable(),
                KillRewardRate = _balance.KillRewardRate,
                CannonChargeTime = _balance.CannonChargeTime,
                CannonRangeRatio = _balance.CannonRangeRatio,
                CannonDamage = _balance.CannonDamage,
                KnockbackDistance = _balance.KnockbackDistance,
                KnockbackDuration = _balance.KnockbackDuration,
                SlowSpeedMultiplier = _balance.SlowSpeedMultiplier,
                CastleKillerMultiplier = _balance.CastleKillerMultiplier,
                RandomSeed = seed,
            };
        }

        private List<UnitStats> PickRandomDeck(List<UnitStats> allUnits, System.Random random)
        {
            return DeckRandomizer.PickDeck(allUnits, _balance.DeckSize, _balance.DeckMinWalls, random);
        }

        private static List<UnitStats> ToDeck(IReadOnlyList<int> unitNos, Dictionary<int, UnitStats> statsByNo)
        {
            var deck = new List<UnitStats>();
            foreach (int no in unitNos)
            {
                if (statsByNo.TryGetValue(no, out UnitStats stats)) deck.Add(stats);
                else Debug.LogWarning($"[BattleRunner] 編成の No.{no} がこちらのカタログにありません（ビルドのバージョン違い？）");
            }
            return deck;
        }

        private Dictionary<int, UnitStats> CollectStatsByNo()
        {
            var statsByNo = new Dictionary<int, UnitStats>();
            foreach (UnitStats stats in CollectAllUnits()) statsByNo[stats.UnitNo] = stats;
            return statsByNo;
        }

        private List<UnitStats> CollectAllUnits()
        {
            var units = new List<UnitStats>();
            foreach (PenguinUnitData data in _catalog.Units)
            {
                if (data != null) units.Add(data.ToStats());
            }
            if (units.Count == 0)
            {
                Debug.LogWarning("[BattleRunner] カタログが空です。Tools > MiniGame > Generate PenguinWars Units を実行してください");
            }
            return units;
        }
    }
}
