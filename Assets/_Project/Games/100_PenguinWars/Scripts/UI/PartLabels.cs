using System.Collections.Generic;
using MiniGame.PenguinWars.Art;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// じぶんペンギンの作成画面に出すパーツの日本語名（仕様書 §7.2 のパーツの表）。
    /// 表に無い ID は ID のまま出す（パーツを足したときに、名前を足し忘れても画面が壊れないように）
    /// </summary>
    public static class PartLabels
    {
        private const string NoneLabel = "なし";

        // aurora は体色と背中の両方にあるので、体色だけ別の表にする
        private static readonly Dictionary<string, string> BodyColors = new Dictionary<string, string>
        {
            ["standard"] = "くろしろ",
            ["ice"] = "みずいろ",
            ["gray"] = "はいいろ",
            ["aurora"] = "むらさき",
        };

        private static readonly Dictionary<string, string> Parts = new Dictionary<string, string>
        {
            // 体の形
            ["basic"] = "ふつう",
            ["wide"] = "よこなが",
            ["tall"] = "たてなが",
            ["round"] = "まんまる",
            ["small"] = "ひな",
            ["tower"] = "3だん",
            ["robot"] = "ロボ",
            // 頭
            ["helmet"] = "ヘルメット",
            ["box"] = "はこ",
            ["chonmage"] = "ちょんまげ",
            ["hood"] = "フード",
            ["wizard_hat"] = "まほうのぼうし",
            ["nightcap"] = "ナイトキャップ",
            ["ribbon"] = "リボン",
            ["crown"] = "おうかん",
            ["halo"] = "てんしのわ",
            ["drill"] = "ドリル",
            ["slime"] = "スライム",
            // 手
            ["axe"] = "おの",
            ["glove"] = "グローブ",
            ["bow"] = "ゆみ",
            ["big_snowball"] = "おおゆきだま",
            ["snowball_throw"] = "ゆきだま",
            ["fish_sword"] = "さかなソード",
            ["hammer"] = "ハンマー",
            ["katana"] = "かたな",
            ["sword_shield"] = "けんとたて",
            ["muscle_arm"] = "ムキムキうで",
            ["fishing_rod"] = "つりざお",
            ["cannon"] = "たいほう",
            ["staff"] = "つえ",
            ["sniper_rifle"] = "ライフル",
            ["boomerang"] = "ブーメラン",
            ["fan"] = "せんす",
            ["harisen"] = "ハリセン",
            ["mic"] = "マイク",
            ["balloon"] = "ふうせん",
            // 背中・体のまわり・足
            ["freezer"] = "れいとうこ",
            ["rocket"] = "ロケット",
            ["cape_blue"] = "あおマント",
            ["cape_red"] = "あかマント",
            ["wings"] = "つばさ",
            ["futon"] = "ふとん",
            ["mawashi"] = "まわし",
            ["snow_aura"] = "ゆきのオーラ",
            ["kamakura"] = "かまくら",
            ["iceberg"] = "ひょうざん",
            ["aurora"] = "オーロラ",
            ["bike"] = "バイク",
            ["long_legs"] = "ながあし",
            ["octopus_legs"] = "タコあし",
            ["tank"] = "せんしゃ",
            ["whale"] = "クジラ",
        };

        public static string Slot(PenguinPartSlot slot)
        {
            switch (slot)
            {
                case PenguinPartSlot.Body: return "体の形";
                case PenguinPartSlot.BodyColor: return "体の色";
                case PenguinPartSlot.Head: return "あたま";
                case PenguinPartSlot.Hand: return "て";
                case PenguinPartSlot.Back: return "せなか";
                default: return slot.ToString();
            }
        }

        public static string Part(PenguinPartSlot slot, string id)
        {
            if (string.IsNullOrEmpty(id)) return NoneLabel;

            Dictionary<string, string> table = slot == PenguinPartSlot.BodyColor ? BodyColors : Parts;
            return table.TryGetValue(id, out string label) ? label : id;
        }
    }
}
