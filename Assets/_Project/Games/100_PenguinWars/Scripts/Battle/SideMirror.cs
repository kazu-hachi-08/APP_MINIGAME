namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// ゲスト画面の左右反転（仕様書 §10.3）。ゲストは右陣営だが、自分の城が左に来るよう「陣営を入れ替え・X を裏返し」て見せる。
    /// 変換をここ1か所に集め、ホストの BattleWorld の座標はホスト基準のままにする。
    /// 入れ替えた後はゲストの自分も Side.Left になるので、UI・演出はステージと同じ「自分 = Left」のコードで動く
    /// </summary>
    public static class SideMirror
    {
        public static float MirrorX(float x, float fieldLength)
        {
            return fieldLength - x;
        }

        public static Side MirrorSide(Side side)
        {
            return side.Opponent();
        }

        public static BattleEvent MirrorEvent(BattleEvent battleEvent, float fieldLength)
        {
            return new BattleEvent(battleEvent.Type, MirrorSide(battleEvent.Side), battleEvent.UnitId,
                MirrorX(battleEvent.X, fieldLength), battleEvent.Amount);
        }
    }
}
