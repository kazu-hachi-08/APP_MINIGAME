namespace MiniGame.Molkky
{
    /// <summary>
    /// モルック固有の進行状態（§11）。共通の MiniGameState（Playing / Paused 等）とは別に持つ。
    /// </summary>
    public enum MolkkyPhase
    {
        PlayerSetup,
        TurnStart,
        Aiming,
        Throwing,
        Scoring,
        PinReset,
        GameSet,
    }
}
