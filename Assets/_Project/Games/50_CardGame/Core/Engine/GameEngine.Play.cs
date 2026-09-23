using System;
using System.Linq;
using CardGame.Core.Commands;
using CardGame.Core.Definitions;
using CardGame.Core.Events;
using CardGame.Core.State;

namespace CardGame.Core.Engine
{
    /// <summary>カードのプレイ(Docs/50_CardGame/spec/01-rules.md「カードのプレイ」、02「対象選択のルール」)。</summary>
    public sealed partial class GameEngine
    {
        /// <summary>手札のカードがプレイ可能か。可能なら null、不可なら理由。対象の妥当性は含まない。</summary>
        public string? CanPlay(int player, int handIndex)
        {
            var err = ValidateMainPhaseTurn(player);
            if (err != null) return err;
            var ps = State.PlayerOf(player);
            if (handIndex < 0 || handIndex >= ps.Hand.Count) return "手札インデックスが範囲外";
            var def = ps.Hand[handIndex].Definition;
            if (def.Cost > ps.Pp) return $"PP が足りない({def.Cost} > {ps.Pp})";
            if (!def.IsSpell && ps.BoardIsFull) return "場が満杯";
            if (def.IsSpell)
            {
                var kind = def.PlaySelectTarget;
                if (kind.HasValue && ValidSelectTargets(player, kind.Value, null).Count == 0)
                    return "対象がいない";
            }
            return null;
        }

        private string? ValidatePlayCard(PlayCardCommand c)
        {
            var err = CanPlay(c.Player, c.HandIndex);
            if (err != null) return err;

            var def = State.PlayerOf(c.Player).Hand[c.HandIndex].Definition;
            var kind = def.PlaySelectTarget;
            if (kind.HasValue)
            {
                var valid = ValidSelectTargets(c.Player, kind.Value, null);
                if (valid.Count == 0)
                {
                    // フォロワー/アミュレットの Fanfare は対象なしでスキップ(スペルは CanPlay で弾いている)
                    if (c.Target.HasValue) return "対象がいないのに対象が指定されている";
                }
                else
                {
                    if (!c.Target.HasValue) return "対象を選択する必要がある";
                    if (!valid.Contains(c.Target.Value)) return $"不正な対象 {c.Target.Value}";
                }
            }
            else if (c.Target.HasValue)
            {
                return "このカードは対象を取らない";
            }
            return null;
        }

        private void ExecutePlayCard(PlayCardCommand c)
        {
            var ps = State.PlayerOf(c.Player);
            var card = ps.Hand[c.HandIndex];
            var def = card.Definition;
            ps.Hand.RemoveAt(c.HandIndex);
            ps.Pp -= def.Cost;
            Emit(new CardPlayedEvent(c.Player, card.InstanceId, def.Id, def.Type, c.Target));
            Emit(new PpChangedEvent(c.Player, ps.Pp, ps.MaxPp));

            // 対象がいない場合の Fanfare スキップ: Target が null なら Select* アクションは何もしない
            if (def.IsSpell)
            {
                ps.Graveyard.Add(def);
                foreach (var effect in def.EffectsFor(Trigger.Spell))
                    _effectQueue.Enqueue(new PendingEffect(c.Player, card.InstanceId, def, effect, c.Target));
            }
            else
            {
                var entity = new BoardEntity(card.InstanceId, def, c.Player);
                int pos = Math.Min(Math.Max(c.BoardPosition, 0), ps.Board.Count);
                ps.Board.Insert(pos, entity);
                Emit(new EntityEnteredBoardEvent(c.Player, entity.InstanceId, def.Id, pos, entity.Attack, entity.Health, byEffect: false));
                foreach (var effect in def.EffectsFor(Trigger.Fanfare))
                {
                    // 02「対象選択のルール」: Select* を含む Fanfare に対象がいなければ、その効果全体をスキップ
                    bool needsSelect = effect.Actions.Any(a => a.Target.HasValue && a.Target.Value.IsSelect());
                    if (needsSelect && !c.Target.HasValue) continue;
                    _effectQueue.Enqueue(new PendingEffect(c.Player, entity.InstanceId, def, effect, c.Target));
                }
            }

            ResolveEffectQueue();
            CheckLeaderDefeat();
        }
    }
}
