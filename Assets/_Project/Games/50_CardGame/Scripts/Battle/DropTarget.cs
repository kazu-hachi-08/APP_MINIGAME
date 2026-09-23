#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardGame.Unity.Battle
{
    public enum DropKind
    {
        /// <summary>自分の場(空き領域)。手札をここに落とすとプレイ。</summary>
        MyBoard,
        /// <summary>相手の場(空き領域)。スペルはここに落としてもプレイできる。</summary>
        EnemyBoard,
        MyLeader,
        EnemyLeader,
        MyEntity,
        EnemyEntity,
    }

    /// <summary>
    /// ドロップ先・対象選択のタップ先になる UI 要素。
    /// CardView(場のカード)、リーダーパネル、自分の場の背景に付ける。
    /// </summary>
    public sealed class DropTarget : MonoBehaviour, IPointerClickHandler
    {
        public DropKind Kind;
        /// <summary>Kind が *Entity のとき: BoardEntity.InstanceId。</summary>
        public int InstanceId = -1;
        /// <summary>Kind が *Leader のとき: プレイヤー番号。</summary>
        public int PlayerIndex = -1;

        public BattleScreen Screen = null!;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;
            Screen.OnTargetClicked(this, eventData);
        }
    }
}
