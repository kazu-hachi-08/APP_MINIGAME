using System.Collections.Generic;
using System.Numerics;

namespace MiniGame.Golf
{
    /// <summary>
    /// 1人分の人間/NPC・ボール位置・今のホールの打数・各ホールのスコア（§15）。
    /// ボールの実体（GolfBall）は1つだけで手番の人の位置に置き直すので、止まっている位置はここで覚えておく。
    /// </summary>
    public sealed class GolfPlayerSlot
    {
        private readonly List<int> _holeScores = new List<int>();

        public GolfPlayerSlot(int seat, GolfPlayerType type = GolfPlayerType.Human, int characterIndex = 0)
        {
            Seat = seat;
            Type = type;
            CharacterIndex = characterIndex;
        }

        /// <summary>席順（0始まり）。P1〜P4 の表示と色に使う</summary>
        public int Seat { get; }

        public GolfPlayerType Type { get; }
        public bool IsNpc => Type != GolfPlayerType.Human;

        /// <summary>GolfCharacterCatalog の番号。アセットではなく番号にして、オンラインでは番号だけ送れば済むようにする</summary>
        public int CharacterIndex { get; }

        public Vector2 Position { get; set; }

        /// <summary>今のホールの打数（罰打を含む）</summary>
        public int Strokes { get; private set; }

        public bool IsHoledOut { get; private set; }
        public bool IsGivenUp { get; private set; }

        /// <summary>カップインか打ち切りで、このホールはもう打たない</summary>
        public bool IsFinished => IsHoledOut || IsGivenUp;

        /// <summary>終わったホールのスコア（打ち切りはパー×2）</summary>
        public IReadOnlyList<int> HoleScores => _holeScores;

        public int Total
        {
            get
            {
                int total = 0;
                foreach (int score in _holeScores) total += score;
                return total;
            }
        }

        public void StartHole(Vector2 teePosition)
        {
            Position = teePosition;
            Strokes = 0;
            IsHoledOut = false;
            IsGivenUp = false;
        }

        public void AddStrokes(int strokes)
        {
            Strokes += strokes;
        }

        /// <summary>§14.2 オンラインでは打った人の端末の打数（罰打を含む）で上書きする</summary>
        public void SetStrokes(int strokes)
        {
            Strokes = strokes;
        }

        public void HoleOut()
        {
            IsHoledOut = true;
            _holeScores.Add(Strokes);
        }

        /// <summary>§6.4 打ち切りの打数は、罰打で上限を超えていても上限として記録する</summary>
        public void GiveUp(int recordedStrokes)
        {
            IsGivenUp = true;
            _holeScores.Add(recordedStrokes);
        }
    }
}
