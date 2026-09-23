using System.Collections.Generic;
using System.Linq;
using CardGame.Core.Commands;
using CardGame.Core.Definitions;
using CardGame.Core.Engine;
using CardGame.Core.State;

namespace CardGame.Core.Ai
{
    /// <summary>
    /// 1 手先読みの貪欲 AI。
    /// 合法手を全て試し、盤面評価が最も良くなる手を選ぶ。改善する手がなければターン終了。
    /// コンボや長期戦は考慮しない(playtest の注意事項参照)。
    /// </summary>
    public sealed class GreedyAgent : IAgent
    {
        private readonly DeterministicRandom _rng;

        /// <summary>マリガンで手札に残す最大コスト。</summary>
        public int MulliganKeepCost { get; set; } = 3;

        public GreedyAgent(ulong seed)
        {
            _rng = new DeterministicRandom(seed);
        }

        public GameCommand Decide(GameEngine engine, int player)
        {
            var state = engine.State;
            if (state.Phase == GamePhase.Mulligan)
            {
                var hand = state.PlayerOf(player).Hand;
                var replace = Enumerable.Range(0, hand.Count).Where(i => hand[i].Cost > MulliganKeepCost);
                return new MulliganCommand(player, replace);
            }

            double baseline = Evaluate(state, player, engine.Db);
            GameCommand? best = null;
            double bestScore = double.NegativeInfinity;
            int ties = 0;

            foreach (var cmd in engine.LegalCommands(player))
            {
                if (cmd is EndTurnCommand) continue;
                var sim = engine.Clone();
                sim.Apply(cmd);
                double score = Evaluate(sim.State, player, engine.Db);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = cmd;
                    ties = 1;
                }
                else if (score == bestScore)
                {
                    // 同点は乱数で選ぶ(決定論的)
                    ties++;
                    if (_rng.Next(ties) == 0) best = cmd;
                }
            }

            if (best != null && bestScore > baseline) return best;
            return new EndTurnCommand(player);
        }

        /// <summary>プレイヤー視点の盤面評価。大きいほど有利。db を渡すとラストワードで出るフォロワーも評価に含める。</summary>
        public static double Evaluate(GameState state, int player, CardDatabase? db = null)
        {
            if (state.IsFinished)
            {
                if (state.Winner == player) return 100000;
                if (state.Winner == null) return 0;
                return -100000;
            }

            var me = state.PlayerOf(player);
            var enemy = state.OpponentOf(player);

            double score = 0;
            // リーダー HP: 自分の HP は残り少ないほど重く見る
            score += HpValue(me.Hp) - HpValue(enemy.Hp);
            // 盤面
            score += me.Board.Sum(x => EntityValue(x, db));
            score -= enemy.Board.Sum(x => EntityValue(x, db));
            // 手札(カードアドバンテージ)。重みは AiTuning
            score += me.Hand.Count * AiTuning.HandCard;
            score -= enemy.Hand.Count * AiTuning.HandCard;
            // デッキ切れが近いとき(残り枚数が少ないと負けに直結する)
            score -= DeckOutPenalty(me.Deck.Count);
            score += DeckOutPenalty(enemy.Deck.Count);
            // 相手の守護に塞がれている打点(自分の手番のみ)。守護を割る価値を 1 手読みでも見えるようにする
            if (state.CurrentPlayer == player && enemy.HasWard)
                score -= AiTuning.BlockedByWard * me.Followers
                    .Where(f => f.Attack > 0 && !f.HasAttackedThisTurn && (!f.SummonedThisTurn || f.HasKeyword(Keyword.Storm)))
                    .Sum(f => f.Attack);
            return score;
        }

        /// <summary>残りデッキが少ないほど大きくなるペナルティ(デッキ切れ = 敗北のため)。</summary>
        private static double DeckOutPenalty(int deckCount)
        {
            if (deckCount > AiTuning.DeckOutWarning) return 0;
            int over = AiTuning.DeckOutWarning - deckCount + 1;
            return over * over * 0.8;
        }

        private static double HpValue(int hp)
        {
            // 20 → 20, 10 → 8, 5 → 2.5 くらいの曲線。低 HP を守る動機付け
            if (hp <= 0) return -50;
            return hp <= 10 ? hp * 0.8 : 8 + (hp - 10) * 1.2;
        }

        private static double EntityValue(BoardEntity e, CardDatabase? db)
        {
            if (e.IsAmulet) return AiTuning.AmuletBase + (e.Countdown ?? 0) * 0.3;
            double v = e.Attack + e.Health + AiTuning.FollowerBase;
            // ラストワード持ちは破壊されても仕事が残るので、失う痛みを割り引く
            if (e.Definition.Effects.Any(x => x.Trigger == Trigger.LastWords)) v -= AiTuning.LastWordsDiscount;
            if (e.HasKeyword(Keyword.Ward)) v += 1.0;
            if (e.HasKeyword(Keyword.Storm)) v += 1.0;
            if (e.HasKeyword(Keyword.Rush)) v += 0.5;
            if (e.HasKeyword(Keyword.Bane)) v += 1.0;
            if (e.HasKeyword(Keyword.Drain)) v += 0.5;
            if (e.Attack == 0) v -= 1.0;
            // ラストワードで出るフォロワー(竜の卵など)。後から場に残る分を先取りで評価する
            if (db != null)
                foreach (var a in e.Definition.EffectsFor(Trigger.LastWords).SelectMany(x => x.Actions).Where(a => a.Type == ActionType.Summon && a.CardId != null))
                {
                    var token = db.Get(a.CardId!);
                    v += a.Count * (token.Attack + token.Health) * AiTuning.LastWordsSummon;
                }
            return v;
        }
    }
}
