using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ステージごとに山と地面の色を変える（仕様書 §3.4）。絵をステージの数だけ作らずに見た目の違いを出すため。
    /// 空はカメラの背景色とつながっているので変えない（変えると空の上端に切れ目が出る）
    /// </summary>
    public class FieldBackdrop : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] _tinted;

        public void SetTint(Color tint)
        {
            foreach (SpriteRenderer renderer in _tinted) renderer.color = tint;
        }
    }
}
