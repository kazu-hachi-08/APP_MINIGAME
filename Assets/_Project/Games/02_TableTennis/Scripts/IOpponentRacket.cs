using System;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 相手側のラケット（NPC／オンラインの対戦相手）。
    /// 表示コンポーネントが、相手がNPCか人間かを気にせず位置とスイングを描けるようにするための窓口。
    /// </summary>
    public interface IOpponentRacket : ICourtActor
    {
        /// <summary>ラケットを振った。スイング演出用</summary>
        event Action OnSwing;
    }
}
