using System;
using System.Collections.Generic;
using System.Linq;
using CardGame.Core.Definitions;
using CardGame.Core.State;

namespace CardGame.Core.Draft
{
    /// <summary>コストの帯(07-draft.md「組の作り方」)。</summary>
    public enum CostBand
    {
        /// <summary>低: コスト 1〜2(0 も含む)。</summary>
        Low,
        /// <summary>中: コスト 3〜4。</summary>
        Mid,
        /// <summary>高: コスト 5〜6。</summary>
        High,
        /// <summary>大型: コスト 7 以上。</summary>
        Top,
    }

    /// <summary>
    /// ドラフト 1 回分の進行(07-draft.md)。10 回、3 枚 1 組 × 3 組から 1 組を取り、30 枚の混沌デッキを作る。
    /// 10 回分の提示はシードだけで最初に全て決まる(選び方に左右されない)。オンラインでは同じシードで 2 人に同じ組を出す。
    /// 同じカードは 10 回の提示全体で 3 回までしか出さないので、どう選んでも同名 3 枚の制限を超えない。
    /// </summary>
    public sealed class DraftSession
    {
        /// <summary>ピックの回数。</summary>
        public const int TotalPicks = 10;
        /// <summary>1 回に提示する組の数。</summary>
        public const int OptionCount = 3;
        /// <summary>1 組の枚数。</summary>
        public const int BundleSize = 3;
        /// <summary>1 回の制限時間(秒)。時間切れの処理は UI 側で <see cref="PickRandom"/> を呼ぶ。</summary>
        public const float SecondsPerPick = 30f;

        private static readonly CostBand[][] Templates =
        {
            new[] { CostBand.Low, CostBand.Mid, CostBand.High },
            new[] { CostBand.Low, CostBand.Mid, CostBand.Top },
            new[] { CostBand.Mid, CostBand.Mid, CostBand.Top },
        };
        /// <summary>各型を何回使うか(Templates と同じ順)。合計 10 回、低 9 / 中 11 / 高 6 / 大型 4 枚になる。</summary>
        private static readonly int[] TemplateCounts = { 6, 3, 1 };

        private readonly DeterministicRandom _rng;
        private readonly DeterministicRandom _pickRng;     // 時間切れのランダムピック用(提示とは別系統)
        private readonly Dictionary<CostBand, List<CardDefinition>> _pool;
        private readonly List<string> _picked = new List<string>();
        private readonly List<IReadOnlyList<IReadOnlyList<CardDefinition>>> _offers = new List<IReadOnlyList<IReadOnlyList<CardDefinition>>>();
        private readonly Dictionary<string, int> _appearances = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>何回目のピックか(0 始まり)。完了後は <see cref="TotalPicks"/>。</summary>
        public int PickIndex { get; private set; }
        /// <summary>今回の 3 組(完了後は空)。</summary>
        public IReadOnlyList<IReadOnlyList<CardDefinition>> CurrentOffer
            => IsComplete ? Array.Empty<IReadOnlyList<CardDefinition>>() : _offers[PickIndex];
        /// <summary>10 回分の提示(0 始まり)。</summary>
        public IReadOnlyList<IReadOnlyList<IReadOnlyList<CardDefinition>>> AllOffers => _offers;
        /// <summary>取ったカード ID(取った順)。</summary>
        public IReadOnlyList<string> Picked => _picked;
        /// <summary>10 回選び終えたか。</summary>
        public bool IsComplete => PickIndex >= TotalPicks;

        public DraftSession(CardDatabase db, ulong seed)
        {
            _rng = new DeterministicRandom(seed);
            _pickRng = new DeterministicRandom(seed ^ 0xD4AF7UL);
            // トークン以外の全カード(クラス縛りなし)。ID 順に並べて列挙順を決定論にする
            _pool = db.All.Where(c => !c.IsToken).OrderBy(c => c.Id, StringComparer.Ordinal)
                .GroupBy(c => BandOf(c.Cost))
                .ToDictionary(g => g.Key, g => g.ToList());
            foreach (CostBand b in Enum.GetValues(typeof(CostBand)))
                if (!_pool.ContainsKey(b)) _pool[b] = new List<CardDefinition>();

            var schedule = new List<CostBand[]>();
            for (int i = 0; i < Templates.Length; i++)
                for (int n = 0; n < TemplateCounts[i]; n++)
                    schedule.Add(Templates[i]);
            _rng.Shuffle(schedule);
            foreach (var template in schedule) _offers.Add(MakeOffer(template));
        }

        /// <summary>コストから帯を決める。</summary>
        public static CostBand BandOf(int cost) => cost <= 2 ? CostBand.Low : cost <= 4 ? CostBand.Mid : cost <= 6 ? CostBand.High : CostBand.Top;

        /// <summary>今回の組 <paramref name="index"/>(0〜2)を取る。</summary>
        public void Pick(int index)
        {
            if (IsComplete) throw new InvalidOperationException("ドラフトは完了している");
            var offer = _offers[PickIndex];
            if (index < 0 || index >= offer.Count) throw new ArgumentOutOfRangeException(nameof(index));
            _picked.AddRange(offer[index].Select(c => c.Id));
            PickIndex++;
        }

        /// <summary>時間切れ: ランダムに 1 組を取る。取った組の番号を返す。</summary>
        public int PickRandom()
        {
            int i = _pickRng.Next(CurrentOffer.Count);
            Pick(i);
            return i;
        }

        /// <summary>完成した 30 枚の混沌デッキ。</summary>
        public DeckDefinition ToDeck(string name = "ドラフトデッキ")
        {
            if (!IsComplete) throw new InvalidOperationException("ドラフトが完了していない");
            return new DeckDefinition(name, CardClass.Chaos, _picked);
        }

        private List<IReadOnlyList<CardDefinition>> MakeOffer(CostBand[] template)
        {
            var offer = new List<IReadOnlyList<CardDefinition>>();
            var usedThisPick = new HashSet<string>();   // 同じ回の 9 枚で重複させない
            for (int o = 0; o < OptionCount; o++)
            {
                var bundle = new List<CardDefinition>();
                foreach (var band in template)
                    bundle.Add(Draw(band, usedThisPick));
                offer.Add(bundle);
            }
            return offer;
        }

        /// <summary>帯から 1 枚引く。候補が無ければ近い帯から補う(同じ距離なら重い方を先に)。</summary>
        private CardDefinition Draw(CostBand band, HashSet<string> usedThisPick)
        {
            foreach (var b in FallbackOrder(band))
            {
                var candidates = _pool[b]
                    .Where(c => !usedThisPick.Contains(c.Id) && _appearances.GetValueOrDefault(c.Id) < DeckDefinition.MaxCopies)
                    .ToList();
                if (candidates.Count == 0) continue;
                var card = candidates[_rng.Next(candidates.Count)];
                usedThisPick.Add(card.Id);
                _appearances[card.Id] = _appearances.GetValueOrDefault(card.Id) + 1;
                return card;
            }
            throw new InvalidOperationException("提示できるカードが無い(カードプールが小さすぎる)");
        }

        private static IEnumerable<CostBand> FallbackOrder(CostBand band)
        {
            int b = (int)band;
            yield return band;
            // 近い帯から。同じ距離なら重い方(大型寄り)を先に見る
            for (int d = 1; d <= 3; d++)
            {
                if (b + d <= (int)CostBand.Top) yield return (CostBand)(b + d);
                if (b - d >= (int)CostBand.Low) yield return (CostBand)(b - d);
            }
        }
    }
}
