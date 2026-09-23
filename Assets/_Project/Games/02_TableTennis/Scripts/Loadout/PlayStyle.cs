namespace MiniGame.TableTennis
{
    /// <summary>選手・ラケットに共通のタイプ。選択画面の表示に使う</summary>
    public enum PlayStyle
    {
        Standard,
        Power,
        Technique
    }

    public static class PlayStyleExtensions
    {
        public static string Label(this PlayStyle style)
        {
            switch (style)
            {
                case PlayStyle.Power: return "パワー";
                case PlayStyle.Technique: return "テクニック";
                default: return "スタンダード";
            }
        }
    }
}
