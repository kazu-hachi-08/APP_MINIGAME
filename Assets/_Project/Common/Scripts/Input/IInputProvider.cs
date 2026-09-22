using UnityEngine;

namespace MiniGame.Common.Input
{
    /// <summary>
    /// ミニゲーム入力操作のインターフェース
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>移動方向ベクトル（-1.0 ～ +1.0、正規化済みまたはアナログ値）</summary>
        Vector2 MoveVector { get; }

        /// <summary>Action 1（例：パス / ショートキック）押下瞬間</summary>
        bool IsAction1Down { get; }

        /// <summary>Action 1 押下中</summary>
        bool IsAction1Held { get; }

        /// <summary>Action 1 離した瞬間</summary>
        bool IsAction1Up { get; }

        /// <summary>Action 2（例：シュート / 強キック）押下瞬間</summary>
        bool IsAction2Down { get; }

        /// <summary>Action 2 押下中</summary>
        bool IsAction2Held { get; }

        /// <summary>Action 2 離した瞬間</summary>
        bool IsAction2Up { get; }

        /// <summary>Action 3（例：選手手動切り替え / ダッシュ）押下瞬間</summary>
        bool IsAction3Down { get; }

        /// <summary>Action 4（例：スライディングタックル）押下瞬間</summary>
        bool IsAction4Down { get; }

        /// <summary>ポーズボタン押下瞬間</summary>
        bool IsPauseDown { get; }
    }
}
