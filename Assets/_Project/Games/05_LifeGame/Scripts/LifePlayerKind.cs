namespace MiniGame.LifeGame
{
    /// <summary>席を人間が操作するかNPCが操作するか。運のゲームなのでNPCに難易度はない（仕様書 §10.2）</summary>
    public enum LifePlayerKind
    {
        Human,
        Npc,
    }
}
