using MiniGame.PenguinWars.Battle;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 操作の送り先。オンラインのゲストは自分で戦闘を計算しないので、BattleRunner は操作をここ（ホストへの送信）に渡す。
    /// UI・キー入力はどちらの場合も BattleRunner.Enqueue を呼ぶだけで済む
    /// </summary>
    public interface ICommandSink
    {
        /// <param name="command">ゲスト画面の向き（自分 = Side.Left）のままの操作。陣営はホストが付け直す</param>
        void Submit(BattleCommand command);
    }
}
