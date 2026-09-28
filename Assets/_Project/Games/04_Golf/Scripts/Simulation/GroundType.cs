namespace MiniGame.Golf
{
    /// <summary>
    /// 地面の種類（ライ）。タイルアセットに数値で保存されるため、並びを変えずに末尾へ足すこと。
    /// </summary>
    public enum GroundType
    {
        Tee = 0,
        Fairway = 1,
        Rough = 2,
        Bunker = 3,
        Green = 4,
        Water = 5,
        OutOfBounds = 6,
    }
}
