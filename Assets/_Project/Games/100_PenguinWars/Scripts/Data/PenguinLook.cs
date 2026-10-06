using System;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 見た目のパーツ指定（仕様書 §7.2）。50体を手描きせず「基本ペンギン＋パーツ」で作るため、キャラごとに持つのは ID 文字列だけ。
    /// パーツの中身は Editor/Art の PenguinBodyPatterns / PenguinPartPatterns にある
    /// </summary>
    [Serializable]
    public class PenguinLook
    {
        public const string DefaultBody = "basic";
        public const string DefaultBodyColor = "standard";
        public const int MaxScale = 3;

        [Tooltip("体の形（basic / wide / tall …）")]
        [SerializeField] private string _body = DefaultBody;
        [Tooltip("体色（standard / ice / gray …）")]
        [SerializeField] private string _bodyColor = DefaultBodyColor;
        [Tooltip("頭パーツ。空ならなし")]
        [SerializeField] private string _head = string.Empty;
        [Tooltip("手に持つパーツ。空ならなし")]
        [SerializeField] private string _hand = string.Empty;
        [Tooltip("背中・乗り物パーツ。空ならなし")]
        [SerializeField] private string _back = string.Empty;
        [Tooltip("拡大率。大型キャラはドットのまま 2倍・3倍に引き伸ばす")]
        [SerializeField, Range(1, MaxScale)] private int _scale = 1;

        public string Body => string.IsNullOrEmpty(_body) ? DefaultBody : _body;
        public string BodyColor => string.IsNullOrEmpty(_bodyColor) ? DefaultBodyColor : _bodyColor;
        public string Head => _head;
        public string Hand => _hand;
        public string Back => _back;
        public int Scale => Mathf.Clamp(_scale, 1, MaxScale);

        /// <summary>何も指定していない（基本ペンギンのまま）か。生成メニューが見た目を書き込んでよいかの判定に使う</summary>
        public bool IsDefault => Body == DefaultBody && BodyColor == DefaultBodyColor &&
                                 string.IsNullOrEmpty(_head) && string.IsNullOrEmpty(_hand) &&
                                 string.IsNullOrEmpty(_back) && Scale == 1;

        public PenguinLook()
        {
        }

        public PenguinLook(string body = DefaultBody, string bodyColor = DefaultBodyColor, string head = "",
            string hand = "", string back = "", int scale = 1)
        {
            _body = body;
            _bodyColor = bodyColor;
            _head = head;
            _hand = hand;
            _back = back;
            _scale = scale;
        }
    }
}
