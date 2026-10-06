using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>CampaignProgress.Record の戻り値。リザルトに「NEW RECORD!」「新しく取った★」「なかまになった！」を出すために使う</summary>
    public readonly struct StageRecordChange
    {
        public bool IsFirstClear { get; }
        public bool IsNewBest { get; }
        public StarFlags NewStars { get; }
        /// <summary>このクリアで新しく使えるようになったキャラの No。初クリアでなければ空</summary>
        public IReadOnlyList<int> NewUnlockNos { get; }

        public StageRecordChange(bool isFirstClear, bool isNewBest, StarFlags newStars, IReadOnlyList<int> newUnlockNos)
        {
            IsFirstClear = isFirstClear;
            IsNewBest = isNewBest;
            NewStars = newStars;
            NewUnlockNos = newUnlockNos ?? Array.Empty<int>();
        }
    }
}
