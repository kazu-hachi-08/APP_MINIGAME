namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 試合の進行（仕様書 §2.4）。ポーズ・リザルトは BaseMiniGameManager の MiniGameState が持つので、
    /// こちらはゲーム固有の段階だけを表す
    /// </summary>
    public enum PenguinWarsPhase
    {
        /// <summary>モード選択・オンラインの接続待ち（編成がまだ決まっていない）</summary>
        ModeSelect,
        Draft,
        Intro,
        Playing,
        Finished,
    }
}
