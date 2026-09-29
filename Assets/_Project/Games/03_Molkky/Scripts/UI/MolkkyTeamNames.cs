namespace MiniGame.Molkky
{
    /// <summary>チームの表示名。スコアボード・手番表示・キャラ選択・リザルトで同じ呼び方にそろえるため1か所にまとめる</summary>
    public static class MolkkyTeamNames
    {
        private static readonly string[] Names = { "チームA", "チームB" };

        public static string Get(int team)
        {
            return Names[team % Names.Length];
        }
    }
}
