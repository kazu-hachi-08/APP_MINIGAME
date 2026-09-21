using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// コート上に位置を持つもの（プレイヤー／NPCのラケット）。
    /// 表示だけを行うコンポーネントが、プレイヤー用・NPC用に分かれずに済むようにするための最小の窓口。
    /// </summary>
    public interface ICourtActor
    {
        /// <summary>x:左右 / y:台からの高さ / z:奥行き</summary>
        Vector3 CourtPosition { get; }
    }
}
