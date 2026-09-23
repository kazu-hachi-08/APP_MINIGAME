using CardGame.Core.Definitions;
using CardGame.Core.State;

namespace CardGame.Core.Events
{
    /// <summary>
    /// Core が状態変化を通知するイベント。Unity 側はこれを見て演出する(docs/spec/05-online.md)。
    /// 各イベントは発生時点の情報を保持し、状態オブジェクトへの参照は持たない(後から読んでも意味が変わらない)。
    /// </summary>
    public abstract class GameEvent
    {
    }

    public sealed class GameStartedEvent : GameEvent
    {
        public int FirstPlayer { get; }
        public GameStartedEvent(int firstPlayer) { FirstPlayer = firstPlayer; }
        public override string ToString() => $"ゲーム開始 先攻=P{FirstPlayer}";
    }

    public sealed class MulliganCompletedEvent : GameEvent
    {
        public int Player { get; }
        public int ReplacedCount { get; }
        public MulliganCompletedEvent(int player, int replacedCount) { Player = player; ReplacedCount = replacedCount; }
        public override string ToString() => $"P{Player} マリガン完了({ReplacedCount}枚交換)";
    }

    public sealed class TurnStartedEvent : GameEvent
    {
        public int Player { get; }
        public int TurnNumber { get; }
        public int MaxPp { get; }
        public TurnStartedEvent(int player, int turnNumber, int maxPp) { Player = player; TurnNumber = turnNumber; MaxPp = maxPp; }
        public override string ToString() => $"--- ターン{TurnNumber} P{Player} (PP {MaxPp}) ---";
    }

    public sealed class TurnEndedEvent : GameEvent
    {
        public int Player { get; }
        public TurnEndedEvent(int player) { Player = player; }
        public override string ToString() => $"P{Player} ターン終了";
    }

    public sealed class CardDrawnEvent : GameEvent
    {
        public int Player { get; }
        public int InstanceId { get; }
        public string CardId { get; }
        public CardDrawnEvent(int player, int instanceId, string cardId) { Player = player; InstanceId = instanceId; CardId = cardId; }
        public override string ToString() => $"P{Player} ドロー {CardId}(#{InstanceId})";
    }

    /// <summary>手札上限で引いたカードが消滅した。</summary>
    public sealed class CardBurnedEvent : GameEvent
    {
        public int Player { get; }
        public string CardId { get; }
        public CardBurnedEvent(int player, string cardId) { Player = player; CardId = cardId; }
        public override string ToString() => $"P{Player} 手札上限で {CardId} が消滅";
    }

    /// <summary>デッキ切れでドローできなかった。</summary>
    public sealed class DeckOutEvent : GameEvent
    {
        public int Player { get; }
        public DeckOutEvent(int player) { Player = player; }
        public override string ToString() => $"P{Player} デッキ切れ";
    }

    public sealed class PpChangedEvent : GameEvent
    {
        public int Player { get; }
        public int Pp { get; }
        public int MaxPp { get; }
        public PpChangedEvent(int player, int pp, int maxPp) { Player = player; Pp = pp; MaxPp = maxPp; }
        public override string ToString() => $"P{Player} PP {Pp}/{MaxPp}";
    }

    /// <summary>後攻の追加 PP(覚醒の刻)を使った。01-rules.md「後攻の追加 PP」。</summary>
    public sealed class BonusPpUsedEvent : GameEvent
    {
        public int Player { get; }
        public int Pp { get; }
        public BonusPpUsedEvent(int player, int pp) { Player = player; Pp = pp; }
        public override string ToString() => $"P{Player} 覚醒の刻 (PP {Pp})";
    }

    public sealed class CardPlayedEvent : GameEvent
    {
        public int Player { get; }
        public int InstanceId { get; }
        public string CardId { get; }
        public CardType Type { get; }
        public TargetRef? Target { get; }
        public CardPlayedEvent(int player, int instanceId, string cardId, CardType type, TargetRef? target)
        { Player = player; InstanceId = instanceId; CardId = cardId; Type = type; Target = target; }
        public override string ToString() => $"P{Player} プレイ {CardId}(#{InstanceId})" + (Target.HasValue ? $" -> {Target}" : "");
    }

