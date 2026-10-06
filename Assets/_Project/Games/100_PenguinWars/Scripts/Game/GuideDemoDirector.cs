using System;
using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// あそびかたのデモ係。GuideTopic の台本どおりに後ろの戦場（BattleRunner）へユニットを出し、LoopSeconds ごとに最初からやり直す。
    /// 戦闘・演出は本番と同じ BattleWorld・View がそのまま動くので、ここは「いつ何を出すか」とデモ用の数値だけを持つ。
    /// デモは両方とも本物の城で、能力は毎回発動する（見せたい能力が確率で出ないことがないように）
    /// </summary>
    public class GuideDemoDirector : MonoBehaviour
    {
        [SerializeField] private BattleRunner _battleRunner;

        [Header("デモの数値")]
        [Tooltip("城キラーのデモで HP バーの減り方が見える程度に低くする。ループ（数秒）の間に落ちない値にすること")]
        [SerializeField] private int _castleHp = 1500;
        [Tooltip("ペンギン砲のデモで、出した直後に撃てるようにする")]
        [SerializeField] private float _cannonChargeTime = 0.1f;
        [Tooltip("なだれのデモの間隔・予告（秒）。本番（60秒）では待ちきれないので短くする。ループ（GuideTopics）の間に1回起きる値にすること")]
        [SerializeField] private float _avalancheInterval = 3.5f;
        [SerializeField] private float _avalancheWarningTime = 2f;
        [Tooltip("なだれのデモの範囲。タイトル中のカメラは左端しか映さないので、本番（中央）ではなく画面に入る所に起こす")]
        [SerializeField] private Vector2 _avalancheRatio = new Vector2(0.2f, 0.4f);
        [Tooltip("ボスのデモの戦場の長さ。タイトル中のカメラ（だいたい X=-2〜14）に敵の城が入るようにする")]
        [SerializeField] private float _bossFieldLength = 14f;
        [Tooltip("ボスのデモで、敵の城HPがこの割合を下回ったらボスが出る。最初の数発で出るよう 1 に近くする")]
        [SerializeField] private float _bossCastleRatio = 0.98f;

        private GuideTopic _topic;
        private float _time;
        private int _nextAction;

        public void Play(GuideTopic topic)
        {
            _topic = topic;
            Restart();
        }

        public void Stop()
        {
            _topic = null;
            _battleRunner.EndDemo();
        }

        private void Update()
        {
            if (_topic == null) return;

            _time += Time.deltaTime;
            if (_time >= _topic.LoopSeconds)
            {
                Restart();
                return;
            }
            RunDueActions();
        }

        private void Restart()
        {
            _time = 0f;
            _nextAction = 0;
            _battleRunner.InitializeDemo(CustomizeSettings, CreateEnemies());
            // 0 秒目の出撃を、作り直したその場で出す（1フレーム空の戦場が見えないように）
            RunDueActions();
        }

        private void CustomizeSettings(BattleSettings settings)
        {
            settings.LeftCastleHp = _castleHp;
            settings.RightCastleHp = _castleHp;
            // ループで作り直すので、時間切れで止まらないようにする
            settings.TimeLimit = 0f;
            settings.CannonChargeTime = _cannonChargeTime;
            settings.AlwaysProcAbilities = true;
            if (_topic.ShowsAvalanche) ApplyAvalanche(settings);
            // 敵の城（ボスの出てくる所）がタイトル中のカメラに入るように縮める
            if (_topic.BossUnitNo != 0) settings.FieldLength = _bossFieldLength;
        }

        private void ApplyAvalanche(BattleSettings settings)
        {
            settings.AvalancheInterval = _avalancheInterval;
            settings.AvalancheWarningTime = _avalancheWarningTime;
            settings.AvalancheStartRatio = _avalancheRatio.x;
            settings.AvalancheEndRatio = _avalancheRatio.y;
        }

        /// <summary>ボスのデモだけ、敵の城を叩くとボスが1体出る台本にする</summary>
        private IReadOnlyList<EnemySpawnEntry> CreateEnemies()
        {
            if (_topic.BossUnitNo == 0) return Array.Empty<EnemySpawnEntry>();

            return new[]
            {
                new EnemySpawnEntry { UnitNo = _topic.BossUnitNo, Count = 1, TriggerCastleHpRatio = _bossCastleRatio, IsBoss = true },
            };
        }

        private void RunDueActions()
        {
            while (_nextAction < _topic.Actions.Count && _topic.Actions[_nextAction].Time <= _time)
            {
                Run(_topic.Actions[_nextAction]);
                _nextAction++;
            }
        }

        private void Run(GuideAction action)
        {
            if (action.IsCannon) _battleRunner.FireDemoCannon(action.Side);
            else _battleRunner.SpawnDemoUnit(action.Side, action.UnitNo, action.X);
        }
    }
}
