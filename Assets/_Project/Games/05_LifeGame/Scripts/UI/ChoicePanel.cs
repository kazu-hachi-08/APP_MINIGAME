using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 分岐・職業・家・保険・株の選択（仕様書 §3.3）。選択肢の数が場面ごとに違うので、見本のボタンを実行時に複製して並べる。
    /// </summary>
    public class ChoicePanel : MonoBehaviour
    {
        public const int Cancelled = -1;

        [SerializeField] private Text _titleText;
        [SerializeField] private Transform _optionRoot;
        [SerializeField] private Button _optionTemplate;
        [Tooltip("複数選択（保険）の決定ボタン")]
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Color _optionColor = new Color(0.3f, 0.33f, 0.4f);
        [SerializeField] private Color _selectedColor = new Color(0.2f, 0.6f, 0.9f);

        private readonly List<Button> _options = new List<Button>();
        private int _chosen;
        private bool _confirmed;

        private void Awake()
        {
            _optionTemplate.gameObject.SetActive(false);
            _confirmButton.onClick.AddListener(() => _confirmed = true);
        }

        /// <summary>1つ選ぶ。押せない選択肢は interactable を false にして出す（なぜ選べないかを見せるため）</summary>
        public IEnumerator ChooseOne(string title, IReadOnlyList<string> labels, IReadOnlyList<bool> enabled, Action<int> onChosen)
        {
            Open(title, labels, enabled);
            _confirmButton.gameObject.SetActive(false);
            _chosen = Cancelled;
            for (int i = 0; i < _options.Count; i++)
            {
                int index = i;
                _options[i].onClick.AddListener(() => _chosen = index);
            }

            yield return new WaitUntil(() => _chosen != Cancelled);

            Close();
            onChosen(_chosen);
        }

        /// <summary>いくつでも選んで「決定」で確定する（保険）。何も選ばずに決定してもよい</summary>
        public IEnumerator ChooseMany(string title, IReadOnlyList<string> labels, IReadOnlyList<bool> enabled, Action<bool[]> onChosen)
        {
            Open(title, labels, enabled);
            _confirmButton.gameObject.SetActive(true);
            _confirmed = false;
            var selected = new bool[labels.Count];
            for (int i = 0; i < _options.Count; i++)
            {
                int index = i;
                _options[i].onClick.AddListener(() =>
                {
                    selected[index] = !selected[index];
                    _options[index].image.color = selected[index] ? _selectedColor : _optionColor;
                });
            }

            yield return new WaitUntil(() => _confirmed);

            Close();
            onChosen(selected);
        }

        /// <summary>
        /// 相手の手番で同じ選択肢を見せる（オンライン）。リスナーを付けないので押しても何も起きない。
        /// interactable を切ると選んだ色まで灰色に沈むため、見た目は通常のままにしている
        /// </summary>
        public void ShowWatching(string title, IReadOnlyList<string> labels, IReadOnlyList<bool> enabled)
        {
            Open(title, labels, enabled);
            _confirmButton.gameObject.SetActive(false);
        }

        /// <summary>相手が何を選んだかを色で見せてから閉じる（結果が急に流れて置いていかれないように）</summary>
        public IEnumerator RevealAndClose(IReadOnlyList<bool> selected, float seconds)
        {
            for (int i = 0; i < _options.Count && i < selected.Count; i++)
            {
                if (selected[i]) _options[i].image.color = _selectedColor;
            }

            yield return new WaitForSeconds(seconds);
            Close();
        }

        private void Open(string title, IReadOnlyList<string> labels, IReadOnlyList<bool> enabled)
        {
            _titleText.text = title;
            for (int i = 0; i < labels.Count; i++)
            {
                Button option = Instantiate(_optionTemplate, _optionRoot);
                option.GetComponentInChildren<Text>().text = labels[i];
                option.image.color = _optionColor;
                option.interactable = enabled == null || enabled[i];
                option.gameObject.SetActive(true);
                _options.Add(option);
            }

            gameObject.SetActive(true);
        }

        private void Close()
        {
            foreach (Button option in _options) Destroy(option.gameObject);
            _options.Clear();
            gameObject.SetActive(false);
        }
    }
}
