using MiniGame.PenguinWars.Art;
using MiniGame.PenguinWars.Battle;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// じぶんペンギンの定義から、既存キャラと同じ形の PenguinUnitData（数値＋両陣営の絵）を作る。
    /// 作成画面のプレビューと対戦の両方がこれを通すので、見た目と数値の作り方が1か所にまとまる
    /// </summary>
    public static class CustomUnitFactory
    {
        // 大型は既存の大型キャラ（UnitLooks の LargeScale）と同じ2倍。見た目は強さに影響させないので拡大率は選ばせない
        private const int LargeScale = 2;
        private const int NormalScale = 1;

        /// <summary>定義はきまりに合わせて直したもの（CustomUnitRules.Sanitize 済み）を渡す</summary>
        public static PenguinUnitData Create(CustomUnitDefinition def, int no)
        {
            UnitStats stats = UnitStatFormula.Calculate(CustomUnitRules.ToUnitDefinition(def, no));
            PenguinLook look = ToLook(def);
            return PenguinUnitData.CreateRuntime(stats, def.Name, look,
                RuntimeUnitSprites.Build(look, Side.Left), RuntimeUnitSprites.Build(look, Side.Right));
        }

        /// <summary>知らない ID（別のビルドのセーブ・通信）は既定に置き換え、合成で落ちないようにする</summary>
        public static PenguinLook ToLook(CustomUnitDefinition def)
        {
            return new PenguinLook(
                PenguinPartCatalog.Sanitize(PenguinPartSlot.Body, def.Body),
                PenguinPartCatalog.Sanitize(PenguinPartSlot.BodyColor, def.BodyColor),
                PenguinPartCatalog.Sanitize(PenguinPartSlot.Head, def.Head),
                PenguinPartCatalog.Sanitize(PenguinPartSlot.Hand, def.Hand),
                PenguinPartCatalog.Sanitize(PenguinPartSlot.Back, def.Back),
                def.Role == UnitRole.Large ? LargeScale : NormalScale);
        }
    }
}
