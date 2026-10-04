using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;

namespace MiniGame.PenguinWars
{
    /// <summary>デモの中で起こすこと1つ。Time 秒目に、ユニットを X に出す（UnitNo が 0 ならペンギン砲を撃つ）</summary>
    public readonly struct GuideAction
    {
        public const int CannonUnitNo = 0;

        public float Time { get; }
        public Side Side { get; }
        public int UnitNo { get; }
        public float X { get; }

        private GuideAction(float time, Side side, int unitNo, float x)
        {
            Time = time;
            Side = side;
            UnitNo = unitNo;
            X = x;
        }

        public static GuideAction Spawn(float time, Side side, int unitNo, float x) => new GuideAction(time, side, unitNo, x);
        public static GuideAction Cannon(float time, Side side) => new GuideAction(time, side, CannonUnitNo, 0f);

        public bool IsCannon => UnitNo == CannonUnitNo;
    }

    /// <summary>あそびかたの1ページ。説明文と、後ろの戦場で流すデモの台本</summary>
    public class GuideTopic
    {
        public string Title { get; }
        public string Body { get; }
        /// <summary>この秒数でデモを最初からやり直す。城が落ちる前・ユニットが画面外に出る前に戻すこと</summary>
        public float LoopSeconds { get; }
        /// <summary>Time の早い順に並べる</summary>
        public IReadOnlyList<GuideAction> Actions { get; }

        public GuideTopic(string title, string body, float loopSeconds, params GuideAction[] actions)
        {
            Title = title;
            Body = body;
            LoopSeconds = loopSeconds;
            Actions = actions;
        }
    }

    /// <summary>
    /// あそびかたのページ一覧。トピックを足すときはここに1つ足すだけでよい。
    /// 説明文の数値は UnitDefinitions の定数・PenguinWarsBalance と手で合わせているので、バランスを変えたらここも直す。
    /// X は左の城が 0。タイトル中のカメラは左端（だいたい X=-2〜14）を映しているので、その中に置く
    /// </summary>
    public static class GuideTopics
    {
        private const Side Ally = Side.Left;
        private const Side Enemy = Side.Right;
        private const float DefaultLoop = 10f;

        // 出てくるキャラの No（UnitDefinitions）
        private const int Penguin = 1;
        private const int WallPenguin = 2;
        private const int HelmetPenguin = 4;
        private const int AxePenguin = 11;
        private const int DrillPenguin = 20;
        private const int FreezePenguin = 34;
        private const int FanPenguin = 35;
        private const int StickyPenguin = 37;

        public static readonly IReadOnlyList<GuideTopic> All = new[]
        {
            new GuideTopic("城キラー",
                "城へのダメージが3倍になる（ユニットへの攻撃はふつう）。\n" +
                "ドリル・バイク・ロケット・せんしゃが持っている。城に着く前に止めよう。",
                DefaultLoop,
                GuideAction.Spawn(0f, Enemy, DrillPenguin, 6f)),

            new GuideTopic("城の守り方",
                "単体攻撃の敵は、城よりも目の前のユニットを先に殴る。城に張り付かれたら壁を出そう。\n" +
                "範囲攻撃（せんしゃなど）は城もまとめて殴るので、ペンギン砲で押し返す。",
                DefaultLoop,
                GuideAction.Spawn(0f, Enemy, DrillPenguin, 6f),
                GuideAction.Spawn(3f, Ally, WallPenguin, 1.5f),
                GuideAction.Spawn(5f, Ally, AxePenguin, 1.5f)),

            new GuideTopic("ふっとばす",
                "攻撃が当たると、確率で相手を後ろに飛ばす（妨害は50%・ほかは30%）。\n" +
                "飛ばされている間は動けない。（デモでは毎回発動）",
                DefaultLoop,
                GuideAction.Spawn(0f, Enemy, FanPenguin, 8f),
                GuideAction.Spawn(0f, Ally, Penguin, 2f)),

            new GuideTopic("止める",
                "攻撃が当たると、40%で相手を2秒間止める。止まっている間は歩くのも攻撃もできない。\n" +
                "（デモでは毎回発動）",
                DefaultLoop,
                GuideAction.Spawn(0f, Enemy, FreezePenguin, 8f),
                GuideAction.Spawn(0f, Ally, AxePenguin, 2f)),

            new GuideTopic("遅くする",
                "攻撃が当たると、50%で相手の歩く速さを3秒間半分にする。\n" +
                "射程の長い味方が来るまでの時間かせぎに強い。（デモでは毎回発動）",
                DefaultLoop,
                GuideAction.Spawn(0f, Enemy, StickyPenguin, 9f),
                GuideAction.Spawn(0f, Ally, Penguin, 2f),
                GuideAction.Spawn(1.5f, Ally, Penguin, 2f)),

            new GuideTopic("ふんばる",
                "どんな攻撃でも後ろに飛ばされない（ペンギン砲でも飛ばない）。\n" +
                "前のヘルメットペンギンはふんばる、後ろのペンギンは飛ばされる。",
                DefaultLoop,
                GuideAction.Spawn(0f, Enemy, FanPenguin, 8f),
                GuideAction.Spawn(0f, Ally, HelmetPenguin, 2.5f),
                GuideAction.Spawn(0f, Ally, Penguin, 1.5f)),

            new GuideTopic("ペンギン砲",
                "40秒でたまる。城の近く（戦場の6割まで）の敵全員に100ダメージ＋後ろに飛ばす。\n" +
                "城に張り付かれたときの立て直しに取っておこう。",
                DefaultLoop,
                GuideAction.Spawn(0f, Enemy, Penguin, 5f),
                GuideAction.Spawn(0f, Enemy, Penguin, 6.5f),
                GuideAction.Spawn(0f, Enemy, Penguin, 8f),
                GuideAction.Cannon(3.5f, Ally)),
        };
    }
}
