using System.Linq;
using CardGame.Core.Commands;
using CardGame.Core.Definitions;
using CardGame.Core.Events;
using CardGame.Core.State;

namespace CardGame.Core.Engine
{
    /// <summary>開始・マリガン・ターン進行・ドロー(docs/spec/01-rules.md「ゲームの流れ」)。</summary>
    public sealed partial class GameEngine
    {
        private void SetupGame(DeckDefinition deck0, DeckDefinition deck1, int? firstPlayer)
        {
            // 先攻決定(シードから)
            int first = firstPlayer ?? State.Rng.Next(2);
            State.CurrentPlayer = first;

            var decks = new[] { deck0, deck1 };
            for (int p = 0; p < 2; p++)
            {
                var ps = State.Players[p];
                foreach (var id in decks[p].CardIds)
                    ps.Deck.Add(State.CreateCard(Db.Get(id)));
                State.Rng.Shuffle(ps.Deck);
            }

            Emit(new GameStartedEvent(first));

            // 初期手札(先攻 3 / 後攻 4)。マリガン前なので CardDrawn イベントは出さない
            DrawInitial(State.Players[first], GameRules.FirstPlayerInitialHand);
            DrawInitial(State.Players[1 - first], GameRules.SecondPlayerInitialHand);
            // 後攻には 1 試合 1 回の追加 PP(覚醒の刻)を与える
            State.Players[1 - first].BonusPpLeft = GameRules.SecondPlayerBonusPp;

            State.Phase = GamePhase.Mulligan;
        }

        private static void DrawInitial(PlayerState ps, int count)
        {
            for (int i = 0; i < count && ps.Deck.Count > 0; i++)
            {
                var card = ps.Deck[ps.Deck.Count - 1];
                ps.Deck.RemoveAt(ps.Deck.Count - 1);
                ps.Hand.Add(card);
            }
        }

        // ---- マリガン ----

        private string? ValidateMulligan(MulliganCommand c)
        {
            if (State.Phase != GamePhase.Mulligan) return "マリガンフェーズではない";
            var ps = State.PlayerOf(c.Player);
            if (ps.MulliganDone) return "既にマリガン済み";
            foreach (var i in c.HandIndices)
                if (i < 0 || i >= ps.Hand.Count) return $"手札インデックス {i} が範囲外";
            return null;
        }

        private void ExecuteMulligan(MulliganCommand c)
        {
            var ps = State.PlayerOf(c.Player);
            var returned = c.HandIndices.Select(i => ps.Hand[i]).ToList();
            foreach (var card in returned) ps.Hand.Remove(card);

            // 先に引き直してから戻したカードを混ぜる(戻したカードをすぐ引き直さないため)
            int drawCount = returned.Count;
            for (int i = 0; i < drawCount && ps.Deck.Count > 0; i++)
            {
                var card = ps.Deck[ps.Deck.Count - 1];
                ps.Deck.RemoveAt(ps.Deck.Count - 1);
                ps.Hand.Add(card);
            }
            ps.Deck.AddRange(returned);
            State.Rng.Shuffle(ps.Deck);
            ps.MulliganDone = true;
            Emit(new MulliganCompletedEvent(c.Player, drawCount));

            if (State.Players.All(p => p.MulliganDone))
                StartTurn(State.CurrentPlayer);
        }

        // ---- ターン進行 ----

        private void StartTurn(int player)
        {
            State.CurrentPlayer = player;
            State.TurnNumber++;
            State.Phase = GamePhase.Main;
            var ps = State.PlayerOf(player);

            // 1. PP
            if (ps.MaxPp < GameRules.PpMax) ps.MaxPp++;
            ps.Pp = ps.MaxPp;
            Emit(new TurnStartedEvent(player, State.TurnNumber, ps.MaxPp));
            // 後攻の追加 PP(覚醒の刻): 1 試合 1 回。使えばもう 1 枚プレイできるターンに自動で発動する
            if (ps.BonusPpLeft > 0 && ShouldUseBonusPp(ps))
            {
                ps.BonusPpLeft--;
                ps.Pp++;
                Emit(new BonusPpUsedEvent(player, ps.Pp));
            }
            Emit(new PpChangedEvent(player, ps.Pp, ps.MaxPp));

            // 攻撃フラグのリセット
            foreach (var e in ps.Board)
            {
                e.SummonedThisTurn = false;
                e.HasAttackedThisTurn = false;
            }

            // 2. ドロー(先攻の第 1 ターンは引かない)
            if (State.TurnNumber != 1)
            {
                Draw(ps);
                if (State.IsFinished) return;
            }

            // 3. カウントダウン
            foreach (var amulet in ps.Amulets.Where(a => a.Countdown.HasValue).ToList())
            {
                amulet.Countdown = amulet.Countdown!.Value - 1;
                Emit(new CountdownChangedEvent(amulet.InstanceId, amulet.Countdown.Value));
            }
            ProcessDeaths();
            ResolveEffectQueue();

            // 4. ターン開始時トリガー: 自分の場のカードを左から順にキューへ積む
            if (!State.IsFinished)
            {
                foreach (var e in ps.Board.ToList())
                    foreach (var effect in e.Definition.EffectsFor(Trigger.TurnStart))
                        _effectQueue.Enqueue(new PendingEffect(player, e.InstanceId, e.Definition, effect, null));
                ResolveEffectQueue();
            }
            CheckLeaderDefeat();
        }

        /// <summary>
        /// 追加 PP を今使うべきか。「+1 すると、今の PP では出せないカードが手札から出せるようになる」ターンで使う。
        /// 使い所を人間が選べるようにするのはフェーズ5(進化と合わせて UI を作る)。
        /// </summary>
        private static bool ShouldUseBonusPp(PlayerState ps)
        {
            if (ps.MaxPp >= GameRules.PpMax) return false;                          // PP が上限なら意味が薄い
            if (ps.MaxPp < GameRules.SecondPlayerBonusPpFromMaxPp) return false;     // 序盤は発動しない
            foreach (var card in ps.Hand)
                if (card.Cost == ps.Pp + 1) return true;
            return false;
        }

        private void ExecuteEndTurn(EndTurnCommand c)
        {
            Emit(new TurnEndedEvent(c.Player));
            // ターン終了時トリガー(フェーズ5)
            StartTurn(1 - c.Player);
        }

        private void ExecuteSurrender(SurrenderCommand c)
        {
            EndGame(1 - c.Player, GameEndReason.Surrender);
        }

        // ---- ドロー ----

        /// <summary>1 枚引く。デッキ切れなら即敗北。手札上限なら消滅。</summary>
        private void Draw(PlayerState ps)
        {
            if (State.IsFinished) return;
            if (ps.Deck.Count == 0)
            {
                Emit(new DeckOutEvent(ps.Index));
                EndGame(1 - ps.Index, GameEndReason.DeckOut);
                return;
            }
            var card = ps.Deck[ps.Deck.Count - 1];
            ps.Deck.RemoveAt(ps.Deck.Count - 1);
            if (ps.Hand.Count >= GameRules.HandMax)
            {
                ps.Graveyard.Add(card.Definition);
                Emit(new CardBurnedEvent(ps.Index, card.CardId));
                return;
            }
            ps.Hand.Add(card);
            Emit(new CardDrawnEvent(ps.Index, card.InstanceId, card.CardId));
        }
    }
}