    /// <summary>フォロワー/アミュレットが場に出た(プレイ・召喚の両方)。</summary>
    public sealed class EntityEnteredBoardEvent : GameEvent
    {
        public int Player { get; }
        public int InstanceId { get; }
        public string CardId { get; }
        public int Position { get; }
        public int Attack { get; }
        public int Health { get; }
        public bool ByEffect { get; }
        public EntityEnteredBoardEvent(int player, int instanceId, string cardId, int position, int attack, int health, bool byEffect)
        { Player = player; InstanceId = instanceId; CardId = cardId; Position = position; Attack = attack; Health = health; ByEffect = byEffect; }
        public override string ToString() => $"P{Player} 場に出る {CardId}(#{InstanceId}) pos={Position}" + (ByEffect ? " [効果]" : "");
    }

    /// <summary>効果が発動した(Fanfare / LastWords / Spell)。</summary>
    public sealed class EffectTriggeredEvent : GameEvent
    {
        public int Player { get; }
        public int SourceInstanceId { get; }
        public string SourceCardId { get; }
        public Trigger Trigger { get; }
        public EffectTriggeredEvent(int player, int sourceInstanceId, string sourceCardId, Trigger trigger)
        { Player = player; SourceInstanceId = sourceInstanceId; SourceCardId = sourceCardId; Trigger = trigger; }
        public override string ToString() => $"P{Player} {Trigger}: {SourceCardId}(#{SourceInstanceId})";
    }

    public sealed class AttackDeclaredEvent : GameEvent
    {
        public int Player { get; }
        public int AttackerInstanceId { get; }
        public TargetRef Target { get; }
        public AttackDeclaredEvent(int player, int attackerInstanceId, TargetRef target) { Player = player; AttackerInstanceId = attackerInstanceId; Target = target; }
        public override string ToString() => $"P{Player} 攻撃 #{AttackerInstanceId} -> {Target}";
    }

    public sealed class DamageDealtEvent : GameEvent
    {
        public TargetRef Target { get; }
        public int Amount { get; }
        /// <summary>ダメージ源(攻撃者または効果のカード)。不明なら null。</summary>
        public int? SourceInstanceId { get; }
        /// <summary>ダメージ後の体力/HP。</summary>
        public int RemainingHealth { get; }
        public DamageDealtEvent(TargetRef target, int amount, int? sourceInstanceId, int remainingHealth)
        { Target = target; Amount = amount; SourceInstanceId = sourceInstanceId; RemainingHealth = remainingHealth; }
        public override string ToString() => $"{Target} に {Amount} ダメージ (残 {RemainingHealth})";
    }

    public sealed class HealedEvent : GameEvent
    {
        public TargetRef Target { get; }
        public int Amount { get; }
        public int ResultHealth { get; }
        public HealedEvent(TargetRef target, int amount, int resultHealth) { Target = target; Amount = amount; ResultHealth = resultHealth; }
        public override string ToString() => $"{Target} を {Amount} 回復 (→ {ResultHealth})";
    }

    public sealed class BuffedEvent : GameEvent
    {
        public int InstanceId { get; }
        public int AttackDelta { get; }
        public int HealthDelta { get; }
        public int Attack { get; }
        public int Health { get; }
        public BuffedEvent(int instanceId, int attackDelta, int healthDelta, int attack, int health)
        { InstanceId = instanceId; AttackDelta = attackDelta; HealthDelta = healthDelta; Attack = attack; Health = health; }
        public override string ToString() => $"#{InstanceId} {AttackDelta:+#;-#;+0}/{HealthDelta:+#;-#;+0} → {Attack}/{Health}";
    }

    public sealed class CountdownChangedEvent : GameEvent
    {
        public int InstanceId { get; }
        public int Countdown { get; }
        public CountdownChangedEvent(int instanceId, int countdown) { InstanceId = instanceId; Countdown = countdown; }
        public override string ToString() => $"#{InstanceId} カウントダウン {Countdown}";
    }

    public sealed class EntityDestroyedEvent : GameEvent
    {
        public int Player { get; }
        public int InstanceId { get; }
        public string CardId { get; }
        public EntityDestroyedEvent(int player, int instanceId, string cardId) { Player = player; InstanceId = instanceId; CardId = cardId; }
        public override string ToString() => $"P{Player} {CardId}(#{InstanceId}) 破壊";
    }

    public sealed class GameEndedEvent : GameEvent
    {
        /// <summary>勝者。引き分けなら null。</summary>
        public int? Winner { get; }
        public GameEndReason Reason { get; }
        public GameEndedEvent(int? winner, GameEndReason reason) { Winner = winner; Reason = reason; }
        public override string ToString() => Winner.HasValue ? $"=== P{Winner} の勝利 ({Reason}) ===" : $"=== 引き分け ({Reason}) ===";
    }

    public enum GameEndReason
    {
        LeaderDefeated,
        DeckOut,
        Surrender,
        Draw,
    }
}
