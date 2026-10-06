#nullable enable
using System.Collections.Generic;
using CardGame.Core.Definitions;
using CardGame.Core.Events;
using CardGame.Core.State;

namespace CardGame.Unity.Battle
{
    /// <summary>GameEvent を人間向けの日本語ログ 1 行にする。</summary>
    public sealed class EventFormatter
    {
        private readonly CardDatabase _db;
        private readonly Dictionary<int, string> _names = new();
        /// <summary>視点プレイヤー(「あなた」と表示する側)。</summary>
        public int Viewer { get; set; }

        public EventFormatter(CardDatabase db) { _db = db; }

        private string Who(int player) => player == Viewer ? "あなた" : "相手";

        private string Name(int instanceId) => _names.TryGetValue(instanceId, out var n) ? n : $"#{instanceId}";

        private string Target(TargetRef t) => t.IsLeader ? $"{Who(t.PlayerIndex)}のリーダー" : Name(t.InstanceId);

        public string? Format(GameEvent e)
        {
            switch (e)
            {
                case CardDrawnEvent d:
                    _names[d.InstanceId] = _db.Get(d.CardId).Name;
                    return d.Player == Viewer ? $"{Name(d.InstanceId)} を引いた" : "相手がカードを引いた";
                case CardBurnedEvent b: return $"{Who(b.Player)}: 手札上限で {_db.Get(b.CardId).Name} が消滅";
                case DeckOutEvent o: return $"{Who(o.Player)}のデッキが尽きた";
                case TurnStartedEvent t: return $"── {Who(t.Player)}のターン (PP {t.MaxPp}) ──";
                case BonusPpUsedEvent bp: return $"<color=#F8C040>覚醒の刻</color>: {Who(bp.Player)}の PP +1 (PP {bp.Pp})";  // 演出は BattleScreen.AwakeningEffect
                case CardPlayedEvent p:
                    _names[p.InstanceId] = _db.Get(p.CardId).Name;
                    return $"{Who(p.Player)}が {Name(p.InstanceId)} をプレイ" + (p.Target.HasValue ? $" → {Target(p.Target.Value)}" : "");
                case EntityEnteredBoardEvent en:
                    _names[en.InstanceId] = _db.Get(en.CardId).Name;
                    return en.ByEffect ? $"{Name(en.InstanceId)} が場に出た" : null;
                case EffectTriggeredEvent fx:
                    return fx.Trigger switch
                    {
                        Trigger.LastWords => $"{Name(fx.SourceInstanceId)} のラストワード",
                        Trigger.TurnStart => $"{Name(fx.SourceInstanceId)} のターン開始時効果",
                        _ => null,
                    };
                case AttackDeclaredEvent a: return $"{Name(a.AttackerInstanceId)} が {Target(a.Target)} に攻撃";
                case DamageDealtEvent dm: return $"{Target(dm.Target)} に {dm.Amount} ダメージ(残り {dm.RemainingHealth})";
                case HealedEvent h: return $"{Target(h.Target)} を {h.Amount} 回復";
                case BuffedEvent bf: return $"{Name(bf.InstanceId)} {(bf.AttackDelta >= 0 ? "+" : "")}{bf.AttackDelta}/{(bf.HealthDelta >= 0 ? "+" : "")}{bf.HealthDelta}";
                case CountdownChangedEvent c: return $"{Name(c.InstanceId)} カウントダウン {c.Countdown}";
                case EntityDestroyedEvent x: return $"{Name(x.InstanceId)} が破壊された";
                case MulliganCompletedEvent m: return $"{Who(m.Player)}がマリガン完了({m.ReplacedCount} 枚交換)";
                case GameEndedEvent g:
                    return g.Winner.HasValue ? $"{Who(g.Winner.Value)}の勝利!" : "引き分け";
                default: return null;
            }
        }
    }
}
