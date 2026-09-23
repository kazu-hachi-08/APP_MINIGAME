using System;
using System.Collections.Generic;
using System.Linq;
using CardGame.Core.Commands;
using CardGame.Core.Definitions;
using CardGame.Core.Events;
using CardGame.Core.State;

namespace CardGame.Core.Engine
{
    /// <summary>
    /// ゲームエンジン本体。コマンドを検証・適用し、発生したイベント列を返す。
    /// 実装は役割ごとに partial に分割している:
    ///   GameEngine.Turn.cs   … 開始・マリガン・ターン進行・ドロー
    ///   GameEngine.Play.cs   … カードのプレイ
    ///   GameEngine.Combat.cs … 攻撃
    ///   GameEngine.Effects.cs… 効果解決(トリガーキュー・アクション・対象・死亡チェック)
    /// </summary>
    public sealed partial class GameEngine
    {
        public CardDatabase Db { get; }
        public GameState State { get; }

        private readonly List<GameEvent> _events = new List<GameEvent>();
        private readonly Queue<PendingEffect> _effectQueue = new Queue<PendingEffect>();

        private GameEngine(CardDatabase db, GameState state)
        {
            Db = db;
            State = state;
        }

        /// <summary>
        /// 新しい対戦を作る。デッキは検証済みであること。
        /// firstPlayer を省略するとシードから決める。
        /// </summary>
        public static GameEngine StartNew(CardDatabase db, DeckDefinition deck0, DeckDefinition deck1, ulong seed, int? firstPlayer = null)
        {
            var state = new GameState(deck0.Class, deck1.Class, seed);
            var engine = new GameEngine(db, state);
            engine.SetupGame(deck0, deck1, firstPlayer);
            return engine;
        }

        /// <summary>
        /// テスト・特殊用途: 空の状態からエンジンを作る(デッキも手札も空、Main フェーズ)。
        /// </summary>
        public static GameEngine CreateEmpty(CardDatabase db, CardClass class0, CardClass class1, ulong seed, int currentPlayer = 0)
        {
            var state = new GameState(class0, class1, seed)
            {
                Phase = GamePhase.Main,
                CurrentPlayer = currentPlayer,
                TurnNumber = 1,
            };
            foreach (var p in state.Players) { p.MulliganDone = true; p.MaxPp = 1; p.Pp = 1; }
            return new GameEngine(db, state);
        }

        /// <summary>AI の先読み用に完全複製する。イベントやキューは引き継がない(キューは常に空の状態で複製すること)。</summary>
        public GameEngine Clone()
        {
            if (_effectQueue.Count != 0) throw new InvalidOperationException("効果解決中に複製はできない");
            return new GameEngine(Db, State.Clone());
        }

        /// <summary>直近の Apply で発生したイベント。</summary>
        public IReadOnlyList<GameEvent> LastEvents => _events;

        // ------------------------------------------------------------------
        // コマンド適用
        // ------------------------------------------------------------------

        /// <summary>コマンドを適用し、発生したイベントを返す。不正なコマンドは <see cref="InvalidCommandException"/>。</summary>
        public IReadOnlyList<GameEvent> Apply(GameCommand command)
        {
            var error = Validate(command);
            if (error != null) throw new InvalidCommandException($"{command}: {error}");

            _events.Clear();
            switch (command)
            {
                case MulliganCommand c: ExecuteMulligan(c); break;
                case PlayCardCommand c: ExecutePlayCard(c); break;
                case AttackCommand c: ExecuteAttack(c); break;
                case EndTurnCommand c: ExecuteEndTurn(c); break;
                case SurrenderCommand c: ExecuteSurrender(c); break;
                default: throw new InvalidCommandException($"未知のコマンド: {command.GetType().Name}");
            }
            return _events.ToList();
        }

        public bool TryApply(GameCommand command, out IReadOnlyList<GameEvent> events, out string? error)
        {
            error = Validate(command);
            if (error != null)
            {
                events = Array.Empty<GameEvent>();
                return false;
            }
            events = Apply(command);
            return true;
        }

        /// <summary>コマンドが合法か。合法なら null、不正なら理由。</summary>
        public string? Validate(GameCommand command)
        {
            if (State.IsFinished) return "ゲームは終了している";
            if (command.Player != 0 && command.Player != 1) return "プレイヤー番号が不正";

            switch (command)
            {
                case MulliganCommand c: return ValidateMulligan(c);
                case SurrenderCommand _: return null;
                case PlayCardCommand c: return ValidatePlayCard(c);
                case AttackCommand c: return ValidateAttack(c);
                case EndTurnCommand c: return ValidateMainPhaseTurn(c.Player);
                default: return "未知のコマンド";
            }
        }

        private string? ValidateMainPhaseTurn(int player)
        {
            if (State.Phase != GamePhase.Main) return "メインフェーズではない";
            if (State.CurrentPlayer != player) return "自分の手番ではない";
            return null;
        }

        // ------------------------------------------------------------------
        // 合法手の列挙(AI / UI 用)
        // ------------------------------------------------------------------

        /// <summary>
        /// 指定プレイヤーが今取れる合法手を列挙する。
        /// マリガン中は何も返さない(マリガンの選び方は AI/UI 側の判断)。
        /// </summary>
        public IEnumerable<GameCommand> LegalCommands(int player)
        {
            if (State.IsFinished || State.Phase != GamePhase.Main || State.CurrentPlayer != player)
                yield break;

            var me = State.PlayerOf(player);
            for (int i = 0; i < me.Hand.Count; i++)
            {
                var card = me.Hand[i];
                if (CanPlay(player, i) != null) continue;
                var selectKind = card.Definition.PlaySelectTarget;
                if (selectKind.HasValue)
                {
                    var targets = ValidSelectTargets(player, selectKind.Value, null);
                    if (targets.Count == 0)
                    {
                        if (!card.Definition.IsSpell) yield return new PlayCardCommand(player, i);
                    }
                    else
                    {
                        foreach (var t in targets) yield return new PlayCardCommand(player, i, int.MaxValue, t);
                    }
                }
                else
                {
                    yield return new PlayCardCommand(player, i);
                }
            }

            foreach (var attacker in me.Followers.ToList())
            {
                if (CanAttack(attacker) != null) continue;
                foreach (var t in ValidAttackTargets(attacker))
                    yield return new AttackCommand(player, attacker.InstanceId, t);
            }

            yield return new EndTurnCommand(player);
        }

        // ------------------------------------------------------------------
        // 内部ユーティリティ
        // ------------------------------------------------------------------

        private void Emit(GameEvent e) => _events.Add(e);

        private void EndGame(int? winner, GameEndReason reason)
        {
            if (State.IsFinished) return;
            State.Phase = GamePhase.Finished;
            State.Winner = winner;
            Emit(new GameEndedEvent(winner, reason));
        }

        /// <summary>リーダー HP による勝敗判定。効果キューが空になった時点で呼ぶ(02 の解決順序 4)。</summary>
        private void CheckLeaderDefeat()
        {
            if (State.IsFinished) return;
            bool p0Dead = State.Players[0].Hp <= 0;
            bool p1Dead = State.Players[1].Hp <= 0;
            if (p0Dead && p1Dead) EndGame(null, GameEndReason.Draw);
            else if (p0Dead) EndGame(1, GameEndReason.LeaderDefeated);
            else if (p1Dead) EndGame(0, GameEndReason.LeaderDefeated);
        }
    }
}
