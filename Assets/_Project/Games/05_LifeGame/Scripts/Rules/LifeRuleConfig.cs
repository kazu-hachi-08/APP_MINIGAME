namespace MiniGame.LifeGame
{
    public sealed class LifeJob
    {
        public int Salary;
        public bool IsAdvanced;
        public bool IsFreeter;
        public LifeJobRole Role;

        public LifeJob(int salary, bool isAdvanced = false, LifeJobRole role = LifeJobRole.None, bool isFreeter = false)
        {
            Salary = salary;
            IsAdvanced = isAdvanced;
            Role = role;
            IsFreeter = isFreeter;
        }
    }

    public sealed class LifeHouse
    {
        public int Price;

        // 精算ルーレットの出目 1〜3 / 4〜7 / 8〜10 ごとの売却倍率（%）。float を避けて全端末で同じ結果にするため整数で持つ
        public int[] SalePercents;

        public LifeHouse(int price, int low, int mid, int high)
        {
            Price = price;
            SalePercents = new[] { low, mid, high };
        }

        public int SalePercent(int roll)
        {
            if (roll <= 3) return SalePercents[0];
            if (roll <= 7) return SalePercents[1];
            return SalePercents[2];
        }
    }

    /// <summary>
    /// ルールの金額・上限（仕様書 §7.7）。LifeRules は Unity に依存しないので、ScriptableObject にせずコードで持つ。
    /// 変えたら LifeSimulationTests の「バランス集計」でルートごとの勝率を確かめる。
    /// </summary>
    public sealed class LifeRuleConfig
    {
        public const int RouletteMax = 10;
        public const int NoJob = -1;
        public const int NoHouse = -1;

        public int StartMoney = 300;

        // 番号は仕様書 §6.2 の職業枠の順（テーマの職業名もこの番号で引く）。
        // 上級職・フリーター・学費は NPC 同士の大量試合（LifeBalanceSimulator）で、分岐①のどのルートも勝率が極端に偏らない値にした
        public LifeJob[] Jobs =
        {
            new LifeJob(80, isFreeter: true),
            new LifeJob(180),
            new LifeJob(130, role: LifeJobRole.Police),
            new LifeJob(130, role: LifeJobRole.Repair),
            new LifeJob(160),
            new LifeJob(150),
            new LifeJob(200, isAdvanced: true, role: LifeJobRole.Healer),
            new LifeJob(230, isAdvanced: true),
            new LifeJob(260, isAdvanced: true),
        };

        public int Tuition = 200;
        public int MoneyCellMin = 30;
        public int MoneyCellMax = 150;
        public int MishapMin = 50;
        public int MishapMax = 150;
        public int GambleMultiplier = 2;
        public int FireWithoutHouse = 30;
        public int AmountStep = 10;
        public int[] GoalBonuses = { 300, 200, 100, 0 };

        public LifeHouse[] Houses =
        {
            new LifeHouse(300, 90, 110, 130),
            new LifeHouse(600, 80, 120, 160),
            new LifeHouse(1000, 60, 120, 200),
        };

        public int LifeInsurancePrice = 50;
        public int AutoInsurancePrice = 30;
        public int FireInsurancePrice = 40;

        public int StockPrice = 100;
        public int MaxStocks = 3;
        public int Dividend = 50;

        // 賭けマス。賭け金はマスの金額（ギャンブルルートは倍）。出目が BetWinMin 以上で勝ち
        public int BetStake = 100;
        public int BetWinMin = 5;

        // 指名・プレゼントマスで動く金額
        public int TransferMin = 50;
        public int TransferMax = 150;

        public int MarriageGift = 30;
        public int BirthGift = 20;
        public int MaxChildren = 4;

        public int NoteUnit = 100;
        public int NoteSettlement = 125;

        // キャラの能力（仕様書 §9.2）
        public int AbilityStartMoney = 50;
        public int AbilitySalaryPercent = 10;
        public int AbilityThriftPercent = 20;

        public int FreeterJobId
        {
            get
            {
                for (int i = 0; i < Jobs.Length; i++)
                {
                    if (Jobs[i].IsFreeter) return i;
                }

                return NoJob;
            }
        }

        public int SalaryOf(int jobId)
        {
            return jobId == NoJob ? 0 : Jobs[jobId].Salary;
        }

        public LifeJobRole RoleOf(int jobId)
        {
            return jobId == NoJob ? LifeJobRole.None : Jobs[jobId].Role;
        }

        public int InsurancePrice(LifeInsurance insurance)
        {
            switch (insurance)
            {
                case LifeInsurance.Life: return LifeInsurancePrice;
                case LifeInsurance.Auto: return AutoInsurancePrice;
                case LifeInsurance.Fire: return FireInsurancePrice;
                default: return 0;
            }
        }
    }
}
