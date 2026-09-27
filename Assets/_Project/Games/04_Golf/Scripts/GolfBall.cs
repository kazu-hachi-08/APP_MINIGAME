using System;
using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// BallSimulator をフレームに合わせて進める窓口。
    /// フレーム時間をため、固定の時間刻みぶんずつ進めることで、フレームレートが違う端末でも同じ飛び方になる。
    /// 見た目は持たず、BallView / ShadowView がここを読んで描く。
    /// </summary>
    public class GolfBall : MonoBehaviour
    {
        [SerializeField] private GolfPhysicsSettings _settings;
        [SerializeField] private GolfTerrainSettings _terrainSettings;

        [Tooltip("池・OBに入った場所を見せてから、打ち直しの位置へ移すまでの時間（秒）")]
        [SerializeField] private float _dropDelay = 1.2f;

        private BallSimulator _simulator;
        private float _accumulatedTime;
        private bool _isDropPending;
        private float _dropTimer;

        /// <summary>地面の平面座標（x:左右 / y:奥行きZ）。ワールド座標の XY にそのまま対応する</summary>
        public Vector2 GroundPosition => ToUnity(Simulator.Position);
        public float Height => Simulator.Height;

        /// <summary>打ってから次に打てるようになるまで true。池・OBの打ち直し待ちも含めて、その間は狙えないようにする</summary>
        public bool IsMoving => Simulator.IsMoving || _isDropPending;

        public bool IsInCup => Simulator.IsInCup;
        public GroundType Ground => Simulator.Ground;

        /// <summary>今いるライでのインパクトゾーンの幅の割合</summary>
        public float ImpactZoneRate => Simulator.Lie.ImpactZoneRate;

        /// <summary>最後に打った位置。飛距離の表示に使う</summary>
        public Vector2 LaunchPosition { get; private set; }

        public Vector2 CupPosition { get; private set; }

        public event Action Launched;

        /// <summary>池・OBに入った（引数は入った地面）。この後、打ち直しの位置へ移ってから Stopped が来る</summary>
        public event Action<GroundType> Penalized;

        public event Action Stopped;

        /// <summary>NPC の試し打ち（TrySimulate）用。ボールを動かすときは Hit / Place を使うこと</summary>
        public BallSimulator Simulation => Simulator;

        // 他のコンポーネントの Awake から参照されても良いように遅延生成する
        private BallSimulator Simulator => _simulator ??= new BallSimulator(_settings.Ball, _terrainSettings.Terrain);

        /// <summary>ホールの地面・傾斜・カップ・風で計算し直し、ティーに置く</summary>
        public void SetCourse(IGroundMap ground, ISlopeMap slope, Vector2 teePosition, Vector2 cupPosition, Wind wind)
        {
            _simulator = new BallSimulator(_settings.Ball, _terrainSettings.Terrain, ground, slope);
            _simulator.SetCup(ToNumerics(cupPosition));
            _simulator.SetWind(wind);
            CupPosition = cupPosition;
            Place(teePosition);
        }

        public void Place(Vector2 groundPosition)
        {
            Simulator.Place(ToNumerics(groundPosition));
            _accumulatedTime = 0f;
            _isDropPending = false;
        }

        /// <summary>power は 0〜1、impactOffset は ShotRequest と同じ。動いている間とカップインした後は打てない</summary>
        public void Hit(Vector2 direction, ClubConfig club, float power, float impactOffset)
        {
            if (IsMoving || IsInCup) return;

            LaunchPosition = GroundPosition;
            Simulator.Launch(new ShotRequest(ToNumerics(direction), club, power, impactOffset));

            _accumulatedTime = 0f;
            if (IsMoving) Launched?.Invoke();
        }

        /// <summary>今の位置からフルパワー・まっすぐで打ったときの着地点（パターは止まる位置）</summary>
        public Vector2 PredictFullPower(ClubConfig club, Vector2 direction)
        {
            return ToUnity(Simulator.PredictFullPower(club, ToNumerics(direction)));
        }

        private void Update()
        {
            if (Simulator.IsMoving)
            {
                AdvanceSimulation();
                if (!Simulator.IsMoving) OnSimulationStopped();
            }
            else if (_isDropPending)
            {
                _dropTimer -= Time.deltaTime;
                if (_dropTimer <= 0f) Drop();
            }
        }

        private void AdvanceSimulation()
        {
            _accumulatedTime += Time.deltaTime;
            while (_accumulatedTime >= Simulator.TimeStep && Simulator.IsMoving)
            {
                Simulator.Advance();
                _accumulatedTime -= Simulator.TimeStep;
            }
        }

        private void OnSimulationStopped()
        {
            if (!Simulator.IsInHazard)
            {
                Stopped?.Invoke();
                return;
            }

            // いきなり打ち直しの位置へ飛ぶと何が起きたか分からないので、入った場所を少し見せる
            _isDropPending = true;
            _dropTimer = _dropDelay;
            Penalized?.Invoke(Simulator.Ground);
        }

        private void Drop()
        {
            Place(ToUnity(Simulator.DropPosition));
            Stopped?.Invoke();
        }

        private static Vector2 ToUnity(System.Numerics.Vector2 v) => new Vector2(v.X, v.Y);
        private static System.Numerics.Vector2 ToNumerics(Vector2 v) => new System.Numerics.Vector2(v.x, v.y);
    }
}
