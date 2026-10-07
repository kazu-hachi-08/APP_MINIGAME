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
        /// <summary>デモの戦場でなだれを起こすか</summary>
        public bool ShowsAvalanche { get; }
        /// <summary>0 でなければ、敵の城を叩いたときにこのキャラがボスとして出る（敵の城が画面に入る短い戦場になる）</summary>
        public int BossUnitNo { get; private set; }

        public GuideTopic(string title, string body, float loopSeconds, params GuideAction[] actions)
            : this(title, body, loopSeconds, false, actions)
        {
        }

        public GuideTopic(string title, string body, float loopSeconds, bool showsAvalanche, params GuideAction[] actions)
        {
            Title = title;
            Body = body;
            LoopSeconds = loopSeconds;
            ShowsAvalanche = showsAvalanche;
            Actions = actions;
        }

        /// <summary>ボスを出すページだけに付ける（1ページのためにコンストラクタの引数を増やさないため）</summary>
        public GuideTopic WithBoss(int unitNo)
        {
            BossUnitNo = unitNo;
            return this;
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
        // デモのなだれ（GuideDemoDirector._avalancheInterval = 3.5秒目）が1回起きて、押し戻されたところまで見せる
        private const float AvalancheLoop = 6f;
        // 大型は体力が多く、キラーでも倒し切るまで11秒ほどかかる
        private const float LargeKillerLoop = 12f;
        // 1秒ほどでボスが出て、WARNING・カメラ寄り（約2.5秒）の後に味方を倒して歩き出すところまで
        private const float BossLoop = 8f;
        // ボスのデモの短い戦場（GuideDemoDirector._bossFieldLength = 14）で、出してすぐ敵の城に届く位置
        private const float NearEnemyCastleX = 10f;
        // 役割の説明か能力の説明かをタイトルで見分けられるようにする（同じ「大型」「遠距離」が両方に出てくるため）
        private const string RolePrefix = "【役割】";
        private const string AbilityPrefix = "【能力】";

        // 出てくるキャラの No（UnitDefinitions）
        private const int Penguin = 1;
        private const int WallPenguin = 2;
        private const int SnowballPenguin = 3;
        private const int HelmetPenguin = 4;
        private const int AxePenguin = 11;
        private const int FishSwordPenguin = 12;
        private const int BoxerPenguin = 13;
        private const int LongLegPenguin = 15;
        private const int DrillPenguin = 20;
        private const int BowPenguin = 23;
        private const int SnowThrowPenguin = 24;
        private const int SniperPenguin = 29;
        private const int BoomerangPenguin = 30;
        private const int FreezePenguin = 34;
        private const int FanPenguin = 35;
        private const int StickyPenguin = 37;
        private const int GiantPenguin = 43;
        private const int IcebergPenguin = 46;
        private const int WhalePenguin = 47;

        public static readonly IReadOnlyList<GuideTopic> All = new[]
        {
            // 遊び方の基本 → ステージモードの遊び方 → 戦い方 → 役割 → 能力 → ステージの仕掛け（ボス・なだれ）の順。最初のページで「何をすれば勝ちか」が分かるようにする。
            // 役割・能力は enum（UnitRole / UnitAbilityType）と同じ順にし、ずかんのしぼりこみの並びとそろえる
            new GuideTopic("出撃と勝ち方",
                "下のボタンを押すと、さかなを払ってペンギンが出撃する。ペンギンは前に歩いて敵を殴る。\n" +
                "相手の城を先に落とせば勝ち。\n" +
                "オンライン対戦では、10体目に自分で作った「じぶんペンギン」が入る。",
                DefaultLoop,
                GuideAction.Spawn(0f, Ally, FishSwordPenguin, 1.5f),
                GuideAction.Spawn(1f, Ally, Penguin, 1.5f),
                GuideAction.Spawn(0f, Enemy, Penguin, 6f)),

            new GuideTopic("さかなと働きペンギン",
                "さかなは時間でたまり、敵を倒しても増える。\n" +
                "左下の働きペンギンにさかなを払うと、たまる速さと上限が上がる。高いキャラを出す前に上げておこう。",
                DefaultLoop,
                GuideAction.Spawn(0f, Ally, FishSwordPenguin, 3f),
                GuideAction.Spawn(0.5f, Ally, FishSwordPenguin, 3f),
                GuideAction.Spawn(0f, Enemy, Penguin, 6f),
                GuideAction.Spawn(2f, Enemy, Penguin, 8f),
                GuideAction.Spawn(4f, Enemy, Penguin, 10f)),

            new GuideTopic("ステージ",
                "スタート →「ステージ」で、3章18ステージを順に攻める。敵の城を落とせばクリア、自分の城が落ちたら失敗。\n" +
                "クリアすると次のステージが遊べるようになる。敵の出方はステージごとに決まっている。",
                DefaultLoop,
                GuideAction.Spawn(0f, Ally, WallPenguin, 1.5f),
                GuideAction.Spawn(1f, Ally, AxePenguin, 1.5f),
                GuideAction.Spawn(0f, Enemy, Penguin, 9f),
                GuideAction.Spawn(2f, Enemy, Penguin, 10f),
                GuideAction.Spawn(4f, Enemy, SnowballPenguin, 10f)),

            new GuideTopic("★（ほし）",
                "★1 クリア ／ ★2 自分の城のHPを半分以上残してクリア ／ ★3 目標タイム以内にクリア。\n" +
                "★は記録だけ（集めなくても先に進める）。守り切るか、速攻で落とすか、遊び方を変えて狙おう。",
                DefaultLoop,
                GuideAction.Spawn(0f, Ally, BoxerPenguin, 2f),
                GuideAction.Spawn(0.5f, Ally, BoxerPenguin, 1.5f),
                GuideAction.Spawn(1f, Ally, SnowThrowPenguin, 1f),
                GuideAction.Spawn(0f, Enemy, Penguin, 9f),
                GuideAction.Spawn(1f, Enemy, Penguin, 10f)),

            new GuideTopic("なかま",
                "最初のなかまは10体。ステージを初めてクリアすると、新しいなかまが加わる（全50体）。\n" +
                "大型のなかまは各章のボスを倒すと加わる。まだのなかまは、ずかんでは影だけ見える。",
                DefaultLoop,
                GuideAction.Spawn(0f, Ally, IcebergPenguin, 2f),
                GuideAction.Spawn(1.5f, Ally, WhalePenguin, 1.5f)),

            new GuideTopic("へんせい",
                "ステージ選択の「へんせい」で、なかまから10体を選んで出撃する。最後に決めた編成は保存される。\n" +
                "「大型禁止」などの制限があるステージでは、当てはまるキャラが暗くなって出せない。",
                DefaultLoop,
                GuideAction.Spawn(0f, Ally, WallPenguin, 3f),
                GuideAction.Spawn(0f, Ally, AxePenguin, 2f),
                GuideAction.Spawn(0f, Ally, BowPenguin, 1f),
                GuideAction.Spawn(0f, Enemy, Penguin, 9f),
                GuideAction.Spawn(1.5f, Enemy, Penguin, 10f)),

            new GuideTopic("城の守り方",
                "単体攻撃の敵は、城よりも目の前のユニットを先に殴る。城に張り付かれたら壁を出そう。\n" +
                "範囲攻撃（せんしゃなど）は城もまとめて殴るので、ペンギン砲で押し返す。",
                DefaultLoop,
                GuideAction.Spawn(0f, Enemy, DrillPenguin, 6f),
                GuideAction.Spawn(3f, Ally, WallPenguin, 1.5f),
                GuideAction.Spawn(5f, Ally, AxePenguin, 1.5f)),

            new GuideTopic("ペンギン砲",
                "40秒でたまる。城の近く（戦場の6割まで）の敵全員に100ダメージ＋後ろに飛ばす。\n" +
                "城に張り付かれたときの立て直しに取っておこう。",
                DefaultLoop,
                GuideAction.Spawn(0f, Enemy, Penguin, 5f),
                GuideAction.Spawn(0f, Enemy, Penguin, 6.5f),
                GuideAction.Spawn(0f, Enemy, Penguin, 8f),
                GuideAction.Cannon(3.5f, Ally)),

            new GuideTopic(RolePrefix + "壁",
                "安くて体力が多いが、攻撃は弱い。再生産が早いので、前に何体も並べて敵の足を止める。\n" +
                "まずは壁を出して、さかなをためる時間をかせごう。",
                DefaultLoop,
                GuideAction.Spawn(0f, Ally, WallPenguin, 3f),
                GuideAction.Spawn(1f, Ally, WallPenguin, 3f),
                GuideAction.Spawn(0f, Enemy, AxePenguin, 8f)),

            new GuideTopic(RolePrefix + "アタッカー",
                "近くで殴る主力。攻撃が高く、足も少し速い。そのぶん打たれ弱い。\n" +
                "壁が敵を止めている間に出して、まとめて倒そう。",
                DefaultLoop,
                GuideAction.Spawn(0f, Ally, WallPenguin, 3f),
                GuideAction.Spawn(1f, Ally, FishSwordPenguin, 1.5f),
                GuideAction.Spawn(0f, Enemy, Penguin, 8f),
                GuideAction.Spawn(1.5f, Enemy, Penguin, 9.5f)),

            // 壁の後ろから撃つのが遠距離の使い方そのものなので、デモは壁と並べて見せる
            new GuideTopic(RolePrefix + "遠距離",
                "射程が長く、後ろから撃つ。体力が少ないので、壁の後ろに置く。\n" +
                "単体攻撃は一番手前の敵しか狙えないので、壁の後ろにいれば狙われにくい。",
                DefaultLoop,
                GuideAction.Spawn(0f, Ally, WallPenguin, 3f),
                GuideAction.Spawn(0f, Ally, BowPenguin, 1f),
                GuideAction.Spawn(0f, Enemy, Penguin, 9f),
                GuideAction.Spawn(1.5f, Enemy, Penguin, 10f)),

            new GuideTopic(RolePrefix + "妨害",
                "攻撃は弱いが、ふっとばす・止める・遅くするの能力で敵の足を止める。\n" +
                "強い敵が来たら、壁の後ろから出して押し返そう。（デモでは毎回発動）",
                DefaultLoop,
                GuideAction.Spawn(0f, Ally, WallPenguin, 3f),
                GuideAction.Spawn(0f, Ally, FreezePenguin, 1.5f),
                GuideAction.Spawn(0f, Enemy, AxePenguin, 8f),
                GuideAction.Spawn(1f, Enemy, FishSwordPenguin, 9f)),

            new GuideTopic(RolePrefix + "大型",
                "とても高く、再生産も長いが、体力も攻撃もけたちがい。1体で戦場をひっくり返す。\n" +
                "大型キラーには弱いので、相手が持っていないときが出しどき。",
                DefaultLoop,
                GuideAction.Spawn(0f, Ally, IcebergPenguin, 2f),
                GuideAction.Spawn(0f, Enemy, Penguin, 7f),
                GuideAction.Spawn(0.5f, Enemy, Penguin, 8f),
                GuideAction.Spawn(1f, Enemy, AxePenguin, 9f)),

            new GuideTopic(AbilityPrefix + "ふっとばす",
                "攻撃が当たると、確率で相手を後ろに飛ばす（妨害は50%・ほかは30%）。\n" +
                "飛ばされている間は動けない。（デモでは毎回発動）",
                DefaultLoop,
                GuideAction.Spawn(0f, Enemy, FanPenguin, 8f),
                GuideAction.Spawn(0f, Ally, Penguin, 2f)),

            new GuideTopic(AbilityPrefix + "止める",
                "攻撃が当たると、40%で相手を2秒間止める。止まっている間は歩くのも攻撃もできない。\n" +
                "（デモでは毎回発動）",
                DefaultLoop,
                GuideAction.Spawn(0f, Enemy, FreezePenguin, 8f),
                GuideAction.Spawn(0f, Ally, AxePenguin, 2f)),

            new GuideTopic(AbilityPrefix + "遅くする",
                "攻撃が当たると、50%で相手の歩く速さを3秒間半分にする。\n" +
                "射程の長い味方が来るまでの時間かせぎに強い。（デモでは毎回発動）",
                DefaultLoop,
                GuideAction.Spawn(0f, Enemy, StickyPenguin, 9f),
                GuideAction.Spawn(0f, Ally, Penguin, 2f),
                GuideAction.Spawn(1.5f, Ally, Penguin, 2f)),

            new GuideTopic(AbilityPrefix + "城キラー",
                "城へのダメージが3倍になる（ユニットへの攻撃はふつう）。\n" +
                "ドリル・バイク・ロケット・せんしゃが持っている。城に着く前に止めよう。",
                DefaultLoop,
                GuideAction.Spawn(0f, Enemy, DrillPenguin, 6f)),

            new GuideTopic(AbilityPrefix + "ふんばる",
                "どんな攻撃でも後ろに飛ばされない（ペンギン砲でも飛ばない）。\n" +
                "前のヘルメットペンギンはふんばる、後ろのペンギンは飛ばされる。",
                DefaultLoop,
                GuideAction.Spawn(0f, Enemy, FanPenguin, 8f),
                GuideAction.Spawn(0f, Ally, HelmetPenguin, 2.5f),
                GuideAction.Spawn(0f, Ally, Penguin, 1.5f)),

            // キラーのデモは、ふつうのキャラに差し替えると同じ時間で倒し切れない配置にしてある（効き目が分かるように）
            new GuideTopic(AbilityPrefix + "大型キラー",
                "大型へのダメージが3倍（ほかの相手と城にはふつう）。おの・スナイパーが持っている。\n" +
                "相手がキングやロボを出したら、見てから合わせよう。",
                LargeKillerLoop,
                GuideAction.Spawn(0f, Enemy, IcebergPenguin, 6.5f),
                GuideAction.Spawn(0f, Ally, WallPenguin, 4f),
                GuideAction.Spawn(0f, Ally, SniperPenguin, 1f),
                GuideAction.Spawn(0f, Ally, AxePenguin, 3f),
                GuideAction.Spawn(0.5f, Ally, AxePenguin, 3f),
                GuideAction.Spawn(1f, Ally, AxePenguin, 3f),
                GuideAction.Spawn(1.5f, Ally, AxePenguin, 3f)),

            new GuideTopic(AbilityPrefix + "遠距離キラー",
                "遠距離へのダメージが3倍（ほかの相手と城にはふつう）。ながあし・ゆきなげが持っている。\n" +
                "後ろから撃ってくるゆみやたいほうに、近づいてまとめて当てよう。",
                DefaultLoop,
                GuideAction.Spawn(0f, Enemy, BowPenguin, 8f),
                GuideAction.Spawn(0f, Enemy, BowPenguin, 9f),
                GuideAction.Spawn(0f, Ally, WallPenguin, 4f),
                GuideAction.Spawn(0f, Ally, LongLegPenguin, 2f)),

            new GuideTopic(AbilityPrefix + "妨害キラー",
                "妨害へのダメージが3倍（ほかの相手と城にはふつう）。ハンマー・ブーメランが持っている。\n" +
                "止める・遅くするで前線が動かなくなったら出そう。",
                DefaultLoop,
                GuideAction.Spawn(0f, Enemy, StickyPenguin, 8f),
                GuideAction.Spawn(0f, Enemy, StickyPenguin, 8.5f),
                GuideAction.Spawn(0f, Ally, WallPenguin, 4f),
                GuideAction.Spawn(0f, Ally, BoomerangPenguin, 3f)),

            new GuideTopic("ボス",
                "敵の城を叩いてHPを減らすと、ボスが出てくるステージがある（各章の最後はボスステージ）。\n" +
                "「WARNING!」が出たら壁を並べて守りを固めよう。ボスのHPは画面の上に出る。",
                BossLoop,
                GuideAction.Spawn(0f, Ally, BowPenguin, NearEnemyCastleX),
                GuideAction.Spawn(0f, Ally, BowPenguin, NearEnemyCastleX - 0.5f),
                GuideAction.Spawn(0f, Ally, WallPenguin, NearEnemyCastleX + 1f)).WithBoss(GiantPenguin),

            new GuideTopic("なだれ",
                "なだれのあるステージ（オンラインは「なだれの谷」）では、決まった間隔で戦場の真ん中にいるユニット全員（敵も味方も）に150ダメージ＋後ろに飛ばす。\n" +
                "5秒前に「なだれ注意！」が出る。真ん中に出すのを少し待とう。（デモでは短い間隔）",
                AvalancheLoop, true,
                GuideAction.Spawn(0f, Ally, WallPenguin, 7f),
                GuideAction.Spawn(0f, Enemy, WallPenguin, 11f),
                GuideAction.Spawn(0f, Ally, Penguin, 2f)),
        };
    }
}
