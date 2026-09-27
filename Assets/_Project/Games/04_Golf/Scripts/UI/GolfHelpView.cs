using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// 「？」ボタンで操作説明のパネルを開き、パネルのどこかをタップで閉じる。
    /// オンライン対戦では時間を止められないので、ゲームは止めずに上に重ねるだけにする。
    /// </summary>
    public class GolfHelpView : MonoBehaviour
    {
        [SerializeField] private ShotInput _input;
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _panel;
        [SerializeField] private Text _text;

        [TextArea(10, 30)]
        [SerializeField] private string _helpText =
            "<b>■ ショットの打ち方（3回タップ）</b>\n" +
            "① ゲージをタップ → 白い線が右へ動く\n" +
            "② もう一度タップ → 強さが決まる（赤い線）\n" +
            "③ 戻ってくる白い線を<color=#F2BF33>黄色</color>で止める\n" +
            "\n" +
            "<b>■ 強さと飛距離</b>\n" +
            "・飛距離は強さの2乗くらい（7割で約半分、5割で約1/4）\n" +
            "・着地予測はフルパワーの位置。風と傾斜は入っていない\n" +
            "\n" +
            "<b>■ 黄色で止める位置</b>\n" +
            "・ど真ん中 → ナイスショット（まっすぐ）\n" +
            "・右寄り（早め）→ スライス（右に曲がる）\n" +
            "・左寄り（遅め）→ フック（左に曲がる）\n" +
            "・黄色の外 → ミスショット（大きく曲がって飛ばない）\n" +
            "・バンカーでは黄色がせまくなる\n" +
            "\n" +
            "<b>■ そのほか</b>\n" +
            "・画面を左右にドラッグ / ◀▶ で方向を変える\n" +
            "・真ん中のボタンでクラブを切り替え\n" +
            "・「全体」を押している間ホール全体が見える\n" +
            "\n" +
            "（タップで閉じる）";

        private void Awake()
        {
            _openButton.onClick.AddListener(Open);
            _panel.onClick.AddListener(Close);
            _text.text = _helpText;
            Close();
        }

        private void LateUpdate()
        {
            // スイング中に開くとタップがゲージに伝わってしまうので、構えている間だけ押せるようにする
            _openButton.interactable = _input.CanAim;
        }

        private void Open() => _panel.gameObject.SetActive(true);

        private void Close() => _panel.gameObject.SetActive(false);
    }
}
