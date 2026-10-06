using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 城の見た目と頭上のHPバー。HP の計算は戦闘ロジック側が持ち、ここは渡された値を描くだけ
    /// </summary>
    public class CastleView : MonoBehaviour
    {
        [SerializeField] private HpBarView _hpBar;

        public void SetHp(int current, int max)
        {
            _hpBar.gameObject.SetActive(true);
            _hpBar.SetRatio(max > 0 ? (float)current / max : 0f);
        }
    }
}
