using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// イベント表示1回分の中身。空の欄は表示しない
    /// （選択の結果だけのときはマスの欄が無い・順位発表は名札が無い）
    /// </summary>
    public class EventPopupContent
    {
        public const int NoSeat = -1;

        public int Seat = NoSeat;
        public string PlayerName;
        public Sprite Face;
        public Color SeatColor;

        /// <summary>止まったマスの名前か「順位発表」。null ならタイトルの欄を出さない</summary>
        public string Title;
        public Sprite Icon;
        public Color IconColor;
        public string Flavor;

        public readonly List<(string text, Color color)> Lines = new List<(string text, Color color)>();

        public bool HasNameplate => Seat != NoSeat;

        /// <summary>名札しか無いとき（次の人の手番が始まっただけ）は出さない</summary>
        public bool IsEmpty => Title == null && Lines.Count == 0;
    }
}
