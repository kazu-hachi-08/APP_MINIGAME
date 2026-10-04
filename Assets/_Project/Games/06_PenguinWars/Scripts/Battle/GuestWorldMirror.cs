using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// オンラインのゲストが持つ「表示用の BattleWorld」に、ホストから届いた状態を左右反転して書き込む（仕様書 §10.2・§10.3）。
    /// ゲストはこの World を Step しない。UI・View はエンドレスと同じく World を読むだけなので、ゲスト専用の表示コードが要らない。
    /// 位置は届くたびに飛ばさず、前回の位置から次の位置へ1回の送信間隔をかけて動かす（約15回/秒の送信でもカクつかないように）
    /// </summary>
    public class GuestWorldMirror
    {
        private class Track
        {
            public UnitState Unit;
            public float FromX;
            public float ToX;
        }

        private readonly BattleWorld _world;
        private readonly Func<int, UnitStats> _findStats;
        private readonly Dictionary<int, Track> _tracks = new Dictionary<int, Track>();
        // 毎回作らないよう使い回す
        private readonly HashSet<int> _receivedIds = new HashSet<int>();
        private readonly List<int> _goneIds = new List<int>();
        // 0=前回の位置、1=届いた位置。全ユニットが同じ時点の状態なので1つで足りる
        private float _progress = 1f;

        private float FieldLength => _world.Settings.FieldLength;

        /// <param name="world">ゲストが表示に使う World。編成（SetDeck）は反転後の陣営で入れておく</param>
        /// <param name="findStats">キャラNo → 数値。届くのは No だけなので、射程・攻撃間隔などはゲストのデータから引く</param>
        public GuestWorldMirror(BattleWorld world, Func<int, UnitStats> findStats)
        {
            _world = world;
            _findStats = findStats;
        }

        public BattleWorld World => _world;

        public void Apply(BattleSnapshot snapshot)
        {
            _world.RemainingTime = snapshot.RemainingTime;
            ApplySide(Side.Left, snapshot.Sides[(int)Side.Left]);
            ApplySide(Side.Right, snapshot.Sides[(int)Side.Right]);
            ApplyUnits(snapshot.Units);
            _progress = 0f;
        }

        /// <param name="interpolationTime">前回の位置から届いた位置まで動かす秒数。ホストの送信間隔と同じにする</param>
        public void Advance(float deltaTime, float interpolationTime)
        {
            _progress = interpolationTime > 0f ? Math.Min(1f, _progress + deltaTime / interpolationTime) : 1f;
            foreach (Track track in _tracks.Values)
            {
                track.Unit.X = track.FromX + (track.ToX - track.FromX) * _progress;
                // 攻撃・ノックバックのコマ選び（UnitView）が残り時間を見るので、届いた後はゲスト側で減らしておく
                track.Unit.ActionTimer = Math.Max(0f, track.Unit.ActionTimer - deltaTime);
            }
        }

        /// <summary>ホストで起きたイベントを、ゲスト画面の向きに直す</summary>
        public BattleEvent ToLocal(BattleEvent battleEvent)
        {
            return SideMirror.MirrorEvent(battleEvent, FieldLength);
        }

        /// <param name="hostSide">ホスト基準の陣営。ゲスト画面では反対側になる</param>
        private void ApplySide(Side hostSide, SideSnapshot data)
        {
            Side side = SideMirror.MirrorSide(hostSide);
            _world.GetCastle(side).Hp = data.CastleHp;
            _world.GetWallet(side).Restore(data.WalletLevel, data.Fish);
            _world.GetCannon(side).Restore(data.CannonCharge);

            IReadOnlyList<UnitStats> deck = _world.GetDeck(side);
            int slotCount = Math.Min(deck.Count, data.SlotRemaining.Length);
            for (int i = 0; i < slotCount; i++) _world.GetSlot(side, i).Restore(data.SlotRemaining[i], deck[i].Cooldown);
        }

        private void ApplyUnits(List<UnitSnapshot> units)
        {
            _receivedIds.Clear();
            foreach (UnitSnapshot unit in units)
            {
                Track track = FindOrCreate(unit);
                if (track == null) continue;

                _receivedIds.Add(unit.Id);
                ApplyUnit(track, unit);
            }
            RemoveGoneUnits();
        }

        private Track FindOrCreate(UnitSnapshot unit)
        {
            if (_tracks.TryGetValue(unit.Id, out Track track)) return track;

            UnitStats stats = _findStats(unit.UnitNo);
            // ホストとゲストでキャラデータがずれている（別バージョンのビルド）。描けないので出さない
            if (stats == null) return null;

            float x = SideMirror.MirrorX(unit.X, FieldLength);
            var state = new UnitState(unit.Id, SideMirror.MirrorSide(unit.Side), stats, x);
            _world.MutableUnits.Add(state);
            // 出てきた瞬間は補間せず、届いた位置にそのまま置く
            track = new Track { Unit = state, FromX = x, ToX = x };
            _tracks.Add(unit.Id, track);
            return track;
        }

        private void ApplyUnit(Track track, UnitSnapshot unit)
        {
            UnitState state = track.Unit;
            track.FromX = state.X;
            track.ToX = SideMirror.MirrorX(unit.X, FieldLength);
            state.Hp = (int)Math.Round(unit.HpRatio * state.Stats.MaxHp);
            state.Status.Restore(unit.IsFrozen, unit.IsSlowed);
            if (state.Action == unit.Action) return;

            state.Action = unit.Action;
            state.ActionTimer = ActionLength(state);
        }

        /// <summary>行動が変わった瞬間の残り時間。ホストと同じ長さから数え始めれば、ゲストでも攻撃・ノックバックのコマが同じように出る</summary>
        private float ActionLength(UnitState state)
        {
            switch (state.Action)
            {
                case UnitAction.Windup: return state.Stats.Windup;
                case UnitAction.Cooldown: return Math.Max(0f, state.Stats.AttackInterval - state.Stats.Windup);
                case UnitAction.Knockback: return _world.Settings.KnockbackDuration;
                default: return 0f;
            }
        }

        /// <summary>届いた状態にいないユニットは倒されたもの（撃破の演出はイベントで別に届く）</summary>
        private void RemoveGoneUnits()
        {
            _goneIds.Clear();
            foreach (int id in _tracks.Keys)
            {
                if (!_receivedIds.Contains(id)) _goneIds.Add(id);
            }
            foreach (int id in _goneIds)
            {
                _world.MutableUnits.Remove(_tracks[id].Unit);
                _tracks.Remove(id);
            }
        }
    }
}
