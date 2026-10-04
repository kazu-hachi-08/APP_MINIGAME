using System;
using System.Collections.Generic;
using MiniGame.Common.Audio;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ペンギン大戦争のタイトル画面（仕様書 §2.0）。ロゴがふわふわ揺れ、ランダムなペンギンがぴょこぴょこ跳ねる。
    /// 何を押したらどこへ進むかは GameManager が決める
    /// </summary>
    public class PenguinWarsTitlePanel : MonoBehaviour
    {
        [SerializeField] private PenguinUnitCatalog _catalog;
        [SerializeField] private RectTransform _logo;
        [SerializeField] private Image[] _paradeIcons;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _zukanButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private PenguinWarsSettingsPanel _settingsPanel;
        [SerializeField] private PenguinZukanPanel _zukanPanel;

        [Header("動き")]
        [SerializeField] private float _logoBobHeight = 12f;
        [SerializeField] private float _logoBobSpeed = 2f;
        [SerializeField] private float _hopHeight = 28f;
        [SerializeField] private float _hopSpeed = 6f;
        [Tooltip("隣のペンギンと跳ねるタイミングをずらす量。揃うと機械的に見えるため")]
        [SerializeField] private float _hopPhaseStep = 0.8f;

        private Action _onStart;
        private Action _onBack;
        private Vector2 _logoBasePosition;
        private Vector2[] _iconBasePositions;

        private void Awake()
        {
            _startButton.onClick.AddListener(() => Close(_onStart));
            _backButton.onClick.AddListener(() => Close(_onBack));
            _settingsButton.onClick.AddListener(OpenSettings);
            _zukanButton.onClick.AddListener(OpenZukan);

            _logoBasePosition = _logo.anchoredPosition;
            _iconBasePositions = new Vector2[_paradeIcons.Length];
            for (int i = 0; i < _paradeIcons.Length; i++) _iconBasePositions[i] = _paradeIcons[i].rectTransform.anchoredPosition;
        }

        public void Show(Action onStart, Action onBack)
        {
            _onStart = onStart;
            _onBack = onBack;
            PickParade();
            _settingsPanel.Hide();
            _zukanPanel.Hide();
            gameObject.SetActive(true);
        }

        private void Update()
        {
            float time = Time.unscaledTime;
            _logo.anchoredPosition = _logoBasePosition + Vector2.up * (Mathf.Sin(time * _logoBobSpeed) * _logoBobHeight);
            for (int i = 0; i < _paradeIcons.Length; i++)
            {
                // Abs で地面に着いたら跳ね返る動きにする
                float hop = Mathf.Abs(Mathf.Sin(time * _hopSpeed + i * _hopPhaseStep)) * _hopHeight;
                _paradeIcons[i].rectTransform.anchoredPosition = _iconBasePositions[i] + Vector2.up * hop;
            }
        }

        /// <summary>毎回違う顔ぶれを見せたいので、開くたびに全キャラから重複なしで選び直す</summary>
        private void PickParade()
        {
            var units = new List<PenguinUnitData>(_catalog.Units);
            for (int i = 0; i < _paradeIcons.Length; i++)
            {
                Sprite icon = null;
                if (units.Count > 0)
                {
                    int index = UnityEngine.Random.Range(0, units.Count);
                    icon = units[index].GetSprites(Side.Left).Icon;
                    units.RemoveAt(index);
                }
                _paradeIcons[i].sprite = icon;
                _paradeIcons[i].enabled = icon != null;
            }
        }

        private void OpenSettings()
        {
            PlayClick();
            _settingsPanel.Show();
        }

        private void OpenZukan()
        {
            PlayClick();
            _zukanPanel.Show();
        }

        private void Close(Action next)
        {
            PlayClick();
            gameObject.SetActive(false);
            next?.Invoke();
        }

        private static void PlayClick()
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlaySe(SeId.ButtonClick);
        }
    }
}
