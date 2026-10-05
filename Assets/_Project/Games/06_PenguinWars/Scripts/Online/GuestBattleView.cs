using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ゲストのみ。ホストから届いたバイト列を状態・イベントに戻して BattleRunner に流す。
    /// 表示（UnitView・UI・演出）はステージと同じものが BattleRunner 経由で動く
    /// </summary>
    public class GuestBattleView : MonoBehaviour
    {
        [SerializeField] private PenguinWarsOnlineLink _link;
        [SerializeField] private BattleRunner _battleRunner;

        // 毎回作らないよう使い回す
        private readonly BattleSnapshot _snapshot = new BattleSnapshot();
        private readonly List<BattleEvent> _events = new List<BattleEvent>();

        private void OnEnable()
        {
            _link.SnapshotReceived += HandleSnapshot;
            _link.EventsReceived += HandleEvents;
        }

        private void OnDisable()
        {
            _link.SnapshotReceived -= HandleSnapshot;
            _link.EventsReceived -= HandleEvents;
        }

        /// <summary>編成が届く前（InitializeGuest 前）に状態が先着することがあるので、そのときは捨てる（次がすぐ届く）</summary>
        private void HandleSnapshot(byte[] bytes)
        {
            if (!_battleRunner.IsGuest) return;

            if (_snapshot.TryRead(bytes)) _battleRunner.ApplyRemoteSnapshot(_snapshot);
            else Debug.LogWarning("[GuestBattleView] 状態を読めませんでした（ホストとビルドのバージョン違い？）");
        }

        private void HandleEvents(byte[] bytes)
        {
            if (!_battleRunner.IsGuest) return;

            _events.Clear();
            if (!BattleEventCodec.TryRead(bytes, _events))
            {
                Debug.LogWarning("[GuestBattleView] イベントを読めませんでした（ホストとビルドのバージョン違い？）");
            }
            foreach (BattleEvent battleEvent in _events) _battleRunner.PushRemoteEvent(battleEvent);
        }
    }
}
