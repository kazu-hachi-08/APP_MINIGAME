using System.Collections.Generic;
using System.Linq;
using CardGame.Core.Definitions;

namespace CardGame.Core.State
{
    /// <summary>1 人のプレイヤーの状態(リーダー、PP、デッキ、手札、場、墓場)。</summary>
    public sealed class PlayerState
    {
        public int Index { get; }
        public CardClass Class { get; }

        public int Hp { get; set; }
        public int MaxPp { get; set; }
        public int Pp { get; set; }

        /// <summary>デッキ。末尾がトップ(引くときは末尾から取る)。</summary>
        public List<CardInstance> Deck { get; }
        public List<CardInstance> Hand { get; }
        /// <summary>場。左から順(index 0 が最も左 = 最も古い)。</summary>
        public List<BoardEntity> Board { get; }
        public List<CardDefinition> Graveyard { get; }

        public bool MulliganDone { get; set; }

        /// <summary>残りの追加 PP(「覚醒の刻」)。後攻は 1、先攻は 0 で始まる。01-rules.md「後攻の追加 PP」。</summary>
        public int BonusPpLeft { get; set; }

        public PlayerState(int index, CardClass cardClass)
        {
            Index = index;
            Class = cardClass;
            Hp = GameRules.LeaderMaxHp;
            Deck = new List<CardInstance>();
            Hand = new List<CardInstance>();
            Board = new List<BoardEntity>();
            Graveyard = new List<CardDefinition>();
        }

        private PlayerState(PlayerState src)
        {
            Index = src.Index;
            Class = src.Class;
            Hp = src.Hp;
            MaxPp = src.MaxPp;
            Pp = src.Pp;
            Deck = new List<CardInstance>(src.Deck);          // CardInstance は不変なので浅いコピーでよい
            Hand = new List<CardInstance>(src.Hand);
            Board = src.Board.Select(e => e.Clone()).ToList();
            Graveyard = new List<CardDefinition>(src.Graveyard);
            MulliganDone = src.MulliganDone;
            BonusPpLeft = src.BonusPpLeft;
        }

        public PlayerState Clone() => new PlayerState(this);

        public IEnumerable<BoardEntity> Followers => Board.Where(e => e.IsFollower);
        public IEnumerable<BoardEntity> Amulets => Board.Where(e => e.IsAmulet);
        public bool HasWard => Followers.Any(f => f.HasKeyword(Keyword.Ward));
        public bool BoardIsFull => Board.Count >= GameRules.BoardMax;

        public BoardEntity? FindEntity(int instanceId) => Board.FirstOrDefault(e => e.InstanceId == instanceId);
    }
}
