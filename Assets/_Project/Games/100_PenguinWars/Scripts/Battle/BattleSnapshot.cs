using System;
using System.Collections.Generic;
using System.IO;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>ユニット1体ぶんの同期内容（仕様書 §10.2）</summary>
    public struct UnitSnapshot
    {
        public int Id;
        public int UnitNo;
        public Side Side;
        public float X;
        public float HpRatio;
        public UnitAction Action;
        public bool IsFrozen;
        public bool IsSlowed;
    }

    /// <summary>陣営1つぶんの同期内容（城・さかな・働きペンギン・砲・再生産）</summary>
    public class SideSnapshot
    {
        public int CastleHp;
        public int Fish;
        public int WalletLevel;
        public float CannonCharge;
        public float[] SlotRemaining = Array.Empty<float>();
    }

    /// <summary>
    /// ホストがゲストへ約15回/秒送る試合の状態。座標・陣営はホスト基準のまま入れ、反転はゲスト側（GuestWorldMirror）で行う。
    /// 再送なしの配信は1パケット（約1.2KB）に収める必要があるので、1体あたり 9 バイトに詰める（60体で約 540 バイト）
    /// </summary>
    public class BattleSnapshot
    {
        // X は 0.01 単位の short（戦場 30 なら 3000 で収まる）、HP割合は 0〜255 の byte
        private const float XScale = 100f;
        private const float HpScale = 255f;
        // 状態 byte の下位 3 ビットが UnitAction、その上が状態異常のフラグ
        private const int ActionMask = 0x07;
        private const int FrozenFlag = 0x08;
        private const int SlowedFlag = 0x10;

        public float RemainingTime;
        /// <summary>添字は (int)Side</summary>
        public readonly SideSnapshot[] Sides = { new SideSnapshot(), new SideSnapshot() };
        public readonly List<UnitSnapshot> Units = new List<UnitSnapshot>();

        public void Capture(BattleWorld world)
        {
            RemainingTime = world.RemainingTime;
            CaptureSide(world, Side.Left);
            CaptureSide(world, Side.Right);

            Units.Clear();
            foreach (UnitState unit in world.Units)
            {
                Units.Add(new UnitSnapshot
                {
                    Id = unit.Id, UnitNo = unit.UnitNo, Side = unit.Side, X = unit.X, HpRatio = unit.HpRatio,
                    Action = unit.Action, IsFrozen = unit.Status.IsFrozen, IsSlowed = unit.Status.IsSlowed,
                });
            }
        }

        private void CaptureSide(BattleWorld world, Side side)
        {
            SideSnapshot data = Sides[(int)side];
            WalletState wallet = world.GetWallet(side);
            data.CastleHp = world.GetCastle(side).Hp;
            data.Fish = wallet.Fish;
            data.WalletLevel = wallet.Level;
            data.CannonCharge = world.GetCannon(side).Charge;

            int slotCount = world.GetDeck(side).Count;
            if (data.SlotRemaining.Length != slotCount) data.SlotRemaining = new float[slotCount];
            for (int i = 0; i < slotCount; i++) data.SlotRemaining[i] = world.GetSlot(side, i).Remaining;
        }

        // ---- バイト列 ----
        // 並び（TryRead と必ずそろえる）:
        //   float 残り時間 / 陣営 Left・Right の順に（int 城HP / ushort さかな / byte 財布Lv / float 砲チャージ / byte 枠数 / 枠ごとに float 再生産残り）/
        //   byte ユニット数 / ユニットごとに（ushort ID / byte キャラNo / byte 陣営 / short X / byte HP割合 / byte 状態）

        public byte[] ToBytes()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(RemainingTime);
                foreach (SideSnapshot side in Sides) WriteSide(writer, side);

                writer.Write((byte)Units.Count);
                foreach (UnitSnapshot unit in Units) WriteUnit(writer, unit);
                writer.Flush();
                return stream.ToArray();
            }
        }

        /// <summary>壊れたデータ（別バージョンのビルド等）なら false。中身は途中まで書き換わっているので使わないこと</summary>
        public bool TryRead(byte[] data)
        {
            try
            {
                using (var reader = new BinaryReader(new MemoryStream(data)))
                {
                    RemainingTime = reader.ReadSingle();
                    foreach (SideSnapshot side in Sides) ReadSide(reader, side);

                    Units.Clear();
                    int count = reader.ReadByte();
                    for (int i = 0; i < count; i++) Units.Add(ReadUnit(reader));
                }
                return true;
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        private static void WriteSide(BinaryWriter writer, SideSnapshot side)
        {
            writer.Write(side.CastleHp);
            writer.Write((ushort)side.Fish);
            writer.Write((byte)side.WalletLevel);
            writer.Write(side.CannonCharge);
            writer.Write((byte)side.SlotRemaining.Length);
            foreach (float remaining in side.SlotRemaining) writer.Write(remaining);
        }

        private static void ReadSide(BinaryReader reader, SideSnapshot side)
        {
            side.CastleHp = reader.ReadInt32();
            side.Fish = reader.ReadUInt16();
            side.WalletLevel = reader.ReadByte();
            side.CannonCharge = reader.ReadSingle();
            int slotCount = reader.ReadByte();
            if (side.SlotRemaining.Length != slotCount) side.SlotRemaining = new float[slotCount];
            for (int i = 0; i < slotCount; i++) side.SlotRemaining[i] = reader.ReadSingle();
        }

        /// <summary>ID は下位 16 ビットだけ送る。ゲストは同じ時点に場にいるユニットを見分けられればよいので、6万体を超えて一周しても困らない</summary>
        private static void WriteUnit(BinaryWriter writer, UnitSnapshot unit)
        {
            writer.Write((ushort)unit.Id);
            writer.Write((byte)unit.UnitNo);
            writer.Write((byte)unit.Side);
            writer.Write((short)Math.Round(unit.X * XScale));
            writer.Write((byte)Math.Round(unit.HpRatio * HpScale));

            int flags = (int)unit.Action & ActionMask;
            if (unit.IsFrozen) flags |= FrozenFlag;
            if (unit.IsSlowed) flags |= SlowedFlag;
            writer.Write((byte)flags);
        }

        private static UnitSnapshot ReadUnit(BinaryReader reader)
        {
            var unit = new UnitSnapshot
            {
                Id = reader.ReadUInt16(),
                UnitNo = reader.ReadByte(),
                Side = (Side)reader.ReadByte(),
                X = reader.ReadInt16() / XScale,
                HpRatio = reader.ReadByte() / HpScale,
            };
            int flags = reader.ReadByte();
            unit.Action = (UnitAction)(flags & ActionMask);
            unit.IsFrozen = (flags & FrozenFlag) != 0;
            unit.IsSlowed = (flags & SlowedFlag) != 0;
            return unit;
        }
    }
}
