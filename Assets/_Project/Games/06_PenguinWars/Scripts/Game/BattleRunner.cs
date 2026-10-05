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
        [SerializeField] private FieldBackdrop _backdrop;
        [Tooltip("オンライン対戦のステージ。ドラフト後にホストがこの中から抽選する（仕様書 §3.4）")]
        [SerializeField] private PenguinStageData[] _versusStages;
        [Tooltip("ゲストが前回の位置から届いた位置まで動かす秒数。ホストの送信間隔（PenguinWarsOnlineLink）と同じにする")]
        [SerializeField] private float _guestInterpolationTime = 1f / 15f;

        [Header("あそびかたのデモ")]
        [Tooltip("城キラーのデモで HP バーの減り方が見える程度に低くする。ループ（数秒）の間に落ちない値にすること")]
        [SerializeField] private int _demoCastleHp = 1500;
        [Tooltip("ペンギン砲のデモで、出した直後に撃てるようにする")]
        [SerializeField] private float _demoCannonChargeTime = 0.1f;
        [Tooltip("なだれのデモの間隔・予告（秒）。本番（60秒）では待ちきれないので短くする。ループ（GuideTopics）の間に1回起きる値にすること")]
        [SerializeField] private float _demoAvalancheInterval = 3.5f;
        [SerializeField] private float _demoAvalancheWarningTime = 2f;
        [Tooltip("なだれのデモの範囲。タイトル中のカメラは左端しか映さないので、本番（中央）ではなく画面に入る所に起こす")]
        [SerializeField] private Vector2 _demoAvalancheRatio = new Vector2(0.2f, 0.4f);
        [Tooltip("ボスのデモの戦場の長さ。タイトル中のカメラ（だいたい X=-2〜14）に敵の城が入るようにする")]
        [SerializeField] private float _demoBossFieldLength = 14f;
        [Tooltip("ボスのデモで、敵の城HPがこの割合を下回ったらボスが出る。最初の数発で出るよう 1 に近くする")]
        [SerializeField] private float _demoBossCastleRatio = 0.98f;

        private BattleWorld _world;
        // デモ中は World を外に見せない（出撃ボタン・さかな表示がデモの中身を拾わないように）
        private bool _isDemo;
        private Dictionary<int, UnitStats> _demoStatsByNo;
        private float _accumulator;
        private readonly List<BattleEvent> _events = new List<BattleEvent>();
        // ゲストのみ
        private GuestWorldMirror _mirror;
        private ICommandSink _remoteSink;

        public bool IsRunning { get; private set; }
        public bool IsGuest => _mirror != null;
        public int VersusStageCount => _versusStages != null ? _versusStages.Length : 0;
        /// <summary>今の対戦のステージ。一人用のステージ・デモは null</summary>
        public PenguinStageData CurrentStage { get; private set; }
        /// <summary>今の試合の戦場の長さ。カメラ・演出はこれで右の城の位置を知る</summary>
        public float FieldLength { get; private set; }

        /// <summary>UI が状態（さかな・再生産など）を読むためだけに公開する。書き換えは Enqueue 経由で行う</summary>
        public BattleWorld World => _isDemo ? null : _world;
        /// <summary>あそびかたのデモ中か。演出側が「本番の1回きり」の記録をデモで使い切らないために見る</summary>
        public bool IsDemo => _isDemo;

        /// <summary>出撃・ヒット・撃破・城崩壊。演出・音・進行はこれを見て動く</summary>
        public event Action<BattleEvent> EventRaised;

        /// <summary>一人用のステージ: 自分は deckNos の編成、右の城からは定義表どおりに敵が湧く</summary>
        public void InitializeStage(StageDefinition stage, IReadOnlyList<int> deckNos)
        {
            // 能力の当たり方は毎回変える（Battle は UnityEngine.Random を使わないので、ここでシードを決める）
            BattleSettings settings = CreateBaseSettings(false, Environment.TickCount);
            stage.ApplyTo(settings);
            _world = new BattleWorld(settings);
            ApplyField(null, stage.FieldLength, ParseTint(stage));

            Dictionary<int, UnitStats> statsByNo = CollectStatsByNo();
            _world.SetDeck(Side.Left, ToSortedDeck(deckNos, statsByNo));
            WarnMissingUnits(stage, statsByNo);
            // 敵は味方と同じデータから湧く
            _world.SetEnemyScript(new EnemyScriptDirector(stage.Entries, statsByNo));
            RefreshViews();
        }

        /// <summary>オンラインのホスト: ドラフトで決まった編成で城を攻め合う。戦闘はすべてここで計算する（仕様書 §10.2）</summary>
        public void InitializeVersusHost(IReadOnlyList<int> leftDeckNos, IReadOnlyList<int> rightDeckNos, int stageIndex)
        {
            _world = new BattleWorld(CreateSettings(true, Environment.TickCount, GetStage(stageIndex)));
            Dictionary<int, UnitStats> statsByNo = CollectStatsByNo();
            _world.SetDeck(Side.Left, ToSortedDeck(leftDeckNos, statsByNo));
            _world.SetDeck(Side.Right, ToSortedDeck(rightDeckNos, statsByNo));
            RefreshViews();
        }

        /// <summary>
        /// あそびかたのデモ: 両方とも本物の城で、能力は毎回発動する。呼ぶたびに作り直すので、トピックの切り替え・ループの頭で呼ぶ
        /// </summary>
        /// <param name="avalanche">true ならなだれを短い間隔で起こす</param>
        /// <param name="bossUnitNo">0 でなければ、短い戦場で敵の城を叩くとこのキャラがボスとして出る</param>
        public void InitializeDemo(bool avalanche, int bossUnitNo)
        {
            BattleSettings settings = CreateSettings(true, 0, null);
            settings.LeftCastleHp = _demoCastleHp;
            settings.RightCastleHp = _demoCastleHp;
            settings.TimeLimit = 0f;
            settings.CannonChargeTime = _demoCannonChargeTime;
            settings.AlwaysProcAbilities = true;
            if (avalanche)
            {
                settings.AvalancheInterval = _demoAvalancheInterval;
                settings.AvalancheWarningTime = _demoAvalancheWarningTime;
                settings.AvalancheStartRatio = _demoAvalancheRatio.x;
                settings.AvalancheEndRatio = _demoAvalancheRatio.y;
            }

            if (bossUnitNo != 0)
            {
                settings.FieldLength = _demoBossFieldLength;
                ApplyField(null, settings.FieldLength, Color.white);
            }

            _world = new BattleWorld(settings);
            _demoStatsByNo ??= CollectStatsByNo();
            if (bossUnitNo != 0) _world.SetEnemyScript(new EnemyScriptDirector(new[] { CreateDemoBossEntry(bossUnitNo) }, _demoStatsByNo));
            _isDemo = true;
            IsRunning = true;
            RefreshViews();
        }

        private EnemySpawnEntry CreateDemoBossEntry(int unitNo) =>
            new EnemySpawnEntry { UnitNo = unitNo, Count = 1, TriggerCastleHpRatio = _demoBossCastleRatio, IsBoss = true };

        public void SpawnDemoUnit(Side side, int unitNo, float x)
        {
            if (!_isDemo) return;

            if (_demoStatsByNo.TryGetValue(unitNo, out UnitStats stats)) _world.SpawnAt(side, stats, x);
            else Debug.LogWarning($"[BattleRunner] デモの No.{unitNo} がカタログにありません");
        }

        public void FireDemoCannon(Side side)
        {
            if (_isDemo) _world.Enqueue(BattleCommand.FireCannon(side));
        }

        /// <summary>デモのユニットを消して止める。タイトルの後ろにデモの続きが残らないように</summary>
        public void EndDemo()
        {
            if (!_isDemo) return;

            _isDemo = false;
            IsRunning = false;
            _world = null;
            _unitViews.Sync(Array.Empty<UnitState>());
            // デモで減った HP バーを満タンに戻す（試合が始まれば実際の値で上書きされる）
            _leftCastle.SetHp(1, 1);
            _rightCastle.SetHp(1, 1);
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
        public void InitializeGuest(IReadOnlyList<int> myDeckNos, IReadOnlyList<int> opponentDeckNos, int stageIndex,
            ICommandSink remoteSink)
        {
            // 反転（SideMirror）に戦場の長さを使うので、ホストと同じステージで作る
            _world = new BattleWorld(CreateSettings(true, 0, GetStage(stageIndex)));
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
            // デモ中に 1〜5 キーなどで出撃させない
            if (!IsRunning || _isDemo) return;

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
            view.SetHp(castle.Hp, castle.MaxHp);
        }

        private static void WarnMissingUnits(StageDefinition stage, Dictionary<int, UnitStats> statsByNo)
        {
            foreach (EnemySpawnEntry entry in stage.Entries)
            {
                if (!statsByNo.ContainsKey(entry.UnitNo)) Debug.LogWarning($"[BattleRunner] ステージ {stage.Id} の敵 No.{entry.UnitNo} がカタログにありません");
            }
        }

        private void DispatchEvents()
        {
            foreach (BattleEvent battleEvent in _events)
            {
                EventRaised?.Invoke(battleEvent);
            }
        }

        /// <summary>範囲外（ステージ未設定・バージョン違い）なら null を返し、Balance の戦場で遊べるようにする</summary>
        private PenguinStageData GetStage(int stageIndex)
        {
            if (stageIndex >= 0 && stageIndex < VersusStageCount) return _versusStages[stageIndex];

            Debug.LogWarning($"[BattleRunner] ステージ {stageIndex} がありません。Tools > MiniGame > Rebuild PenguinWars を実行してください");
            return null;
        }

        /// <param name="stage">null なら Balance の戦場（デモ）</param>
        private BattleSettings CreateSettings(bool versus, int seed, PenguinStageData stage)
        {
            BattleSettings settings = CreateBaseSettings(versus, seed);
            if (stage != null) stage.ApplyTo(settings);
            ApplyField(stage, settings.FieldLength, stage != null ? stage.Tint : Color.white);
            return settings;
        }

        /// <summary>シーンは1つの戦場で作ってあるので、ステージの長さに合わせて右の城を動かし、色を変える</summary>
        /// <param name="versusStage">一人用のステージ・デモは null</param>
        private void ApplyField(PenguinStageData versusStage, float fieldLength, Color tint)
        {
            CurrentStage = versusStage;
            FieldLength = fieldLength;
            Vector3 position = _rightCastle.transform.localPosition;
            _rightCastle.transform.localPosition = new Vector3(fieldLength, position.y, position.z);
            _backdrop.SetTint(tint);
        }

        /// <summary>定義表の色は文字列（Battle は Color を持てないため）。書き間違いは白にして、遊べなくはしない</summary>
        private static Color ParseTint(StageDefinition stage)
        {
            if (string.IsNullOrEmpty(stage.TintHex)) return Color.white;
            if (ColorUtility.TryParseHtmlString(stage.TintHex, out Color tint)) return tint;

            Debug.LogWarning($"[BattleRunner] ステージ {stage.Id} の色 {stage.TintHex} が読めません（\"#RRGGBB\" で書く）");
            return Color.white;
        }

        private BattleSettings CreateBaseSettings(bool versus, int seed)
        {
            // 城HP はステージの定義表・対戦のステージで上書きする（デモもその後で上書きする）
            return _balance.CreateBattleSettings(versus, seed);
        }

        /// <summary>
        /// ドラフト・ステージの編成は選んだ順に並ぶので、安い順に並べ直す。
        /// ゲストにはこの並びのまま編成が送られるので、並べ替えはホストだけでよい
        /// </summary>
        private static List<UnitStats> ToSortedDeck(IReadOnlyList<int> unitNos, Dictionary<int, UnitStats> statsByNo)
        {
            List<UnitStats> deck = ToDeck(unitNos, statsByNo);
            DeckRandomizer.SortByCost(deck);
            return deck;
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
                Debug.LogWarning("[BattleRunner] カタログが空です。Tools > MiniGame > Rebuild PenguinWars を実行してください");
            }
            return units;
        }
    }
}
