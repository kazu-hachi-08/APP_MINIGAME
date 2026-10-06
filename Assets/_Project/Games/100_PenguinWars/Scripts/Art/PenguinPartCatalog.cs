using System.Collections.Generic;

namespace MiniGame.PenguinWars.Art
{
    /// <summary>見た目のどの部位か（じぶんペンギンの作成画面で1行ずつ選ぶ）</summary>
    public enum PenguinPartSlot
    {
        Body,
        BodyColor,
        Head,
        Hand,
        Back,
    }

    /// <summary>
    /// 部位ごとに選べる ID の一覧。パターンの辞書のキーから作るので、パーツを足せば自動で選べるようになる。
    /// 頭・手・背中は「なし」（空文字）を先頭に入れる。保存データや通信で届いた知らない ID を掃除するのにも使う
    /// </summary>
    public static class PenguinPartCatalog
    {
        private static readonly Dictionary<PenguinPartSlot, List<string>> IdsBySlot = Build();

        public static IReadOnlyList<string> Ids(PenguinPartSlot slot) => IdsBySlot[slot];

        public static bool IsKnown(PenguinPartSlot slot, string id) => IdsBySlot[slot].Contains(id ?? string.Empty);

        public static bool IsOptional(PenguinPartSlot slot) => slot != PenguinPartSlot.Body && slot != PenguinPartSlot.BodyColor;

        /// <summary>知らない ID を、その部位の既定（体は basic・体色は standard・ほかは「なし」）に置き換える</summary>
        public static string Sanitize(PenguinPartSlot slot, string id)
        {
            if (IsKnown(slot, id)) return id ?? string.Empty;
            switch (slot)
            {
                case PenguinPartSlot.Body: return PenguinLook.DefaultBody;
                case PenguinPartSlot.BodyColor: return PenguinLook.DefaultBodyColor;
                default: return string.Empty;
            }
        }

        private static Dictionary<PenguinPartSlot, List<string>> Build()
        {
            return new Dictionary<PenguinPartSlot, List<string>>
            {
                [PenguinPartSlot.Body] = new List<string>(PenguinBodyPatterns.Shapes.Keys),
                [PenguinPartSlot.BodyColor] = new List<string>(PenguinPalette.BodyColorIds),
                [PenguinPartSlot.Head] = WithNone(PenguinPartPatterns.Heads.Keys),
                [PenguinPartSlot.Hand] = WithNone(PenguinPartPatterns.Hands.Keys),
                [PenguinPartSlot.Back] = WithNone(PenguinPartPatterns.Backs.Keys),
            };
        }

        private static List<string> WithNone(IEnumerable<string> ids)
        {
            var list = new List<string> { string.Empty };
            list.AddRange(ids);
            return list;
        }
    }
}
