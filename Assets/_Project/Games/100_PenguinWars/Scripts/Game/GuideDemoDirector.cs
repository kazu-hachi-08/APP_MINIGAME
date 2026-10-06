using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// あそびかたのデモ係。GuideTopic の台本どおりに後ろの戦場（BattleRunner）へユニットを出し、LoopSeconds ごとに最初からやり直す。
    /// 戦闘・演出は本番と同じ BattleWorld・View がそのまま動くので、ここは「いつ何を出すか」だけを持つ
    /// </summary>
    public class GuideDemoDirector : MonoBehaviour
    {
        [SerializeField] private BattleRunner _battleRunner;

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
            _battleRunner.InitializeDemo(_topic.ShowsAvalanche, _topic.BossUnitNo);
            // 0 秒目の出撃を、作り直したその場で出す（1フレーム空の戦場が見えないように）
            RunDueActions();
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
