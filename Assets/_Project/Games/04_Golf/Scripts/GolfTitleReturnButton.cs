using MiniGame.Common.Scene;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// タイトルへ戻るボタン（Phase 0 の遷移確認用）。
    /// GolfGameManager（BaseMiniGameManager）ができるまでの間、PAUSE を経由せずに直接戻れるようにしておく。
    /// </summary>
    public class GolfTitleReturnButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private void Awake()
        {
            _button.onClick.AddListener(ReturnToTitle);
        }

        private void ReturnToTitle()
        {
            // GolfScene をエディタで直接再生したときなど SceneLoader が無い場合でも戻れるようにする
            if (SceneLoader.HasInstance)
            {
                SceneLoader.Instance.LoadTitleScene();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.Title);
            }
        }
    }
}
