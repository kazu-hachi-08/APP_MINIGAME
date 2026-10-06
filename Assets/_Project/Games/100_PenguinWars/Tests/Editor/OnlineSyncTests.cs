using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using NUnit.Framework;
using static MiniGame.PenguinWars.Battle.Tests.BattleTestUtil;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>オンライン対戦（§10.2）: 時間切れの判定と、ゲストへ送る状態・イベントの書き出し→読み込み</summary>
    public class OnlineSyncTests
    {
        private const float FieldLength = 10f;
        private const int CastleHp = 1000;
        // X は 0.01 単位、HP割合は 1/255 単位に詰めて送るので、その分だけずれてよい
        private const float XTolerance = 0.01f;
        private const float HpTolerance = 1f / 255f;

        private static UnitStats Melee(int unitNo = 1, int cost = 0)
        {
            return new UnitStats
            {
                UnitNo = unitNo, Cost = cost, Cooldown = 2f, MaxHp = 100, Attack = 10, Range = 1.4f,
                AttackInterval = 1.2f, Windup = 0.3f, MoveSpeed = 1f,
            };
        }

        private static BattleWorld CreateVersus(float timeLimit)
        {
            var world = new BattleWorld(new BattleSettings
            {
                FieldLength = FieldLength,
                LeftCastleHp = CastleHp,
                RightCastleHp = CastleHp,
                TimeLimit = timeLimit,
            });
            world.SetDeck(Side.Left, new[] { Melee(1), Melee(2) });
            world.SetDeck(Side.Right, new[] { Melee(3) });
            return world;
        }

        private static void Run(BattleWorld world, float seconds)
        {
            int steps = (int)(seconds / StepTime) + 1;
            for (int i = 0; i < steps && !world.IsFinished; i++) world.Step(StepTime);
        }

        // ---- 時間切れ ----

        [Test]
        public void TimeLimit_ZeroMeansNoTimeUp()
        {
            BattleWorld world = CreateVersus(0f);
            Run(world, 5f);
            Assert.IsFalse(world.IsFinished);
            Assert.IsFalse(world.HasTimeLimit);
        }

        [Test]
        public void TimeUp_SameCastleHp_IsDraw()
        {
            BattleWorld world = CreateVersus(1f);
            Run(world, 1.5f);

            Assert.IsTrue(world.IsFinished);
            Assert.IsTrue(world.IsDraw);
            Assert.AreEqual(0f, world.RemainingTime);
            BattleEvent timeUp = Drain(world).Find(e => e.Type == BattleEventType.TimeUp);
            Assert.AreEqual(1, timeUp.Amount);
        }

        [Test]
        public void TimeUp_LowerCastleHpRatioLoses()
        {
            BattleWorld world = CreateVersus(1f);
            world.GetCastle(Side.Right).Hp = CastleHp - 1;
            Run(world, 1.5f);

            Assert.IsTrue(world.IsFinished);
            Assert.IsFalse(world.IsDraw);
            Assert.AreEqual(Side.Right, world.Loser);
            BattleEvent timeUp = Drain(world).Find(e => e.Type == BattleEventType.TimeUp);
            Assert.AreEqual(Side.Right, timeUp.Side);
            Assert.AreEqual(0, timeUp.Amount);
        }

        [Test]
        public void TimeUp_ComparesRatioNotRawHp()
        {
            var world = new BattleWorld(new BattleSettings
            {
                FieldLength = FieldLength, LeftCastleHp = 2000, RightCastleHp = 1000, TimeLimit = 1f,
            });
            // 左 1200/2000 = 60%、右 700/1000 = 70%。HP の数だけなら左が多いが、割合では左の負け
            world.GetCastle(Side.Left).Hp = 1200;
            world.GetCastle(Side.Right).Hp = 700;
            Run(world, 1.5f);

            Assert.AreEqual(Side.Left, world.Loser);
            Assert.IsFalse(world.IsDraw);
        }

        // ---- 状態の書き出し→読み込み ----

        [Test]
        public void Snapshot_RoundTrip_KeepsValues()
        {
            BattleWorld world = CreateVersus(300f);
            Run(world, 3f);
            world.Enqueue(BattleCommand.Spawn(Side.Left, 1));
            world.Enqueue(BattleCommand.Spawn(Side.Right, 0));
            Run(world, 1f);

            var sent = new BattleSnapshot();
            sent.Capture(world);
            var received = new BattleSnapshot();
            Assert.IsTrue(received.TryRead(sent.ToBytes()));

            Assert.AreEqual(sent.RemainingTime, received.RemainingTime);
            for (int side = 0; side < 2; side++) AssertSideEqual(sent.Sides[side], received.Sides[side]);
            Assert.AreEqual(2, received.Units.Count);
            for (int i = 0; i < sent.Units.Count; i++) AssertUnitEqual(sent.Units[i], received.Units[i]);
        }

        [Test]
        public void Snapshot_RoundTrip_KeepsStatusFlagsAndAction()
        {
            var sent = new BattleSnapshot();
            sent.Units.Add(new UnitSnapshot
            {
                Id = 42, UnitNo = 50, Side = Side.Right, X = 12.34f, HpRatio = 0.5f,
                Action = UnitAction.Knockback, IsFrozen = true, IsSlowed = true,
            });
            var received = new BattleSnapshot();
            Assert.IsTrue(received.TryRead(sent.ToBytes()));

            AssertUnitEqual(sent.Units[0], received.Units[0]);
        }

        [Test]
        public void Snapshot_TruncatedData_ReturnsFalse()
        {
            var sent = new BattleSnapshot();
            sent.Capture(CreateVersus(300f));
            byte[] bytes = sent.ToBytes();
            System.Array.Resize(ref bytes, bytes.Length / 2);

            Assert.IsFalse(new BattleSnapshot().TryRead(bytes));
        }

        [Test]
        public void Events_RoundTrip_KeepsValues()
        {
            var sent = new List<BattleEvent>
            {
                new BattleEvent(BattleEventType.Spawned, Side.Left, 7, 1.5f),
                new BattleEvent(BattleEventType.Hit, Side.Right, BattleEvent.CastleId, 10f, 123),
                new BattleEvent(BattleEventType.TimeUp, Side.Left, BattleEvent.CastleId, 0f, 1),
            };
            var received = new List<BattleEvent>();
            Assert.IsTrue(BattleEventCodec.TryRead(BattleEventCodec.ToBytes(sent), received));

            Assert.AreEqual(sent.Count, received.Count);
            for (int i = 0; i < sent.Count; i++)
            {
                Assert.AreEqual(sent[i].Type, received[i].Type);
                Assert.AreEqual(sent[i].Side, received[i].Side);
                Assert.AreEqual(sent[i].UnitId, received[i].UnitId);
                Assert.AreEqual(sent[i].X, received[i].X);
                Assert.AreEqual(sent[i].Amount, received[i].Amount);
            }
        }

        // ---- ゲストの写し（左右反転） ----

        [Test]
        public void GuestMirror_SwapsSidesAndMirrorsX()
        {
            BattleWorld host = CreateVersus(300f);
            host.Enqueue(BattleCommand.Spawn(Side.Left, 0));
            Run(host, 1f);
            host.GetCastle(Side.Left).Hp = 400;

            // ゲスト画面では自分（ホストの右）が左に来るので、編成も入れ替えて持つ
            BattleWorld guest = CreateGuestWorld(host);
            var mirror = new GuestWorldMirror(guest, no => Melee(no));
            var snapshot = new BattleSnapshot();
            snapshot.Capture(host);
            mirror.Apply(snapshot);
            mirror.Advance(1f, 0.1f);

            UnitState hostUnit = host.Units[0];
            UnitState guestUnit = guest.Units[0];
            Assert.AreEqual(Side.Right, guestUnit.Side);
            Assert.AreEqual(FieldLength - hostUnit.X, guestUnit.X, XTolerance);
            Assert.AreEqual(400, guest.GetCastle(Side.Right).Hp);
            Assert.AreEqual(host.GetWallet(Side.Right).Fish, guest.GetWallet(Side.Left).Fish);
            Assert.AreEqual(host.GetSlot(Side.Left, 0).Remaining, guest.GetSlot(Side.Right, 0).Remaining, 0.0001f);
        }

        [Test]
        public void GuestMirror_RemovesUnitsMissingFromSnapshot()
        {
            BattleWorld host = CreateVersus(300f);
            host.Enqueue(BattleCommand.Spawn(Side.Left, 0));
            host.Step(StepTime);
            BattleWorld guest = CreateGuestWorld(host);
            var mirror = new GuestWorldMirror(guest, no => Melee(no));
            var snapshot = new BattleSnapshot();
            snapshot.Capture(host);
            mirror.Apply(snapshot);
            Assert.AreEqual(1, guest.Units.Count);

            snapshot.Units.Clear();
            mirror.Apply(snapshot);
            Assert.AreEqual(0, guest.Units.Count);
        }

        [Test]
        public void MirrorEvent_SwapsSideAndX()
        {
            var hostEvent = new BattleEvent(BattleEventType.CastleDestroyed, Side.Right, BattleEvent.CastleId, FieldLength);
            BattleEvent local = SideMirror.MirrorEvent(hostEvent, FieldLength);

            Assert.AreEqual(Side.Left, local.Side);
            Assert.AreEqual(0f, local.X);
        }

        private static BattleWorld CreateGuestWorld(BattleWorld host)
        {
            BattleWorld guest = CreateVersus(300f);
            guest.SetDeck(Side.Left, host.GetDeck(Side.Right));
            guest.SetDeck(Side.Right, host.GetDeck(Side.Left));
            return guest;
        }

        private static void AssertSideEqual(SideSnapshot expected, SideSnapshot actual)
        {
            Assert.AreEqual(expected.CastleHp, actual.CastleHp);
            Assert.AreEqual(expected.Fish, actual.Fish);
            Assert.AreEqual(expected.WalletLevel, actual.WalletLevel);
            Assert.AreEqual(expected.CannonCharge, actual.CannonCharge);
            CollectionAssert.AreEqual(expected.SlotRemaining, actual.SlotRemaining);
        }

        private static void AssertUnitEqual(UnitSnapshot expected, UnitSnapshot actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.UnitNo, actual.UnitNo);
            Assert.AreEqual(expected.Side, actual.Side);
            Assert.AreEqual(expected.X, actual.X, XTolerance);
            Assert.AreEqual(expected.HpRatio, actual.HpRatio, HpTolerance);
            Assert.AreEqual(expected.Action, actual.Action);
            Assert.AreEqual(expected.IsFrozen, actual.IsFrozen);
            Assert.AreEqual(expected.IsSlowed, actual.IsSlowed);
        }
    }
}
