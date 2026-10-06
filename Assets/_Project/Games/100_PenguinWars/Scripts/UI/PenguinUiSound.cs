using MiniGame.Common.Audio;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 画面のボタンを押したときの音。どのパネルでも同じ音を鳴らすので1か所にまとめる。
    /// AudioManager の無い状態（Scene を直接開いた・テスト中）でも止まらないよう、有無を確かめてから鳴らす
    /// </summary>
    public static class PenguinUiSound
    {
        public static void Click()
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlaySe(SeId.ButtonClick);
        }
    }
}
