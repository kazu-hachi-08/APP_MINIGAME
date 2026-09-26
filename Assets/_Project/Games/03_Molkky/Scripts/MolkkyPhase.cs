namespace MiniGame.Molkky
{
    /// <summary>
    /// モルック固有の進行状態（§11）。共通の MiniGameState（Playing / Paused 等）とは別に持つ。
    /// PlayerSetup は Phase 5 で追加する。
    /// </summary>
    public enum MolkkyPhase
    {
        TurnStart,
        Aiming,
        Throwing,
        Scoring,
        PinReset,
        GameSet,
    }
}
