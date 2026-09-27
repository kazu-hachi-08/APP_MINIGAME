namespace MiniGame.Common.Scene
{
    /// <summary>
    /// ブラウザ版の表示枠をシーンの向きに合わせる。
    /// ブラウザでは Screen.orientation が効かず、枠の形が固定だと縦画面用のUIが横長に潰れるため、
    /// 枠そのものを 9:16 / 16:9 に切り替える。
    /// </summary>
    public static class WebScreen
    {
        // UIの基準解像度(CanvasScaler)と同じ比率にする
        private const int ShortSide = 9;
        private const int LongSide = 16;

        public static void SetPortrait() => SetAspect(ShortSide, LongSide);

        public static void SetLandscape() => SetAspect(LongSide, ShortSide);

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void MiniGame_SetAspect(int width, int height);

        private static void SetAspect(int width, int height) => MiniGame_SetAspect(width, height);
#else
        private static void SetAspect(int width, int height) { }
#endif
    }
}
