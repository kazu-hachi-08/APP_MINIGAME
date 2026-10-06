using System;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ステージ選択のマス1つ。クリア済み・遊べる・ロックで色を変え、ボスステージは大きく赤くして章の山場だと分かるようにする。
    /// 遊べる最新のマスはふわふわ上下させ、新しく遊べるようになったマスは鍵が外れる演出を出す（仕様書 §9）
    /// </summary>
    public class StageNode : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Text _idLabel;
        [SerializeField] private Text _starsLabel;
        [SerializeField] private GameObject _lockMark;
        [SerializeField] private GameObject _bossMark;
        [SerializeField] private Color _clearedColor = new Color(0.2f, 0.5f, 0.85f);
        [SerializeField] private Color _playableColor = new Color(0.95f, 0.55f, 0.15f);
        [SerializeField] private Color _lockedColor = new Color(0.25f, 0.27f, 0.32f);
        [SerializeField] private Color _bossColor = new Color(0.8f, 0.2f, 0.25f);
        [SerializeField] private float _bossScale = 1.2f;

        [Header("ふわふわ・鍵が外れる演出")]
        [Tooltip("見た目の本体。並び（HorizontalLayoutGroup）が決める自分の位置は動かさず、本体だけを動かす")]
        [SerializeField] private RectTransform _body;
        [SerializeField] private PenguinWarsAudio _audio;
        [SerializeField] private float _floatHeight = 12f;
        [SerializeField] private float _floatSeconds = 1.2f;
        [SerializeField] private float _unlockDuration = 0.6f;
        [Tooltip("外れた鍵がこの倍率まで大きくなりながら消える")]
        [SerializeField] private float _lockScaleUp = 2.2f;

        private bool _isLatest;
        private bool _unlocking;
        private float _unlockElapsed;
        private float _unlockDelay;
        private Color _unlockedColor;

        public string StageId { get; private set; }

        /// <param name="isLatest">遊べるステージのうち一番先のもの（次に遊ぶ所）。ふわふわ動かして目立たせる</param>
        public void Show(StageDefinition stage, CampaignProgress progress, bool isLatest, Action<StageDefinition> onClick)
        {
            bool isPlayable = progress.IsPlayable(stage.Id);
            bool isCleared = progress.IsCleared(stage.Id);

            StageId = stage.Id;
            _idLabel.text = stage.Id;
            _starsLabel.text = StageLabels.Stars(progress.GetStars(stage.Id));
            _starsLabel.gameObject.SetActive(isPlayable);
            _lockMark.SetActive(!isPlayable);
            ResetLockMark();
            _bossMark.SetActive(stage.IsBossStage);
            _background.color = PickColor(stage, isPlayable, isCleared);
            transform.localScale = Vector3.one * (stage.IsBossStage ? _bossScale : 1f);
            _isLatest = isLatest;
            _unlocking = false;
            _body.anchoredPosition = Vector2.zero;

            _button.interactable = isPlayable;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick(stage));
            gameObject.SetActive(true);
        }

        /// <summary>いったんロックの見た目に戻し、delay 秒後に鍵が外れる。Show の後に呼ぶ</summary>
        public void PlayUnlock(float delay)
        {
            _unlockedColor = _background.color;
            _background.color = _lockedColor;
            _lockMark.SetActive(true);
            _starsLabel.gameObject.SetActive(false);
            _button.interactable = false;
            _unlockDelay = delay;
            _unlockElapsed = 0f;
            _unlocking = true;
        }

        public void Hide() => gameObject.SetActive(false);

        private void Update()
        {
            if (_unlocking) UpdateUnlock();
            else if (_isLatest) _body.anchoredPosition = Vector2.up * (Mathf.Sin(Time.unscaledTime / _floatSeconds * 2f * Mathf.PI) * _floatHeight);
        }

        private void UpdateUnlock()
        {
            float previous = _unlockElapsed;
            _unlockElapsed += Time.unscaledDeltaTime;
            if (_unlockElapsed < _unlockDelay) return;
            if (previous < _unlockDelay) _audio.PlayUnlock();

            float rate = Mathf.Clamp01((_unlockElapsed - _unlockDelay) / _unlockDuration);
            _lockMark.transform.localScale = Vector3.one * Mathf.Lerp(1f, _lockScaleUp, rate);
            SetLockAlpha(1f - rate);
            _background.color = Color.Lerp(_lockedColor, _unlockedColor, rate);
            if (rate >= 1f) FinishUnlock();
        }

        private void FinishUnlock()
        {
            _unlocking = false;
            _lockMark.SetActive(false);
            ResetLockMark();
            _starsLabel.gameObject.SetActive(true);
            _button.interactable = true;
        }

        private void ResetLockMark()
        {
            _lockMark.transform.localScale = Vector3.one;
            SetLockAlpha(1f);
        }

        private void SetLockAlpha(float alpha)
        {
            var graphic = _lockMark.GetComponent<Graphic>();
            if (graphic == null) return;

            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        /// <summary>まだ遊んでいないステージを一番目立たせ、次に何を遊べばいいかすぐ分かるようにする</summary>
        private Color PickColor(StageDefinition stage, bool isPlayable, bool isCleared)
        {
            if (!isPlayable) return _lockedColor;
            if (!isCleared) return _playableColor;
            return stage.IsBossStage ? _bossColor : _clearedColor;
        }
    }
}
