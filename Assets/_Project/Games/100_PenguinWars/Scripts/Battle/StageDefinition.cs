using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>ステージ1つ分の定義（StageDefinitions の1要素）。セーブは番号ではなく Id で記録する（並び替えで壊れないように）</summary>
    public class StageDefinition
    {
        /// <summary>各章の最後（この番号）がボスステージ</summary>
        public const int StagesPerChapter = 6;
        private const float DefaultSafeHpRatio = 0.5f;

        /// <summary>"1-3" のような「章-番号」</summary>
        public string Id { get; set; }
        public int Chapter { get; set; }
        /// <summary>章の中で何番目か（1 始まり）</summary>
        public int Index { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        /// <summary>地面の絵は 30 ＋余白までしか敷いていないので、それより長くしない</summary>
        public float FieldLength { get; set; } = 30f;
        public int PlayerCastleHp { get; set; } = 3000;
        public int EnemyCastleHp { get; set; } = 3000;
        public IReadOnlyList<EnemySpawnEntry> Entries { get; set; } = Array.Empty<EnemySpawnEntry>();
        /// <summary>★3 の目標タイム（秒）</summary>
        public float TargetSeconds { get; set; }
        /// <summary>★2 に必要な自城HPの割合。ステージごとに変えたくなったときのために定義に持たせる</summary>
        public float SafeHpRatio { get; set; } = DefaultSafeHpRatio;
        /// <summary>初めてクリアしたときに仲間になるキャラの No</summary>
        public IReadOnlyList<int> UnlockNos { get; set; } = Array.Empty<int>();

        // ---- ギミック（既定値のままならギミックなし。BattleSettings への反映は ApplyTo） ----

        /// <summary>山と地面の色（"#RRGGBB"）。Battle は UnityEngine.Color を持てないので文字列で持つ。null なら色を変えない</summary>
        public string TintHex { get; set; }
        /// <summary>なだれの間隔（秒）。0 ならなし。範囲・予告・ダメージはオンラインの「なだれの谷」と同じ既定値</summary>
        public float AvalancheInterval { get; set; }
        /// <summary>最初から持っているさかな。0 なら通常どおり 0 から</summary>
        public int StartingFish { get; set; }
        /// <summary>働きペンギンをこのレベルより上げられない。0 なら制限なし</summary>
        public int MaxWalletLevel { get; set; }
        /// <summary>敵の城が撃つペンギン砲。null なら撃たない</summary>
        public EnemyCannonSettings EnemyCannon { get; set; }
        /// <summary>編成制限: これより高いコストのキャラは出せない。0 なら制限なし</summary>
        public int MaxUnitCost { get; set; }
        /// <summary>編成制限: この役割のキャラは出せない</summary>
        public IReadOnlyList<UnitRole> BannedRoles { get; set; } = Array.Empty<UnitRole>();

        public bool IsBossStage => Index == StagesPerChapter;

        /// <summary>Balance から作った設定のうち、ステージで変わる分だけ上書きする（PenguinStageData.ApplyTo と同じ考え方）</summary>
        public void ApplyTo(BattleSettings settings)
        {
            settings.FieldLength = FieldLength;
            settings.LeftCastleHp = PlayerCastleHp;
            settings.RightCastleHp = EnemyCastleHp;
            settings.AvalancheInterval = AvalancheInterval;
            settings.StartingFish = StartingFish;
            settings.MaxWalletLevel = MaxWalletLevel;
            settings.EnemyCannon = EnemyCannon;
            settings.DeckMaxUnitCost = MaxUnitCost;
            settings.DeckBannedRoles = BannedRoles ?? Array.Empty<UnitRole>();
        }

        /// <summary>「出てくる敵」の紹介用。出てくる順（定義の行の順）で重複なし</summary>
        public List<int> DistinctEnemyNos()
        {
            var nos = new List<int>();
            foreach (EnemySpawnEntry entry in Entries)
            {
                if (!nos.Contains(entry.UnitNo)) nos.Add(entry.UnitNo);
            }
            return nos;
        }

        /// <summary>このキャラがボスの行で出てくるか（紹介で枠を赤くする）</summary>
        public bool IsBossUnit(int unitNo)
        {
            foreach (EnemySpawnEntry entry in Entries)
            {
                if (entry.IsBoss && entry.UnitNo == unitNo) return true;
            }
            return false;
        }
    }
}
