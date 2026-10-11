using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// GAME SET からリザルトまでの間に、勝った選手を主役にして勝敗を見せる画面。
    /// 専用の絵を用意せずに済むよう、選手の正面絵を拡大して跳ねさせ、セリフで個性を出す。
    /// リザルト自体は共通の ResultDialog に任せ、この画面はその後ろに残して背景にする。
    /// </summary>
    public class VictoryScreen : MonoBehaviour
    {
        /// <summary>飛び出しのうち、行き過ぎの大きさに達するまでの割合。残りで元の大きさへ戻す</summary>
        private const float PopPeakRatio = 0.7f;

        [SerializeField] private Text _titleText;
        [SerializeField] private Image _winnerPortrait;
        [SerializeField] private Text _winnerQuoteText;
        [SerializeField] private Image _loserPortrait;
        [SerializeField] private Text _loserQuoteText;
        [SerializeField] private ConfettiEffect _confetti;

        [Header("Timing")]
        [Tooltip("リザルトを出すまでの時間。セリフを読み切れる長さにする")]
        [SerializeField] private float _displayDuration = 3f;

        [Tooltip("勝者が飛び出してくる時間")]
        [SerializeField] private float _popInDuration = 0.35f;

        [Tooltip("飛び出しの行き過ぎの大きさ。1より大きいほど弾んで見える")]
        [SerializeField] private float _popOvershoot = 1.2f;

        [Header("Winner")]
        [SerializeField] private float _hopHeight = 40f;
        [SerializeField] private float _hopsPerSecond = 1.5f;

        [Header("Loser")]
        [Tooltip("負けた選手の色。暗くして勝者を引き立てる")]
        [SerializeField] private Color _loserTint = new Color(0.45f, 0.45f, 0.55f);

        [Tooltip("負けた選手を縦に縮める割合。うなだれて見せる")]
        [Range(0f, 1f)]
        [SerializeField] private float _loserSquash = 0.1f;

        [Header("Title")]
        [SerializeField] private Color _playerWinColor = new Color(1f, 0.85f, 0.2f);

        [Tooltip("負けたときのタイトル色。勝ったときと色で区別する")]
        [SerializeField] private Color _playerLoseColor = new Color(1f, 0.4f, 0.35f);

        private Vector2 _winnerBasePosition;
        private float _elapsed;
        private bool _isPlaying;

        /// <summary>勝敗を見せ、リザルトを出してよくなるまで待つ</summary>
        public IEnumerator Play(string winnerLabel, CharacterData winner, CharacterData loser, bool isPlayerWinner)
        {
            _titleText.text = $"{winnerLabel} WIN!";
            _titleText.color = isPlayerWinner ? _playerWinColor : _playerLoseColor;
            ShowCharacter(_winnerPortrait, _winnerQuoteText, winner.FrontSprite, winner.WinQuote);
            ShowCharacter(_loserPortrait, _loserQuoteText, loser.FrontSprite, loser.LoseQuote);
            _loserPortrait.color = _loserTint;
            _loserPortrait.rectTransform.localScale = new Vector3(1f, 1f - _loserSquash, 1f);

            _winnerBasePosition = _winnerPortrait.rectTransform.anchoredPosition;
            _elapsed = 0f;
            _isPlaying = true;
            gameObject.SetActive(true);

            // 負けたときまで紙吹雪を降らせると、どちらが勝ったのか一瞬で分からなくなるため
            _confetti.gameObject.SetActive(isPlayerWinner);
            if (isPlayerWinner)
            {
                _confetti.Play();
            }

            yield return new WaitForSeconds(_displayDuration);
        }

        public void Hide()
        {
            _isPlaying = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isPlaying) return;

            _elapsed += Time.deltaTime;

            float scale = PopInScale();
            float hop = Mathf.Abs(Mathf.Sin(_elapsed * _hopsPerSecond * Mathf.PI)) * _hopHeight;
            _winnerPortrait.rectTransform.localScale = new Vector3(scale, scale, 1f);
            _winnerPortrait.rectTransform.anchoredPosition = _winnerBasePosition + Vector2.up * hop;
        }

        /// <summary>0 → 行き過ぎ → 1 と弾ませて「飛び出してきた」ように見せる</summary>
        private float PopInScale()
        {
            float t = Mathf.Clamp01(_elapsed / _popInDuration);
            if (t < PopPeakRatio)
            {
                return Mathf.Lerp(0f, _popOvershoot, t / PopPeakRatio);
            }

            return Mathf.Lerp(_popOvershoot, 1f, (t - PopPeakRatio) / (1f - PopPeakRatio));
        }

        private static void ShowCharacter(Image portrait, Text quoteText, Sprite sprite, string quote)
        {
            portrait.sprite = sprite;
            quoteText.text = string.IsNullOrEmpty(quote) ? string.Empty : $"「{quote}」";
        }
    }
}
