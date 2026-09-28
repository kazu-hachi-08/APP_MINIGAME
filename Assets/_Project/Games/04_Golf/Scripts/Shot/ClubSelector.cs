using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 今持っているクラブ。
    /// グリーン上では自動でパターにし、それ以外ではカップまでの距離に届く一番短いクラブを初期値にする。
    /// </summary>
    public class ClubSelector : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;
        [SerializeField] private GolfClubData[] _clubs;

        private int _index;

        public GolfClubData Current => _clubs[_index];

        /// <summary>オンラインで打ったクラブを送るための添字（全端末で同じ並び）</summary>
        public int CurrentIndex => _index;

        /// <summary>グリーン上ではパター以外を選べない</summary>
        public bool CanChange => !IsOnGreen;

        private bool IsOnGreen => _ball.Ground == GroundType.Green;

        /// <summary>ボールが止まって次のショットを構えるときに呼ぶ</summary>
        public void SelectDefault()
        {
            _index = IsOnGreen ? PutterIndex() : ShortestReachingIndex();
        }

        /// <summary>オンラインで他の人が打ったクラブに合わせる。グリーン上の制限は打った人の端末で済んでいるので見ない</summary>
        public void Select(int index)
        {
            _index = Mathf.Clamp(index, 0, _clubs.Length - 1);
        }

        public void Next()
        {
            if (!CanChange) return;

            _index = (_index + 1) % _clubs.Length;
        }

        /// <summary>フルパワーでまっすぐ打ったときの距離（ユニット）。パターは転がる距離</summary>
        public float MaxDistance(GolfClubData club, Vector2 direction)
        {
            return Vector2.Distance(_ball.GroundPosition, _ball.PredictFullPower(club.Config, direction));
        }

        private int PutterIndex()
        {
            for (int i = 0; i < _clubs.Length; i++)
            {
                if (_clubs[i].IsPutter) return i;
            }

            return 0;
        }

        /// <summary>届くクラブが無ければ一番飛ぶクラブにする</summary>
        private int ShortestReachingIndex()
        {
            Vector2 toCup = _ball.CupPosition - _ball.GroundPosition;
            float distanceToCup = toCup.magnitude;

            int shortest = -1;
            int longest = 0;
            float shortestDistance = float.MaxValue;
            float longestDistance = 0f;

            for (int i = 0; i < _clubs.Length; i++)
            {
                if (_clubs[i].IsPutter) continue;

                float distance = MaxDistance(_clubs[i], toCup);
                if (distance >= distanceToCup && distance < shortestDistance)
                {
                    shortest = i;
                    shortestDistance = distance;
                }

                if (distance > longestDistance)
                {
                    longest = i;
                    longestDistance = distance;
                }
            }

            return shortest >= 0 ? shortest : longest;
        }
    }
}
