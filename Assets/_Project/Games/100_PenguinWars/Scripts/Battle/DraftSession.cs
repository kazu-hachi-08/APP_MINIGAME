using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// オンライン対戦前のドラフト（仕様書 §6）。各ラウンドで両者に別々の候補を出し、両者が1体ずつ選んだら次へ進む。
    /// 抽選・時間計測はホストだけが行う（乱数のずれを防ぐため）。通信は知らないので、候補の送り先は呼び出し側が決める。
    /// 陣営はホスト基準（Left = ホスト、Right = ゲスト）
    /// </summary>
    public class DraftSession
    {
        private class Seat
        {
            public readonly List<int> Offer = new List<int>();
            public readonly List<int> Picks = new List<int>();
            public bool HasPicked;
        }

        private readonly IReadOnlyList<int> _pool;
        private readonly int _offerSize;
        private readonly float _pickTime;
        private readonly Random _random;
        private readonly Seat _left = new Seat();
        private readonly Seat _right = new Seat();

        /// <summary>今のラウンド（0始まり）。全ラウンド終わると TotalRounds と同じになる</summary>
        public int Round { get; private set; }
        public int TotalRounds { get; }
        /// <summary>今のラウンドの残り秒。0 になったら選んでいない側は候補からランダムに決まる</summary>
        public float RemainingTime { get; private set; }
        public bool IsComplete => Round >= TotalRounds;

        /// <param name="poolUnitNos">候補にするキャラNo（全キャラ）</param>
        public DraftSession(IReadOnlyList<int> poolUnitNos, int totalRounds, int offerSize, float pickTime, Random random)
        {
            _pool = poolUnitNos;
            TotalRounds = totalRounds;
            _offerSize = offerSize;
            _pickTime = pickTime;
            _random = random;
            if (!IsComplete) StartRound();
        }

        public IReadOnlyList<int> GetOffer(Side side) => GetSeat(side).Offer;

        /// <summary>選んだ順（= 編成の枠の順）</summary>
        public IReadOnlyList<int> GetPicks(Side side) => GetSeat(side).Picks;

        public bool HasPicked(Side side) => GetSeat(side).HasPicked;

        /// <summary>候補の何番目を選ぶか。このラウンドで選び済み・範囲外なら受け付けない</summary>
        public bool Pick(Side side, int offerIndex)
        {
            Seat seat = GetSeat(side);
            if (IsComplete || seat.HasPicked || offerIndex < 0 || offerIndex >= seat.Offer.Count) return false;

            seat.Picks.Add(seat.Offer[offerIndex]);
            seat.HasPicked = true;
            return true;
        }

        /// <summary>時間を進める。両者が選び終わってラウンドが進んだら true（新しい候補を配る合図）</summary>
        public bool Tick(float deltaTime)
        {
            if (IsComplete) return false;

            RemainingTime -= deltaTime;
            if (RemainingTime <= 0f)
            {
                PickRandomIfWaiting(Side.Left);
                PickRandomIfWaiting(Side.Right);
            }
            if (!_left.HasPicked || !_right.HasPicked) return false;

            Round++;
            if (!IsComplete) StartRound();
            return true;
        }

        private void PickRandomIfWaiting(Side side)
        {
            Seat seat = GetSeat(side);
            if (seat.HasPicked) return;

            // 候補が空になることは50体・10ラウンドでは起きないが、起きても進行が止まらないよう選んだ扱いにする
            if (seat.Offer.Count == 0) seat.HasPicked = true;
            else Pick(side, _random.Next(seat.Offer.Count));
        }

        private void StartRound()
        {
            RemainingTime = _pickTime;
            Deal(_left);
            Deal(_right);
        }

        /// <summary>自分が取ったキャラは出さない。相手が取ったキャラは出してよい（被りあり。仕様書 §6）</summary>
        private void Deal(Seat seat)
        {
            var candidates = new List<int>();
            foreach (int unitNo in _pool)
            {
                if (!seat.Picks.Contains(unitNo)) candidates.Add(unitNo);
            }
            seat.Offer.Clear();
            seat.Offer.AddRange(DeckRandomizer.Pick(candidates, _offerSize, _random));
            seat.HasPicked = false;
        }

        private Seat GetSeat(Side side) => side == Side.Left ? _left : _right;
    }
}
