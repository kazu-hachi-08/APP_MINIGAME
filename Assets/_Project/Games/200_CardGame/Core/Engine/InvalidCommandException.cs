using System;

namespace CardGame.Core.Engine
{
    /// <summary>ルール上許されないコマンドが渡された。</summary>
    public sealed class InvalidCommandException : Exception
    {
        public InvalidCommandException(string message) : base(message) { }
    }
}
