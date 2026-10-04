using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>戦闘の進行役（BattleRunner）・キー入力・ユニットの見た目の見本</summary>
    public static partial class PenguinWarsSceneBuilder
    {
        // HPバーの高さは UnitView がキャラの絵に合わせて置き直す。これは絵が無いときの仮の位置
        private const float UnitHpBarOffsetY = 1.4f;
        private static readonly Vector2 UnitHpBarSize = new Vector2(0.8f, 0.12f);

        // 城（1）より手前、城のHPバー（10〜11）より奥
        private const int UnitSortingOrder = 5;
        private const int UnitHpBarBackSortingOrder = 6;

        private static BattleRunner CreateBattle(PenguinWarsBalance balance, PenguinUnitCatalog catalog,
            CastleView leftCastle, CastleView rightCastle)
        {
            var battleObj = new GameObject("Battle");
            UnitViewPool unitViews = CreateUnitViewPool(battleObj.transform, catalog);

            var runner = battleObj.AddComponent<BattleRunner>();
            SetRefs(runner, ("_balance", balance), ("_catalog", catalog), ("_unitViews", unitViews),
                ("_leftCastle", leftCastle), ("_rightCastle", rightCastle));

            // キー入力は UI 部品を呼ぶので、参照は UI を作った後に BuildInternal でつなぐ
            battleObj.AddComponent<KeyboardCommandInput>();
            return runner;
        }

        private static UnitViewPool CreateUnitViewPool(Transform parent, PenguinUnitCatalog catalog)
        {
            var poolObj = new GameObject("Units");
            poolObj.transform.SetParent(parent, false);
            UnitView template = CreateUnitTemplate(poolObj.transform);

            var pool = poolObj.AddComponent<UnitViewPool>();
            SetRefs(pool, ("_template", template), ("_catalog", catalog));
            return pool;
        }

        /// <summary>UnitViewPool が複製して使う見本。原点は足元（城と同じく X だけで位置が決まるように）</summary>
        private static UnitView CreateUnitTemplate(Transform parent)
        {
            var unitObj = new GameObject("UnitTemplate");
            unitObj.transform.SetParent(parent, false);

            // 絵は UnitViewPool がキャラごとに差し替える。スプライトの原点が足元なので位置は (0, 0) のまま
            SpriteRenderer body = CreateSprite(unitObj.transform, "Body", null, UnitSortingOrder);
            HpBarView hpBar = CreateHpBar(unitObj.transform, UnitHpBarOffsetY, UnitHpBarSize, UnitHpBarBackSortingOrder);

            var unitView = unitObj.AddComponent<UnitView>();
            SetRefs(unitView, ("_body", body), ("_hpBar", hpBar));
            unitObj.SetActive(false);
            return unitView;
        }
    }
}
