namespace MiniGame.Golf
{
    /// <summary>
    /// ゴルフ固有の進行状態。共通基盤の MiniGameState とは別に持つ。
    /// 方向・クラブ・ゲージの操作（Aiming / Swinging）は ShotInput の中で分かれているので、ここでは Aiming にまとめる。
    /// </summary>
    public enum GolfPhase
    {
        Setup,
        CharacterSelect,
        HoleStart,
        TurnStart,
        Aiming,
        BallMoving,
        ShotResult,
        HoleResult,
        GameSet,
    }
}
