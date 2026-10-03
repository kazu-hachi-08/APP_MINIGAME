using System;
using UnityEngine;

namespace MiniGame.LifeGame
{
    /// <summary>マスの種類ごとの文面（仕様書 §6.1）</summary>
    [Serializable]
    public class LifeCellTexts
    {
        public LifeCellType Type;
        public string[] Lines;
    }

    /// <summary>
    /// 1テーマ分の表示名・通貨・職業名・文面・色（仕様書 §6）。道の形・ルール・金額は3テーマ共通なので持たない。
    /// 1テーマ1アセットに分け、2人で別々のテーマを同時に書いてもコンフリクトしないようにしている。
    /// </summary>
    [CreateAssetMenu(fileName = "LifeTheme_", menuName = "MiniGame/LifeGame/Theme Data")]
    public class LifeThemeData : ScriptableObject
    {
        [SerializeField] private string _displayName = "現代";
        [SerializeField] private string _currency = "万円";

        [Tooltip("LifeRuleConfig.Jobs と同じ並び（フリーター・一般1〜5・上級1〜3）")]
        [SerializeField] private string[] _jobNames = new string[0];

        [Tooltip("LifeRuleConfig.Houses と同じ並び（安い順）")]
        [SerializeField] private string[] _houseNames = new string[0];

        [Header("Route")]
        [SerializeField] private string _jobRoute = "就職";
        [SerializeField] private string _universityRoute = "大学";
        [SerializeField] private string _freeterRoute = "フリーター";
        [SerializeField] private string _safeRoute = "安全";
        [SerializeField] private string _gambleRoute = "ギャンブル";

        [Tooltip("止まったマスで金額と一緒に出す一言。盤面生成で決めた番号（LifeCell.TextVariant）で1つ選ぶ")]
        [SerializeField] private LifeCellTexts[] _cellTexts = new LifeCellTexts[0];

        [Header("Color")]
        [SerializeField] private Color _background = new Color(0.55f, 0.75f, 0.5f);
        [SerializeField] private Color _road = new Color(0.45f, 0.4f, 0.35f);
        [SerializeField] private Color _moneyGood = new Color(0.55f, 0.85f, 0.45f);
        [SerializeField] private Color _moneyBad = new Color(0.9f, 0.5f, 0.45f);
        [SerializeField] private Color _mishap = new Color(0.7f, 0.45f, 0.75f);
        [SerializeField] private Color _family = new Color(1f, 0.65f, 0.8f);
        [SerializeField] private Color _purchase = new Color(0.45f, 0.75f, 0.9f);
        [SerializeField] private Color _job = new Color(1f, 0.75f, 0.3f);
        [SerializeField] private Color _milestone = new Color(1f, 0.92f, 0.45f);
        [SerializeField] private Color _plain = new Color(0.85f, 0.85f, 0.85f);

        [Header("Art")]
        [Tooltip("コマの乗り物の車体。白〜灰色で描き、席の色を掛けて使う")]
        [SerializeField] private Sprite _vehicleBody;
        [Tooltip("車体の上に重ねる窓・車輪など、席の色に染めない部分")]
        [SerializeField] private Sprite _vehicleDetail;
        [Tooltip("盤面の後ろに敷き詰める背景。色はそのまま出す（背景色 _background と同じ地の色で描く）")]
        [SerializeField] private Sprite _backgroundTile;

        [Header("Landmark")]
        [Tooltip("節目のマスの横に建てる建物。盤面を進むと人生の場面が変わっていくように見せる")]
        [SerializeField] private Sprite _landmarkStart;
        [SerializeField] private Sprite _landmarkJob;
        [SerializeField] private Sprite _landmarkSchool;
        [SerializeField] private Sprite _landmarkWedding;
        [SerializeField] private Sprite _landmarkGoal;

        [Tooltip("道の外に散らす飾り（木・岩など）。どれを置くかは位置で決まる")]
        [SerializeField] private Sprite[] _decorations = new Sprite[0];

        public string DisplayName => _displayName;
        public string Currency => _currency;
        public Color Background => _background;
        public Color Road => _road;
        public Sprite VehicleBody => _vehicleBody;
        public Sprite VehicleDetail => _vehicleDetail;
        public Sprite BackgroundTile => _backgroundTile;
        public Sprite[] Decorations => _decorations;

        /// <summary>建物を建てる節目のマスなら建物の絵、それ以外（と絵の無いテーマ）は null</summary>
        public Sprite Landmark(LifeCellType type)
        {
            switch (type)
            {
                case LifeCellType.Start: return _landmarkStart;
                case LifeCellType.JobOffer: return _landmarkJob;
                case LifeCellType.Graduation: return _landmarkSchool;
                case LifeCellType.Marriage: return _landmarkWedding;
                case LifeCellType.Goal: return _landmarkGoal;
                default: return null;
            }
        }

        public string JobName(int jobId) => _jobNames[jobId];

        public string HouseName(int houseId) => _houseNames[houseId];

        /// <summary>分岐の行き先の名前。分岐の無い区間は null</summary>
        public string RouteName(LifeSection section)
        {
            switch (section)
            {
                case LifeSection.Job: return _jobRoute;
                case LifeSection.University: return _universityRoute;
                case LifeSection.Freeter: return _freeterRoute;
                case LifeSection.Safe: return _safeRoute;
                case LifeSection.Gamble: return _gambleRoute;
                default: return null;
            }
        }

        /// <summary>文面が無い種類は null。variant は文面の数で割った余りで使う（盤面はテーマより先に決まり、文面の数を知らないため）</summary>
        public string CellText(LifeCellType type, int variant)
        {
            foreach (LifeCellTexts texts in _cellTexts)
            {
                if (texts.Type != type || texts.Lines == null || texts.Lines.Length == 0) continue;

                return texts.Lines[variant % texts.Lines.Length];
            }

            return null;
        }

        /// <summary>種類をまとめて色分けする（お金が増える／減る／災難／家族／買い物／職業／節目）。マスが多いので種類ごとに全部違う色にすると逆に見分けにくい</summary>
        public Color CellColor(LifeCellType type)
        {
            switch (type)
            {
                case LifeCellType.Income:
                case LifeCellType.Payday:
                case LifeCellType.Nominate:
                case LifeCellType.Bet:
                    return _moneyGood;
                case LifeCellType.Expense:
                case LifeCellType.Tuition:
                case LifeCellType.Present:
                    return _moneyBad;
                case LifeCellType.Sickness:
                case LifeCellType.Accident:
                case LifeCellType.Fire:
                    return _mishap;
                case LifeCellType.Birth:
                case LifeCellType.Marriage:
                    return _family;
                case LifeCellType.House:
                case LifeCellType.Insurance:
                case LifeCellType.Stock:
                    return _purchase;
                case LifeCellType.JobOffer:
                case LifeCellType.Graduation:
                case LifeCellType.ChangeJob:
                case LifeCellType.SwapJob:
                    return _job;
                case LifeCellType.Start:
                case LifeCellType.Goal:
                    return _milestone;
                default:
                    return _plain;
            }
        }
    }
}
