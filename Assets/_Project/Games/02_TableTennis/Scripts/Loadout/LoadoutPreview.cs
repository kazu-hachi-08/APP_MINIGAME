using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 選択中の選手・ラケットの絵・能力・必殺技を見せる選択画面のカード。
    /// ボタン名だけでは違いが分からず「なんとなく選ぶ」画面になっていたため、押すたびに中身が変わるようにする。
    /// 能力は倍率をそのまま見せず、5段階のバーにして直感で比べられるようにする。
    /// </summary>
    public class LoadoutPreview : MonoBehaviour
    {
        private const int MaxLevel = 5;
        private const int StandardLevel = 3;
        private const char BarMark = '■';

        [SerializeField] private Image _portrait;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _statsText;

        [Header("Bar")]
        [Tooltip("倍率がこれだけ変わるごとにバーを1段階上下させる（スタンダード=1.0が3段階）")]
        [SerializeField] private float _multiplierPerLevel = 0.1f;

        [SerializeField] private Color _filledColor = new Color(1f, 0.82f, 0.25f);
        [SerializeField] private Color _emptyColor = new Color(0.35f, 0.38f, 0.45f);

        [Header("Pop")]
        [Tooltip("選手を変えたときに絵を弾ませる時間。切り替わったことを目で追えるようにする")]
        [SerializeField] private float _popDuration = 0.18f;

        [SerializeField] private float _popScale = 1.2f;

        private CharacterData _shownCharacter;
        private float _popTimer;

        public void Show(CharacterData character, RacketData racket)
        {
            // ラケットだけ変えたときに絵が跳ねると、選手が変わったように見えてしまうため
            if (character != _shownCharacter)
            {
                _shownCharacter = character;
                _popTimer = _popDuration;
            }

            _portrait.sprite = character.FrontSprite;
            _nameText.text = $"{character.DisplayName}（{character.Style.Label()}）";
            _statsText.text = BuildStats(character, racket);
        }

        private void Update()
        {
            if (_popTimer <= 0f) return;

            _popTimer = Mathf.Max(0f, _popTimer - Time.unscaledDeltaTime);
            float scale = Mathf.Lerp(1f, _popScale, _popTimer / _popDuration);
            _portrait.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        private string BuildStats(CharacterData character, RacketData racket)
        {
            // タイミング誤差は小さいほど良いので、反転して「安定」として見せる
            string body = $"スピード{Bar(character.MoveSpeedMultiplier)}  リーチ{Bar(character.ReachMultiplier)}  猶予{Bar(character.SwingDurationMultiplier)}";
            string tool = $"球速{Bar(racket.SpeedMultiplier)}  回転{Bar(racket.SpinMultiplier)}  安定{Bar(2f - racket.ErrorMultiplier)}";
            return $"{body}\n{tool}\n{BuildSpecials(character)}";
        }

        private static string BuildSpecials(CharacterData character)
        {
            string weak = character.WeakSpecial != null ? character.WeakSpecial.DisplayName : "-";
            string strong = character.StrongSpecial != null ? character.StrongSpecial.DisplayName : "-";
            return $"弱SP {weak} / 強SP {strong}";
        }

        private string Bar(float multiplier)
        {
            int level = Mathf.Clamp(StandardLevel + Mathf.RoundToInt((multiplier - 1f) / _multiplierPerLevel), 1, MaxLevel);
            string filled = Colored(new string(BarMark, level), _filledColor);
            string empty = Colored(new string(BarMark, MaxLevel - level), _emptyColor);
            return filled + empty;
        }

        private static string Colored(string text, Color color)
        {
            return text.Length > 0 ? $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>" : string.Empty;
        }
    }
}
