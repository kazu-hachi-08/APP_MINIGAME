using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// ゴルファーのスイングをコマ送りで見せる。いつ・どこまで振るかは GolferView が決め、ここは「その進み具合のコマ」を出すだけ。
    /// 1コマは GolferSwingSprites の4枚（色そのまま・シャツ・帽子・髪）を重ね、白で描いた3枚にだけ色を掛ける。
    /// </summary>
    public class GolferSprite : MonoBehaviour
    {
        [Tooltip("肌・ズボン・靴・クラブ・縁取り。色を掛けない")]
        [SerializeField] private SpriteRenderer _base;

        [Tooltip("シャツ。プレイヤー色を掛ける")]
        [SerializeField] private SpriteRenderer _shirt;

        [Tooltip("帽子。キャラの帽子の色を掛ける")]
        [SerializeField] private SpriteRenderer _cap;

        [Tooltip("髪。キャラの髪の色を掛ける")]
        [SerializeField] private SpriteRenderer _hair;

        /// <summary>GolferSwingSprites.Layer の並び</summary>
        private SpriteRenderer[] _layers;

        private void Awake()
        {
            _layers = new[] { _base, _shirt, _cap, _hair };
            ShowFrame(GolferSwingSprites.Backswing[0]);
        }

        public void SetVisible(bool visible)
        {
            foreach (SpriteRenderer layer in _layers) layer.enabled = visible;
        }

        public void SetPlayerColor(Color color)
        {
            _shirt.color = color;
        }

        /// <summary>シャツはプレイヤー色のままなので、同じキャラ同士でも区別できる</summary>
        public void SetCharacterColors(Color cap, Color hair)
        {
            _cap.color = cap;
            _hair.color = hair;
        }

        /// <param name="backswing">構え0 → トップ1。ダウンスイングでは同じコマを逆に戻る</param>
        /// <param name="followThrough">インパクト0 → フィニッシュ1。0より大きい間はこちらのコマを出す</param>
        public void ApplyPose(float backswing, float followThrough)
        {
            bool isFollowingThrough = followThrough > 0f;
            Sprite[][] frames = isFollowingThrough ? GolferSwingSprites.FollowThrough : GolferSwingSprites.Backswing;
            float progress = isFollowingThrough ? followThrough : backswing;
            ShowFrame(frames[Mathf.RoundToInt(progress * (frames.Length - 1))]);
        }

        private void ShowFrame(Sprite[] frame)
        {
            for (int i = 0; i < GolferSwingSprites.LayerCount; i++) _layers[i].sprite = frame[i];
        }
    }
}
