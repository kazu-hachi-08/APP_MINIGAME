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
        [Tooltip("仮の編成（キャラNo。並び順が 1〜5 キーに対応）。敵も同じ編成から湧く。Phase 5 でランダム編成に置き換える")]
        [SerializeField] private int[] _deckUnitNos = { 1, 11, 23 };
        [Tooltip("仮の敵の湧き間隔（秒）。Phase 4 で敵レベルに置き換える")]
        [SerializeField] private float _enemySpawnInterval = 4f;

        private BattleWorld _world;
        private SimpleEnemySpawner _enemySpawner;
        private float _accumulator;
        private readonly List<BattleEvent> _events = new List<BattleEvent>();

        public bool IsRunning { get; private set; }

        /// <summary>出撃・ヒット・撃破・城崩壊。演出・音・進行はこれを見て動く</summary>
        public event Action<BattleEvent> EventRaised;

        public void Initialize()
        {
            _world = new BattleWorld(new BattleSettings
            {
                FieldLength = _balance.FieldLength,
                LeftCastleHp = _balance.CastleHpEndless,
                RightCastleHp = _balance.CastleHpEndless,
                RightCastleInvincible = true,
                SpawnOffset = _balance.SpawnOffset,
                MaxUnitsPerSide = _balance.MaxUnitsPerSide,
            });

            List<UnitStats> deck = BuildDeck();
            _world.SetDeck(Side.Left, deck);
            _world.SetDeck(Side.Right, deck);
            _enemySpawner = new SimpleEnemySpawner(_enemySpawnInterval, deck.Count, Environment.TickCount);
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
                _enemySpawner.Tick(_world, FixedStep);
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
            if (castle.IsInvincible) view.HideHp();
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

        private List<UnitStats> BuildDeck()
        {
            var deck = new List<UnitStats>();
            foreach (int no in _deckUnitNos)
            {
                PenguinUnitData data = _catalog.Get(no);
                if (data == null)
                {
                    Debug.LogWarning($"[BattleRunner] キャラNo {no} がカタログにありません。Tools > MiniGame > Generate PenguinWars Units を実行してください");
                    continue;
                }
                deck.Add(data.ToStats());
            }
            return deck;
        }
    }
}
