using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>§14.2 試合開始時にホストが決めて配る、ホールの並び（カタログの添字）と各ホールの風</summary>
    public sealed class GolfMatchSetup
    {
        public GolfMatchSetup(IReadOnlyList<int> holeIndices, IReadOnlyList<Wind> winds)
        {
            HoleIndices = holeIndices;
            Winds = winds;
        }

        public IReadOnlyList<int> HoleIndices { get; }
        public IReadOnlyList<Wind> Winds { get; }
    }

    /// <summary>
    /// 打った瞬間の入力。他の端末でボールが飛ぶ演出を再生するためだけに使う。
    /// ClubConfig はアセットなので送らず、ClubSelector の並びの添字で送る
    /// </summary>
    public readonly struct GolfShotMessage
    {
        public GolfShotMessage(Vector2 direction, int clubIndex, float power, float impactOffset)
        {
            Direction = direction;
            ClubIndex = clubIndex;
            Power = power;
            ImpactOffset = impactOffset;
        }

        public Vector2 Direction { get; }
        public int ClubIndex { get; }
        public float Power { get; }
        public float ImpactOffset { get; }
    }

    /// <summary>§14.2 止まった後に打った人の端末が確定させる結果。他の端末はこれで上書きする</summary>
    public readonly struct GolfShotResultMessage
    {
        public GolfShotResultMessage(Vector2 position, int strokes, bool isInCup)
        {
            Position = position;
            Strokes = strokes;
            IsInCup = isInCup;
        }

        /// <summary>池・OBのときは打ち直しの位置</summary>
        public Vector2 Position { get; }

        /// <summary>罰打を含む今のホールの打数</summary>
        public int Strokes { get; }

        public bool IsInCup { get; }
    }
}
