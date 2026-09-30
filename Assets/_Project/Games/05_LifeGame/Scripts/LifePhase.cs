namespace MiniGame.LifeGame
{
    /// <summary>人生ゲーム固有の進行の区切り（仕様書 §2.4）</summary>
    public enum LifePhase
    {
        ThemeSelect,
        PlayerSetup,
        CharacterSelect,
        TurnStart,
        Spinning,
        Moving,
        CellEvent,
        Settlement,
        GameSet,
    }
}
