using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 城の見た目と頭上のHPバー。HP の計算は戦闘ロジック側が持ち、ここは渡された値を描くだけ
    /// </summary>
    public class CastleView : MonoBehaviour
    {
        [SerializeField] private HpBarView _hpBar;
        [SerializeField] private SpriteRenderer _body;
        [Tooltip("エンドレスで無敵の出現ゲートとして見せるときの絵（仕様書 §7.3）。左の城には不要")]
        [SerializeField] private Sprite _gateSprite;

        public void SetHp(int current, int max)
        {
            _hpBar.gameObject.SetActive(true);
            _hpBar.SetRatio(max > 0 ? (float)current / max : 0f);
        }

        /// <summary>同じシーンをエンドレスと対戦で使うので、城かゲートかは試合が始まってから決める</summary>
        public void ShowAsGate()
        {
            _hpBar.gameObject.SetActive(false);
            if (_gateSprite == null) return;

            _body.sprite = _gateSprite;
            // 右の城用の左右反転を外し、ゲートは描いたとおりの向き（岩の影が右）で見せる
            _body.flipX = false;
        }
    }
}
