using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 1位発表の紙吹雪（計画 Phase 7 ⑥）。パーティクルは使わず、UI の Image をばらまいて落とすだけにする。
    /// 入力は奥へ通し（raycastTarget なし）、勝利演出の上に重ねたまま降らせられるようにする。
    /// </summary>
    public class ConfettiView : MonoBehaviour
    {
        [SerializeField] private int _pieceCount = 60;
        [SerializeField] private Vector2 _pieceSize = new Vector2(24f, 36f);
        [SerializeField] private float _duration = 4f;

        [Header("Motion（UI単位/秒）")]
        [SerializeField] private Vector2 _fallSpeed = new Vector2(300f, 600f);
        [SerializeField] private float _swayWidth = 60f;
        [SerializeField] private Vector2 _swaySpeed = new Vector2(2f, 5f);
        [SerializeField] private Vector2 _spinSpeed = new Vector2(-360f, 360f);
        [Tooltip("降り始めの高さをばらつかせる幅。全部が同時に画面へ入ると板のように見えるため")]
        [SerializeField] private float _startSpread = 600f;

        private sealed class Piece
        {
            public RectTransform Rect;
            public float X;
            public float Y;
            public float Fall;
            public float SwayPhase;
            public float SwaySpeed;
            public float Spin;
        }

        private readonly List<Piece> _pieces = new List<Piece>();

        /// <summary>
        /// 自分の上でコルーチンを回して降らせる。呼んだ側の View が先に隠れても止まらないようにするため
        /// （1位の発表で降らせ始め、勝利演出の間も降り続ける）
        /// </summary>
        public void Burst(IReadOnlyList<Color> colors)
        {
            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(Play(colors));
        }

        private IEnumerator Play(IReadOnlyList<Color> colors)
        {
            Rect area = ((RectTransform)transform).rect;
            EnsurePieces();
            foreach (Piece piece in _pieces) Scatter(piece, area, colors);

            for (float t = 0f; t < _duration; t += Time.deltaTime)
            {
                foreach (Piece piece in _pieces) Move(piece, area, t);
                yield return null;
            }

            gameObject.SetActive(false);
        }

        private void EnsurePieces()
        {
            while (_pieces.Count < _pieceCount)
            {
                var obj = new GameObject("Piece", typeof(RectTransform), typeof(Image));
                obj.transform.SetParent(transform, false);
                obj.GetComponent<Image>().raycastTarget = false;
                var rect = (RectTransform)obj.transform;
                rect.sizeDelta = _pieceSize;
                _pieces.Add(new Piece { Rect = rect });
            }
        }

        /// <summary>見た目のばらつきだけなので、ルールの乱数ではなく UnityEngine.Random を使う（オンラインで揃える必要がない）</summary>
        private void Scatter(Piece piece, Rect area, IReadOnlyList<Color> colors)
        {
            piece.X = Random.Range(area.xMin, area.xMax);
            piece.Y = area.yMax + Random.Range(0f, _startSpread);
            piece.Fall = Random.Range(_fallSpeed.x, _fallSpeed.y);
            piece.SwayPhase = Random.Range(0f, Mathf.PI * 2f);
            piece.SwaySpeed = Random.Range(_swaySpeed.x, _swaySpeed.y);
            piece.Spin = Random.Range(_spinSpeed.x, _spinSpeed.y);
            piece.Rect.GetComponent<Image>().color = colors[Random.Range(0, colors.Count)];
        }

        /// <summary>下まで落ちたら上へ戻し、演出の間は降り続けるようにする</summary>
        private void Move(Piece piece, Rect area, float time)
        {
            piece.Y -= piece.Fall * Time.deltaTime;
            if (piece.Y < area.yMin - _pieceSize.y) piece.Y = area.yMax + _pieceSize.y;

            float sway = Mathf.Sin(piece.SwayPhase + time * piece.SwaySpeed) * _swayWidth;
            piece.Rect.anchoredPosition = new Vector2(piece.X + sway, piece.Y);
            piece.Rect.localEulerAngles = new Vector3(0f, 0f, piece.Spin * time);
        }
    }
}
