using UnityEngine;

namespace MiniGame.LifeGame.Editor
{
    /// <summary>
    /// 3テーマの初期値（仕様書 §6）。アセットを作るときに1回だけ使う。
    /// 生成後の文面・色の調整はアセット（Data/Themes/LifeTheme_*.asset）を直接変える。
    /// </summary>
    internal sealed class LifeThemeDefaults
    {
        public string Id;
        public string Name;
        public string Currency;

        /// <summary>LifeRuleConfig.Jobs の並び（フリーター・一般1〜5・上級1〜3）</summary>
        public string[] Jobs;

        public string[] Houses;

        /// <summary>就職・大学・フリーター・安全・ギャンブル の順</summary>
        public string[] Routes;

        /// <summary>お金が増える・減る・災難・家族・買い物・職業・節目・その他 の順（LifeThemeData の色と同じ並び）</summary>
        public Color[] CellColors;

        public Color Background;
        public Color Road;
        public (LifeCellType Type, string[] Lines)[] Texts;

        public static readonly LifeThemeDefaults[] All = { Modern(), Fantasy(), Space() };

        private static LifeThemeDefaults Modern()
        {
            return new LifeThemeDefaults
            {
                Id = "Modern",
                Name = "現代",
                Currency = "万円",
                Jobs = new[] { "フリーター", "会社員", "警察官", "大工", "料理人", "保育士", "医者", "弁護士", "社長" },
                Houses = new[] { "小さい家", "普通の家", "豪邸" },
                Routes = new[] { "就職", "大学", "フリーター", "安全", "ギャンブル" },
                Background = new Color(0.55f, 0.75f, 0.5f),
                Road = new Color(0.45f, 0.4f, 0.35f),
                CellColors = new[]
                {
                    new Color(0.55f, 0.85f, 0.45f), new Color(0.9f, 0.5f, 0.45f), new Color(0.7f, 0.45f, 0.75f),
                    new Color(1f, 0.65f, 0.8f), new Color(0.45f, 0.75f, 0.9f), new Color(1f, 0.75f, 0.3f),
                    new Color(1f, 0.92f, 0.45f), new Color(0.85f, 0.85f, 0.85f),
                },
                Texts = new[]
                {
                    (LifeCellType.Income, new[] { "宝くじで小当たり！", "フリマアプリで不用品が売れた", "副業のイラストが好評", "懸賞に当たった", "落とし物を届けてお礼をもらった" }),
                    (LifeCellType.Expense, new[] { "スマホの画面を割った", "推しのライブに全通した", "引っ越し費用がかさんだ", "最新ゲーム機を衝動買い", "友達の結婚式が続いた" }),
                    (LifeCellType.Sickness, new[] { "インフルエンザで寝込んだ", "階段で転んで骨折", "虫歯の治療に通う", "健康診断で再検査" }),
                    (LifeCellType.Accident, new[] { "車をこすってしまった", "スピード違反で捕まった", "駐車違反の反則金", "自転車で信号無視" }),
                    (LifeCellType.Fire, new[] { "天ぷら油から火が！", "隣の家からもらい火", "電気ストーブの消し忘れ" }),
                    (LifeCellType.Birth, new[] { "元気な赤ちゃんが生まれた！", "家族がにぎやかになった", "パパ・ママになった！" }),
                    (LifeCellType.House, new[] { "住宅展示場に寄ってみた", "不動産屋で掘り出し物を発見", "マイホームの夢がふくらむ" }),
                    (LifeCellType.Insurance, new[] { "保険の営業さんがやってきた", "将来に備えて見直しを", "もしものときの備え" }),
                    (LifeCellType.Stock, new[] { "証券口座を開いた", "話題の銘柄が気になる", "投資セミナーに参加した" }),
                    (LifeCellType.ChangeJob, new[] { "転職サイトからスカウト", "ヘッドハンティングされた", "心機一転！" }),
                    (LifeCellType.Bet, new[] { "競馬場に寄り道した", "友達と勝負することに", "ここは一発勝負！" }),
                    (LifeCellType.Nominate, new[] { "貸したお金を返してもらう", "割り勘をおごってもらう", "誕生日プレゼントをねだった" }),
                    (LifeCellType.Present, new[] { "困っている友達に差し入れ", "お年玉をあげた", "募金箱を見かけた" }),
                    (LifeCellType.SwapJob, new[] { "隣の芝生は青い", "仕事を交換してみない？", "人事異動の季節" }),
                    (LifeCellType.Forward, new[] { "電車が予定より早く着いた", "近道を見つけた！", "絶好調で一気に進む" }),
                    (LifeCellType.Back, new[] { "忘れ物を取りに戻る", "道を間違えた…", "スマホを家に置いてきた" }),
                    (LifeCellType.Rest, new[] { "風邪で寝込んだ", "有給休暇を取った", "家でのんびり" }),
                    (LifeCellType.Lottery, new[] { "年末ジャンボを買った", "商店街の福引き", "スクラッチくじに挑戦" }),
                    (LifeCellType.Payday, new[] { "今月もおつかれさま", "待ちに待った給料日" }),
                    (LifeCellType.Marriage, new[] { "運命の人と出会った", "盛大な結婚式！" }),
                    (LifeCellType.Tuition, new[] { "学費の振り込み", "教科書代もばかにならない" }),
                    (LifeCellType.JobOffer, new[] { "就職活動スタート", "面接を突破した！" }),
                    (LifeCellType.Graduation, new[] { "卒業おめでとう！", "卒業論文を出し切った" }),
                    (LifeCellType.Goal, new[] { "人生のゴール！", "長い旅だった" }),
                },
            };
        }

