using System;
using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// BattleWorld を固定ステップで進め、状態を View に流す橋渡し役。
    /// 固定ステップにするのは、フレームレートが違う端末でも同じ結果にするため（オンラインのずれ・テストとの差を防ぐ）
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

        private BattleWorld _world;
        private float _accumulator;
        private readonly List<BattleEvent> _events = new List<BattleEvent>();

        public bool IsRunning { get; private set; }

        /// <summary>UI が状態（さかな・再生産など）を読むためだけに公開する。書き換えは Enqueue 経由で行う</summary>
        public BattleWorld World => _world;

        /// <summary>出撃・ヒット・撃破・城崩壊。演出・音・進行はこれを見て動く</summary>
        public event Action<BattleEvent> EventRaised;

        public void Initialize()
        {
            // 毎回違う編成・違う湧き方・違う能力の当たり方にする（Battle は UnityEngine.Random を使わないので、ここでシードを決める）
            var random = new System.Random(Environment.TickCount);
            _world = new BattleWorld(new BattleSettings
            {
                FieldLength = _balance.FieldLength,
                LeftCastleHp = _balance.CastleHpEndless,
                RightCastleHp = _balance.CastleHpEndless,
                RightCastleInvincible = true,
                SpawnOffset = _balance.SpawnOffset,
                MaxUnitsPerSide = _balance.MaxUnitsPerSide,
                WalletTable = _balance.CreateWalletTable(),
                // エンドレスの敵はお金を持たない（仕様書 §8）
                RightSpawnsFree = true,
                KillRewardRate = _balance.KillRewardRate,
                CannonChargeTime = _balance.CannonChargeTime,
                CannonRangeRatio = _balance.CannonRangeRatio,
                CannonDamage = _balance.CannonDamage,
                KnockbackDistance = _balance.KnockbackDistance,
                KnockbackDuration = _balance.KnockbackDuration,
                SlowSpeedMultiplier = _balance.SlowSpeedMultiplier,
                CastleKillerMultiplier = _balance.CastleKillerMultiplier,
                RandomSeed = random.Next(),
            });

            List<UnitStats> allUnits = CollectAllUnits();
            _world.SetDeck(Side.Left, DeckRandomizer.PickDeck(allUnits, _balance.DeckSize, _balance.DeckMinWalls, random));
            // 敵は味方と同じデータから湧く（仕様書 §8.1）
            _world.SetEnemyWaves(new EnemyWaveDirector(_balance.CreateEnemyWaveSettings(), allUnits, random.Next()));
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

            _world.Enqueue(command);
        }

        private void Update()
        {
            if (!IsRunning) return;

            StepFixed(Time.deltaTime);
            RefreshViews();
            DispatchEvents();
            if (_world.IsFinished) IsRunning = false;
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
            _events.Clear();
            _world.DrainEvents(_events);
            foreach (BattleEvent battleEvent in _events)
            {
                EventRaised?.Invoke(battleEvent);
            }
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
