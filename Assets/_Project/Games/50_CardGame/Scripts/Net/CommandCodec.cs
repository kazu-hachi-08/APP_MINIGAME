#nullable enable
using System;
using System.Linq;
using CardGame.Core.Commands;
using CardGame.Core.State;
using Newtonsoft.Json;

namespace CardGame.Unity.Net
{
    /// <summary>
    /// GameCommand ⇔ JSON。オンライン対戦でコマンドをそのまま送るための最小のシリアライザ(05-online.md)。
    /// </summary>
    public static class CommandCodec
    {
        private sealed class Dto
        {
            public string t = "";          // 種別
            public int p;                  // プレイヤー
            public int[]? idx;             // Mulligan: 手札インデックス
            public int hand;               // PlayCard: 手札インデックス
            public int pos;                // PlayCard: 場の位置
            public int atk;                // Attack: 攻撃側 InstanceId
            public bool hasTarget;
            public bool targetLeader;
            public int targetPlayer;
            public int targetId;
        }

        public static string Encode(GameCommand cmd)
        {
            var d = new Dto { p = cmd.Player };
            switch (cmd)
            {
                case MulliganCommand m: d.t = "mul"; d.idx = m.HandIndices.ToArray(); break;
                case PlayCardCommand pc: d.t = "play"; d.hand = pc.HandIndex; d.pos = pc.BoardPosition; SetTarget(d, pc.Target); break;
                case AttackCommand a: d.t = "atk"; d.atk = a.AttackerInstanceId; SetTarget(d, a.Target); break;
                case EndTurnCommand: d.t = "end"; break;
                case SurrenderCommand: d.t = "sur"; break;
                default: throw new ArgumentException($"未対応のコマンド {cmd.GetType().Name}");
            }
            return JsonConvert.SerializeObject(d);
        }

        public static GameCommand Decode(string json)
        {
            var d = JsonConvert.DeserializeObject<Dto>(json) ?? throw new FormatException("コマンド JSON が不正");
            TargetRef? target = d.hasTarget ? (d.targetLeader ? TargetRef.Leader(d.targetPlayer) : TargetRef.Entity(d.targetId)) : null;
            return d.t switch
            {
                "mul" => new MulliganCommand(d.p, d.idx),
                "play" => new PlayCardCommand(d.p, d.hand, d.pos, target),
                "atk" => new AttackCommand(d.p, d.atk, target ?? throw new FormatException("Attack に対象がない")),
                "end" => new EndTurnCommand(d.p),
                "sur" => new SurrenderCommand(d.p),
                _ => throw new FormatException($"未知のコマンド種別 {d.t}"),
            };
        }

        private static void SetTarget(Dto d, TargetRef? t)
        {
            if (!t.HasValue) return;
            d.hasTarget = true;
            d.targetLeader = t.Value.IsLeader;
            d.targetPlayer = t.Value.PlayerIndex;
            d.targetId = t.Value.InstanceId;
        }
    }
}
