using System;
using System.Collections;
using System.Collections.Generic;
using MiniGame.Common.Scene;
using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 試合の進行（§12）。設定 → ホール開始 →「○○の番」→ ショット → 結果 を繰り返し、ホールごとにスコアカードを出す。
    /// ボールの実体は1つだけで、手番の人の止まっていた位置に置き直して打たせる（他の人のボールは OtherBallsView が表示だけする）。
    /// </summary>
    public class GolfGameManager : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;
        [SerializeField] private HoleLoader _holeLoader;
        [SerializeField] private ShotInput _input;
        [SerializeField] private BallView _ballView;
        [SerializeField] private GolfSetupPanel _setupPanel;
        [SerializeField] private GolfTurnBannerView _turnBanner;
        [SerializeField] private ScoreCardView _scoreCard;

        [Tooltip("ボールが止まってから次の人の番を出すまでの時間（秒）。止まった場所を見せる")]
        [SerializeField] private float _shotResultDelay = 1f;

        private readonly List<GolfPlayerSlot> _slots = new List<GolfPlayerSlot>();
        private readonly List<GolfHoleData> _holes = new List<GolfHoleData>();
        private List<int> _teeOrder = new List<int>();
        private bool _shotFinished;

        public GolfPhase Phase { get; private set; } = GolfPhase.Setup;
        public IReadOnlyList<GolfPlayerSlot> Slots => _slots;

        /// <summary>手番の人の添字。試合前は -1</summary>
        public int CurrentPlayer { get; private set; } = -1;

        /// <summary>何ホール目か（0始まり）と全ホール数。HUD の「H1/3」表示に使う</summary>
        public int HoleNumber { get; private set; }
        public int HoleCount => _holes.Count;

        public event Action TurnStarted;

        private GolfPlayerSlot Current => _slots[CurrentPlayer];

        private void Awake()
        {
            // 「○○の番」をタップするまで打てないようにする
            _input.enabled = false;
        }

        private void OnEnable()
        {
            _ball.Launched += OnBallLaunched;
            _ball.Penalized += OnBallPenalized;
            _ball.Stopped += OnBallStopped;
        }

        private void OnDisable()
        {
            _ball.Launched -= OnBallLaunched;
            _ball.Penalized -= OnBallPenalized;
            _ball.Stopped -= OnBallStopped;
        }

        private void Start()
        {
            _setupPanel.Show(_holeLoader.Holes.Count, (holeCount, playerCount) => StartCoroutine(PlayMatch(holeCount, playerCount)));
        }

        private IEnumerator PlayMatch(int holeCount, int playerCount)
        {
            SetUpPlayers(playerCount);
            PickHoles(holeCount);

            for (HoleNumber = 0; HoleNumber < _holes.Count; HoleNumber++)
            {
                yield return PlayHole(_holes[HoleNumber]);
                _teeOrder = GolfRules.NextTeeOrder(_teeOrder, _slots);
            }

            ShowGameSet();
        }

        /// <summary>§6.3 1ホール目のティーは席順</summary>
        private void SetUpPlayers(int playerCount)
        {
            _slots.Clear();
            _teeOrder.Clear();
            for (int i = 0; i < playerCount; i++)
            {
                _slots.Add(new GolfPlayerSlot(i));
                _teeOrder.Add(i);
            }
        }

        private void PickHoles(int holeCount)
        {
            _holes.Clear();
            foreach (int index in GolfRules.PickHoles(_holeLoader.Holes.Count, holeCount, new System.Random()))
            {
                _holes.Add(_holeLoader.Holes[index]);
            }
        }

        private IEnumerator PlayHole(GolfHoleData hole)
        {
            Phase = GolfPhase.HoleStart;
            _holeLoader.Load(hole);
            foreach (GolfPlayerSlot slot in _slots) slot.StartHole(ToNumerics(_ball.GroundPosition));

            CurrentPlayer = GolfRules.NextPlayer(_slots, _teeOrder, ToNumerics(_ball.CupPosition));
            PlaceCurrentBall();
            yield return _turnBanner.Play(HoleTitle(), HoleDetail(hole), Color.white);

            string lastResult = string.Empty;
            while (CurrentPlayer >= 0)
            {
                yield return PlayTurn(lastResult);
                lastResult = FinishShot(hole);

                Phase = GolfPhase.ShotResult;
                yield return new WaitForSeconds(_shotResultDelay);

                CurrentPlayer = GolfRules.NextPlayer(_slots, _teeOrder, ToNumerics(_ball.CupPosition));
            }

            Phase = GolfPhase.HoleResult;
            bool next = false;
            string nextLabel = HoleNumber + 1 < _holes.Count ? "次のホールへ" : "結果を見る";
            _scoreCard.Show($"ホール{HoleNumber + 1} 終了", _holes, _slots, false, nextLabel, () => next = true, null);
            yield return new WaitUntil(() => next);
        }

        /// <summary>前のショットの結果と「○○の番」を出し、タップ後にボールが止まるまで待つ</summary>
        private IEnumerator PlayTurn(string lastResult)
        {
            Phase = GolfPhase.TurnStart;
            PlaceCurrentBall();
            TurnStarted?.Invoke();

            string name = GolfPlayerColors.Name(Current.Seat);
            yield return _turnBanner.Play($"{name} の番", lastResult, GolfPlayerColors.Get(Current.Seat));

            Phase = GolfPhase.Aiming;
            _shotFinished = false;
            _input.enabled = true;
            yield return new WaitUntil(() => _shotFinished);
            _input.enabled = false;
        }

        /// <summary>カメラが次の人のボールへ寄れるよう、バナーを出す前に置き直して構えておく</summary>
        private void PlaceCurrentBall()
        {
            if (CurrentPlayer < 0) return;

            _ball.Place(ToUnity(Current.Position));
            _ballView.SetPlayerColor(GolfPlayerColors.Get(Current.Seat));
            _input.PrepareShot();
        }

        /// <summary>止まった位置を覚え、カップイン・打ち切りを決める。次のバナーに出す結果を返す</summary>
        private string FinishShot(GolfHoleData hole)
        {
            Current.Position = ToNumerics(_ball.GroundPosition);
            string name = GolfPlayerColors.Name(Current.Seat);

            if (_ball.IsInCup)
            {
                Current.HoleOut();
                return $"{name} カップイン！（{Current.Strokes}打）";
            }

            if (GolfRules.ShouldGiveUp(Current.Strokes, hole.Par))
            {
                Current.GiveUp(GolfRules.StrokeLimit(hole.Par));
                return $"{name} ギブアップ（パー×2）";
            }

            return string.Empty;
        }

        private void ShowGameSet()
        {
            Phase = GolfPhase.GameSet;
            CurrentPlayer = -1;
            _scoreCard.Show("試合終了", _holes, _slots, true, "もう一度", Retry, ReturnToTitle);
        }

        private string HoleTitle()
        {
            return _holes.Count > 1 ? $"ホール {HoleNumber + 1}/{_holes.Count}" : "1ホール勝負";
        }

        private string HoleDetail(GolfHoleData hole)
        {
            return $"{hole.DisplayName}  PAR{hole.Par}\n風 {Mathf.RoundToInt(_holeLoader.CurrentWind.Strength)}m";
        }

        private void OnBallLaunched()
        {
            Phase = GolfPhase.BallMoving;
            Current.AddStrokes(1);
        }

        private void OnBallPenalized(GroundType ground)
        {
            Current.AddStrokes(GolfRules.PenaltyStrokes);
        }

        private void OnBallStopped()
        {
            _shotFinished = true;
        }

        private static void Retry()
        {
            if (SceneLoader.HasInstance)
            {
                SceneLoader.Instance.RestartCurrentScene();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.Golf);
            }
        }

        // GolfScene をエディタで直接再生したときなど SceneLoader が無い場合でも戻れるようにする
        private static void ReturnToTitle()
        {
            if (SceneLoader.HasInstance)
            {
                SceneLoader.Instance.LoadTitleScene();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.Title);
            }
        }

        private static Vector2 ToUnity(System.Numerics.Vector2 v) => new Vector2(v.X, v.Y);
        private static System.Numerics.Vector2 ToNumerics(Vector2 v) => new System.Numerics.Vector2(v.x, v.y);
    }
}
