namespace MiniGame.Common.Profile
{
    /// <summary>
    /// 「席N の表示名」表。各ゲームは席番号から名前を引くだけにして、ローカル/オンラインの違いはここで吸収する。
    /// 名前が入っていない席は従来どおり「P1」「P2」… を返す。
    /// </summary>
    public static class SeatNames
    {
        private static string[] _names = System.Array.Empty<string>();

        public static string Get(int seat) => Get(seat, DefaultName(seat));

        /// <summary>名前が届いていない席は fallback を返す（卓球の RIVAL、サッカーの AWAY など従来表記に戻すため）</summary>
        public static string Get(int seat, string fallback)
        {
            bool hasName = seat >= 0 && seat < _names.Length && !string.IsNullOrEmpty(_names[seat]);
            return hasName ? _names[seat] : fallback;
        }

        public static string DefaultName(int seat) => $"P{seat + 1}";

        /// <summary>
        /// 1台で遊ぶとき。ユーザー名は端末の持ち主＝席0 にだけ付け、P2 以降は従来どおり。
        /// 席0 が NPC のときはユーザー名を付けない（NPC が自分の名前で動くのは不自然なため）
        /// </summary>
        public static void UseLocal(bool seat0IsHuman = true)
        {
            _names = seat0IsHuman && UserProfile.HasName ? new[] { UserProfile.Name } : System.Array.Empty<string>();
        }

        /// <summary>オンライン。null や届いていない席は「P{n}」になる</summary>
        public static void UseOnline(string[] names)
        {
            _names = names ?? System.Array.Empty<string>();
        }
    }
}
