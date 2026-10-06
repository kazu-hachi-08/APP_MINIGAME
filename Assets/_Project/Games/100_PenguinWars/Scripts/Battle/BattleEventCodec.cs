using System.Collections.Generic;
using System.IO;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// ホストで起きた BattleEvent をゲストへ送るためのバイト列変換。1フレーム分をまとめて1メッセージにする（ヒットは多いので1件ずつ送らない）。
    /// 座標・陣営はホスト基準のまま。反転は受け取ったゲストが SideMirror で行う
    /// </summary>
    public static class BattleEventCodec
    {
        // 並び（TryRead と必ずそろえる）: ushort 件数 / 1件ごとに（byte 種類 / byte 陣営 / int ユニットID / float X / int Amount）

        public static byte[] ToBytes(IReadOnlyList<BattleEvent> events)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((ushort)events.Count);
                foreach (BattleEvent battleEvent in events)
                {
                    writer.Write((byte)battleEvent.Type);
                    writer.Write((byte)battleEvent.Side);
                    writer.Write(battleEvent.UnitId);
                    writer.Write(battleEvent.X);
                    writer.Write(battleEvent.Amount);
                }
                writer.Flush();
                return stream.ToArray();
            }
        }

        /// <summary>output に追加する。壊れたデータなら false（途中まで追加されていることがある）</summary>
        public static bool TryRead(byte[] data, List<BattleEvent> output)
        {
            try
            {
                using (var reader = new BinaryReader(new MemoryStream(data)))
                {
                    int count = reader.ReadUInt16();
                    for (int i = 0; i < count; i++)
                    {
                        var type = (BattleEventType)reader.ReadByte();
                        var side = (Side)reader.ReadByte();
                        int unitId = reader.ReadInt32();
                        float x = reader.ReadSingle();
                        int amount = reader.ReadInt32();
                        output.Add(new BattleEvent(type, side, unitId, x, amount));
                    }
                }
                return true;
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }
    }
}
