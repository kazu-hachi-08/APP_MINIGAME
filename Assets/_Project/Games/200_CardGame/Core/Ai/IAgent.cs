using CardGame.Core.Commands;
using CardGame.Core.Engine;

namespace CardGame.Core.Ai
{
    /// <summary>プレイヤーの意思決定を行うもの(AI)。手番ごとに 1 コマンドずつ返す。</summary>
    public interface IAgent
    {
        /// <summary>
        /// 現在の状態から次に実行するコマンドを決める。
        /// engine は読み取り専用として扱い、先読みは <see cref="GameEngine.Clone"/> で行うこと。
        /// </summary>
        GameCommand Decide(GameEngine engine, int player);
    }
}