        private static LifeThemeDefaults Fantasy()
        {
            return new LifeThemeDefaults
            {
                Id = "Fantasy",
                Name = "ファンタジー",
                Currency = "G",
                Jobs = new[] { "冒険者見習い", "商人", "衛兵", "鍛冶屋", "吟遊詩人", "農夫", "僧侶", "大魔導士", "王宮騎士" },
                Houses = new[] { "小屋", "石造りの家", "古城" },
                Routes = new[] { "ギルド", "魔法学院", "放浪", "街道", "魔の森" },
                Background = new Color(0.4f, 0.58f, 0.35f),
                Road = new Color(0.62f, 0.48f, 0.3f),
                CellColors = new[]
                {
                    new Color(0.75f, 0.85f, 0.45f), new Color(0.85f, 0.55f, 0.4f), new Color(0.65f, 0.45f, 0.7f),
                    new Color(0.95f, 0.7f, 0.75f), new Color(0.55f, 0.7f, 0.85f), new Color(0.95f, 0.75f, 0.35f),
                    new Color(1f, 0.85f, 0.4f), new Color(0.9f, 0.85f, 0.75f),
                },
                Texts = new[]
                {
                    (LifeCellType.Income, new[] { "宝箱を見つけた！", "スライム退治の報酬", "旅人を道案内してお礼", "酒場の賭けに勝った", "竜のウロコが高く売れた" }),
                    (LifeCellType.Expense, new[] { "装備を新調した", "酒場でおごりすぎた", "ポーションを買いだめ", "盗賊に財布をすられた" }),
                    (LifeCellType.Sickness, new[] { "毒キノコを食べた", "ゴブリンにかまれた", "呪いにかかった", "風邪をこじらせた" }),
                    (LifeCellType.Accident, new[] { "馬車が城門にぶつかった", "城下町で騒ぎを起こした", "通行税を払い忘れた" }),
                    (LifeCellType.Fire, new[] { "ドラゴンのくしゃみで火事！", "暖炉の火が燃え移った", "火の魔法の練習に失敗" }),
                    (LifeCellType.Birth, new[] { "小さな勇者が生まれた！", "妖精の祝福を受けた赤ちゃん", "家族がにぎやかになった" }),
                    (LifeCellType.House, new[] { "村はずれに空き家が", "領主が土地を売りに出した", "城の競売が開かれた" }),
                    (LifeCellType.Insurance, new[] { "教会の加護を受けられる", "守りの護符を勧められた", "もしものときの備え" }),
                    (LifeCellType.Stock, new[] { "商会が出資者を募集中", "交易船の株を勧められた", "ギルドのもうけ話" }),
                    (LifeCellType.ChangeJob, new[] { "ギルドに新しい依頼", "王の使者がスカウトに来た", "転職の神殿にたどり着いた" }),
                    (LifeCellType.Bet, new[] { "カジノの町に着いた", "サイコロ賭博に誘われた", "運命の女神に挑む！" }),
                    (LifeCellType.Nominate, new[] { "冒険の分け前を請求", "酒場のツケを取り立てる", "仲間に貢がせた" }),
                    (LifeCellType.Present, new[] { "貧しい村に施しを", "旅人にパンを分けた", "教会に寄付した" }),
                    (LifeCellType.SwapJob, new[] { "入れ替わりの魔法が暴発！", "身分を交換してみた", "王子と乞食ごっこ" }),
                    (LifeCellType.Forward, new[] { "転移の魔法陣を踏んだ", "飛竜に乗せてもらった", "追い風の加護" }),
                    (LifeCellType.Back, new[] { "迷いの森で道に迷った", "罠にかかって押し戻された", "宿に剣を忘れた" }),
                    (LifeCellType.Rest, new[] { "宿屋でひと晩休む", "眠りの呪いにかかった", "毒で動けない…" }),
                    (LifeCellType.Lottery, new[] { "王国の富くじを買った", "妖精のくじ引き", "女神の宝くじ" }),
                    (LifeCellType.Payday, new[] { "報酬の日だ！", "ギルドから報酬が届いた" }),
                    (LifeCellType.Marriage, new[] { "酒場で運命の出会い", "教会で結婚式！" }),
                    (LifeCellType.Tuition, new[] { "魔導書代を払う", "学院の授業料" }),
                    (LifeCellType.JobOffer, new[] { "冒険者ギルドに登録", "仕事を探そう" }),
                    (LifeCellType.Graduation, new[] { "魔法学院を卒業！", "最終試験に合格" }),
                    (LifeCellType.Goal, new[] { "伝説となった", "冒険の終わり" }),
                },
            };
        }

