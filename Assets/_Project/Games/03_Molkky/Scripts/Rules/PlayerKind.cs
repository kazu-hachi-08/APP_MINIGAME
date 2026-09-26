namespace MiniGame.Molkky
{
    /// <summary>
    /// プレイヤーを人間が操作するか、どの難易度のNPCが操作するか（§10.1）。
    /// NPCは よわい → つよい の順に並べ、NpcThrower の難易度配列の並びと合わせている。
    /// </summary>
    public enum PlayerKind
    {
        Human,
        NpcWeak,
        NpcNormal,
        NpcStrong,
    }
}
