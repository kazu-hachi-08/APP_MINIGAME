using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>称号カードに顔を出す1人ぶん</summary>
    public struct TitleWinner
    {
        public Sprite Face;
        public string Name;
        public Color Color;
    }

    /// <summary>
    /// 称号発表（計画 Phase 7 ③）。カードを1枚ずつめくり、もらった人の顔を「+100」付きで飛び出させる。
    /// 演出の途中でタップするとそのカードの最後の状態まで飛ばし、もう一度タップ（または一定時間）で次のカードへ進む。
    /// </summary>
    public class TitleRevealView : MonoBehaviour
    {
        [SerializeField] private Button _tapArea;
        [SerializeField] private RectTransform _card;
        [SerializeField] private GameObject _cardBack;
        [SerializeField] private GameObject _cardFront;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _detailText;
        [SerializeField] private LifeAudio _audio;

        [Header("Winner slots（席の数だけ並べる）")]
        [SerializeField] private RectTransform[] _slots;
        [SerializeField] private Image[] _slotFaces;
        [SerializeField] private Text[] _slotNames;
        [SerializeField] private Text[] _slotBonuses;

        [Header("Timing (sec)")]
        [SerializeField] private float _beforeFlip = 0.3f;
        [SerializeField] private float _flipDuration = 0.4f;
        [SerializeField] private float _popDuration = 0.25f;
        [SerializeField] private float _popInterval = 0.15f;
        [Tooltip("全部出し切ってから次のカードへ自動で進むまで。タップでも進める")]
        [SerializeField] private float _holdDuration = 1.5f;

        [Header("Motion")]
        [SerializeField] private float _popOvershoot = 1.25f;
        [Tooltip("「+100」が顔の上へ浮き上がる量（UI単位）")]
        [SerializeField] private float _bonusRise = 40f;

        private bool _tapped;
        private Vector2[] _bonusBase;

        private void Awake()
        {
            _tapArea.onClick.AddListener(() => _tapped = true);
            _bonusBase = new Vector2[_slotBonuses.Length];
            for (int i = 0; i < _slotBonuses.Length; i++) _bonusBase[i] = _slotBonuses[i].rectTransform.anchoredPosition;
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>カード1枚ぶん。先に Show() で表示しておく</summary>
        public IEnumerator Play(string title, string detail, IReadOnlyList<TitleWinner> winners, string bonus)
        {
            Setup(title, detail, winners, bonus);

            yield return Wait(_beforeFlip);
            _audio.PlayStep();
            yield return Animate(_flipDuration, Flip);
            for (int i = 0; i < winners.Count; i++)
            {
                _audio.PlayPop();
                int slot = i;
                yield return Animate(_popDuration, t => Pop(slot, t));
                yield return Wait(_popInterval);
            }

            // ここまでのタップは「飛ばす」に使ったので、次へ進むタップは改めて待つ
            _tapped = false;
            yield return Wait(_holdDuration);
        }

        private void Setup(string title, string detail, IReadOnlyList<TitleWinner> winners, string bonus)
        {
            _titleText.text = title;
            _detailText.text = detail;
            for (int i = 0; i < _slots.Length; i++)
            {
                bool used = i < winners.Count;
                _slots[i].gameObject.SetActive(used);
                _slots[i].localScale = Vector3.zero;
                if (!used) continue;

                _slotFaces[i].sprite = winners[i].Face;
                _slotNames[i].text = winners[i].Name;
                _slotNames[i].color = winners[i].Color;
                _slotBonuses[i].text = bonus;
            }

            Flip(0f);
            _tapped = false;
        }

        /// <summary>前半で裏を横に潰し、後半で表を広げて、カードがめくれたように見せる</summary>
        private void Flip(float t)
        {
            bool front = t >= 0.5f;
            _cardBack.SetActive(!front);
            _cardFront.SetActive(front);
            float width = front ? (t - 0.5f) * 2f : 1f - t * 2f;
            _card.localScale = new Vector3(width, 1f, 1f);
        }

        /// <summary>少し大きくなってから等倍に戻り、「+100」は顔の上へ浮き上がる</summary>
        private void Pop(int slot, float t)
        {
            float scale = t < 0.6f ? Mathf.Lerp(0f, _popOvershoot, t / 0.6f) : Mathf.Lerp(_popOvershoot, 1f, (t - 0.6f) / 0.4f);
            _slots[slot].localScale = Vector3.one * scale;
            _slotBonuses[slot].rectTransform.anchoredPosition = _bonusBase[slot] + Vector2.up * (_bonusRise * t);
        }

        /// <summary>経過の割合（0〜1）を apply に渡し続ける。タップされたら最後の状態にして抜ける</summary>
        private IEnumerator Animate(float duration, Action<float> apply)
        {
            for (float t = 0f; t < duration && !_tapped; t += Time.deltaTime)
            {
                apply(t / duration);
                yield return null;
            }

            apply(1f);
        }

        private IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds && !_tapped; t += Time.deltaTime) yield return null;
        }
    }
}