        private static LifeThemeDefaults Space()
        {
            return new LifeThemeDefaults
            {
                Id = "Space",
                Name = "宇宙",
                Currency = "クレジット",
                Jobs = new[] { "流れ者", "貨物パイロット", "保安官", "整備士", "配信者", "プラント員", "宇宙医師", "艦長", "研究者" },
                Houses = new[] { "カプセル居住区", "コロニーの一軒家", "小惑星まるごと" },
                Routes = new[] { "宇宙港", "アカデミー", "漂流", "定期航路", "小惑星帯" },
                Background = new Color(0.08f, 0.08f, 0.18f),
                Road = new Color(0.5f, 0.55f, 0.7f),
                CellColors = new[]
                {
                    new Color(0.4f, 0.9f, 0.6f), new Color(0.95f, 0.45f, 0.5f), new Color(0.75f, 0.5f, 0.95f),
                    new Color(1f, 0.6f, 0.85f), new Color(0.4f, 0.8f, 1f), new Color(1f, 0.8f, 0.35f),
                    new Color(1f, 0.95f, 0.5f), new Color(0.75f, 0.78f, 0.85f),
                },
                Texts = new[]
                {
                    (LifeCellType.Income, new[] { "宇宙ゴミからレアメタル", "異星人に翻訳を頼まれた", "無重力レースで優勝", "隕石を拾って売った" }),
                    (LifeCellType.Expense, new[] { "酸素ボンベを買い足した", "宇宙食のサブスク", "船の塗装を塗り替えた", "ワープ代が高かった" }),
                    (LifeCellType.Sickness, new[] { "宇宙酔いでダウン", "未知のウイルスに感染", "無重力で腰を痛めた" }),
                    (LifeCellType.Accident, new[] { "速度超過で止められた", "ドッキングに失敗", "航路違反の罰金" }),
                    (LifeCellType.Fire, new[] { "エンジンが火を噴いた！", "太陽フレアで回路が炎上", "調理ロボが暴走" }),
                    (LifeCellType.Birth, new[] { "船内で赤ちゃん誕生！", "未来の宇宙飛行士が生まれた", "家族がにぎやかになった" }),
                    (LifeCellType.House, new[] { "新コロニーの入居者募集", "テラフォーム済みの土地", "小惑星が売りに出た" }),
                    (LifeCellType.Insurance, new[] { "宇宙保険の案内が届いた", "銀河共済に入る？", "もしものときの備え" }),
                    (LifeCellType.Stock, new[] { "宇宙開発企業の株", "ワープ技術のベンチャー", "月面リゾートに出資" }),
                    (LifeCellType.ChangeJob, new[] { "星間求人ネットで募集", "艦隊からスカウト", "新天地を目指す" }),
                    (LifeCellType.Bet, new[] { "宇宙カジノに寄港", "無重力ルーレットに挑戦", "銀河一の勝負師になる！" }),
                    (LifeCellType.Nominate, new[] { "燃料代を請求した", "宇宙ステーションの割り勘", "翻訳料を取り立てる" }),
                    (LifeCellType.Present, new[] { "漂流船に物資を届けた", "コロニーに寄付した", "異星人にお土産を" }),
                    (LifeCellType.SwapJob, new[] { "転送装置の誤作動！", "クローンと入れ替わった", "艦内の配置換え" }),
                    (LifeCellType.Forward, new[] { "ワープ航法に成功！", "流星に乗って加速", "スイングバイで加速" }),
                    (LifeCellType.Back, new[] { "小惑星帯で引き返す", "燃料切れで漂流", "ブラックホールに引っ張られた" }),
                    (LifeCellType.Rest, new[] { "コールドスリープ中", "宇宙酔いでダウン", "メンテナンスで足止め" }),
                    (LifeCellType.Lottery, new[] { "銀河宝くじを買った", "宇宙港のくじ引き", "隕石の欠片くじ" }),
                    (LifeCellType.Payday, new[] { "クレジットが入金された", "報酬が振り込まれた" }),
                    (LifeCellType.Marriage, new[] { "星の見える式場で結婚！", "別の星の人と恋に落ちた" }),
                    (LifeCellType.Tuition, new[] { "アカデミーの授業料", "シミュレーター代" }),
                    (LifeCellType.JobOffer, new[] { "宇宙港で職探し", "乗組員募集！" }),
                    (LifeCellType.Graduation, new[] { "アカデミーを卒業！", "最終航行試験に合格" }),
                    (LifeCellType.Goal, new[] { "最果ての星に到着", "旅の終わり" }),
                },
            };
        }
    }
}
