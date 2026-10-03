using System.Collections.Generic;

namespace MiniGame.LifeGame
{
    public sealed class LifePlayerState
    {
        public const int NotGoaled = -1;

        public int Seat;
        public int Money;
        public int Position = LifeBoard.StartIndex;
        public int JobId = LifeRuleConfig.NoJob;
        public int HouseId = LifeRuleConfig.NoHouse;
        public LifeInsurance Insurances;

        // 生命保険は精算で払った額の半分が返るので、払った額を覚えておく
        public int LifeInsurancePaid;

        /// <summary>持っている番号株（1〜10）</summary>
        public List<int> Stocks = new List<int>();

        /// <summary>約束手形の枚数</summary>
        public int Notes;

        public LifeAbility Ability;

        /// <summary>振り直しを使い切ったか（らっきーの能力は1試合に1回）</summary>
        public bool RerollUsed;

        public bool IsMarried;
        public int Children;

        /// <summary>残りの休みの回数。手番が回ってくるたびに1減らして飛ばす</summary>
        public int RestTurns;

        // 称号（精算の最後）の判定用に数えておく
        public int DividendTotal;

        /// <summary>切った約束手形の枚数（返済しても減らない）</summary>
        public int NotesIssued;

        /// <summary>賭け・宝くじで勝って受け取った額（宝くじの残念賞は含めない）</summary>
        public int GambleWinnings;

        /// <summary>他の人へ払った額（ご祝儀・係の手数料・指名・プレゼント）</summary>
        public int PaidToOthers;

        /// <summary>ゴールした順番（0 = 1着）。未ゴールは NotGoaled</summary>
        public int GoalOrder = NotGoaled;

        public bool HasGoaled => GoalOrder != NotGoaled;
        public bool HasHouse => HouseId != LifeRuleConfig.NoHouse;
    }

    /// <summary>
    /// ルールの状態すべて。オンラインでは全端末が同じシードから作り、同じコマンドを渡して同じ状態を保つ。
    /// </summary>
    public sealed class LifeGameState
    {
        public LifeRuleConfig Config;
        public LifeBoard Board;

        // 盤面生成・出目・職業カード・精算ルーレットはすべてこの1本の乱数から引く（端末間で引く順番がずれないように）
        public LifeRandom Random;

        public List<LifePlayerState> Players = new List<LifePlayerState>();
        public int CurrentSeat;
        public LifePending Pending;

        /// <summary>分岐で止まっているときの残りの歩数</summary>
        public int StepsLeft;

        public int LastRoll;

        /// <summary>進むマスで移動中か。着いたマスの効果を出さないために、分岐で止まっている間も覚えておく</summary>
        public bool IsWarping;

        /// <summary>今の時代。先頭が区間B・分岐②の道・区間C に入るたびに変わる</summary>
        public LifeEra Era;

        /// <summary>時代を引いた区間の段階（LifeEras.StageOf）。同じ区間で2回引かないように覚えておく</summary>
        public int EraStage;

        /// <summary>職業カード・転職で引いた2枚（職業番号）。選択待ちでなければ空</summary>
        public List<int> JobCards = new List<int>();

        public int GoalCount;

        public LifePlayerState Current => Players[CurrentSeat];
        public LifeCell CurrentCell => Board[Current.Position];

        /// <summary>全員能力なしで作る（テスト用）</summary>
        public static LifeGameState Create(int playerCount, int seed, LifeRuleConfig config)
        {
            return Create(new LifeAbility[playerCount], seed, config);
        }

        /// <summary>席ごとのキャラの能力を渡して作る。人数は abilities の数</summary>
        public static LifeGameState Create(IReadOnlyList<LifeAbility> abilities, int seed, LifeRuleConfig config)
        {
            var random = new LifeRandom(seed);
            var state = new LifeGameState
            {
                Config = config,
                Random = random,
                Board = LifeBoardGenerator.Generate(LifeBoardShape.CreateDefault(), config, random),
                Pending = LifePending.Spin,
            };

            for (int seat = 0; seat < abilities.Count; seat++)
            {
                LifeAbility ability = abilities[seat];
                int money = config.StartMoney + (ability == LifeAbility.StartMoney ? config.AbilityStartMoney : 0);
                state.Players.Add(new LifePlayerState { Seat = seat, Money = money, Ability = ability });
            }

            return state;
        }
    }
}
