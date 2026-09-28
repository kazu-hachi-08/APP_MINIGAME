using System;
using System.Numerics;

namespace MiniGame.Golf
{
    /// <summary>
    /// NPC の狙いを決める。カップを狙ってパワーを試し打ちで選び、池・OBを避け、風・傾斜を読んで補正し、最後にブレを加える。
    /// MonoBehaviour にしないのは EditModeテストで検証するため。
    /// </summary>
    public sealed class NpcShotPlanner
    {
        private const float MaxPower = 1f;

        // パワーは粗く試してから、一番良かった値の前後を細かく試す。パットは距離がパワーの2乗で効くので細かさが要る
        private const int CoarsePowerSteps = 20;
        private const int FinePowerSteps = 10;

        // 風・傾斜で流された分だけ狙いをずらす、を繰り返す回数
        private const int CorrectionIterations = 3;

        // 池・OBを避けるときに試す向きのずらし方（度）。近い方から試す
        private static readonly float[] AvoidAngles = { 8f, -8f, 16f, -16f, 24f, -24f };

        // 避けられなかったときに手前に刻むパワーの刻み
        private const float LayUpPowerStep = 0.1f;

        private readonly BallSimulator _ball;
        private readonly NpcDifficultyConfig _difficulty;
        private readonly Random _random;

        /// <param name="ball">今打つボール（位置・地面・傾斜・カップ・風が入ったもの）。試し打ちは別の計算機でするので状態は変わらない</param>
        public NpcShotPlanner(BallSimulator ball, NpcDifficultyConfig difficulty, Random random)
        {
            _ball = ball ?? throw new ArgumentNullException(nameof(ball));
            _difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>ブレまで入れた、実際に打つショット。club はクラブ選択で選ばれたもの（ClubSelector の初期値）を渡す</summary>
        public ShotRequest Plan(ClubConfig club, Vector2 cup)
        {
            return AddError(Aim(club, cup));
        }

        /// <summary>ブレを入れる前の狙い</summary>
        public ShotRequest Aim(ClubConfig club, Vector2 cup)
        {
            // カップを狙う。届かなければパワーの探索で自然に飛距離いっぱいになる
            ShotRequest shot = AimAt(club, cup);
            if (IsHazard(shot)) shot = AvoidHazard(shot);

            return CorrectForEnvironment(club, shot);
        }

        /// <summary>風・曲がり・傾斜なしで、point の一番近くに止まるパワーを選ぶ</summary>
        private ShotRequest AimAt(ClubConfig club, Vector2 point)
        {
            Vector2 toPoint = point - _ball.Position;
            Vector2 direction = toPoint.LengthSquared() > 0f ? Vector2.Normalize(toPoint) : Vector2.UnitY;

            float coarseStep = MaxPower / CoarsePowerSteps;
            float coarsePower = SearchPower(club, direction, point, coarseStep, MaxPower, CoarsePowerSteps);

            // 細かく試す範囲は粗い探索の前後1刻み。パワー0では打てないので下限は細かい刻み1つ分にする
            float fineFrom = Math.Max(coarsePower - coarseStep, coarseStep / FinePowerSteps);
            float fineTo = Math.Min(coarsePower + coarseStep, MaxPower);
            float power = SearchPower(club, direction, point, fineFrom, fineTo, FinePowerSteps);

            return new ShotRequest(direction, club, power, 0f);
        }

        private float SearchPower(ClubConfig club, Vector2 direction, Vector2 point, float from, float to, int steps)
        {
            float bestPower = to;
            float bestDistance = float.MaxValue;
            for (int i = 0; i <= steps; i++)
            {
                float power = from + (to - from) * i / steps;
                ShotOutcome outcome = SimulateStraight(new ShotRequest(direction, club, power, 0f));
                float distance = Vector2.DistanceSquared(outcome.Position, point);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPower = power;
                }
            }

            return bestPower;
        }

        /// <summary>
        /// 池・OBを避ける設定の難易度だけが左右にずらして避ける。どの難易度でも最後は手前に刻む。
        /// 刻みを全員に入れるのは、同じ打ち直しの位置から毎回池へ打ち込み続けると試合が進まず見ていてつらいため
        /// </summary>
        private ShotRequest AvoidHazard(ShotRequest shot)
        {
            if (_difficulty.AvoidHazards && TrySidestep(shot, out ShotRequest shifted)) return shifted;
            if (TryLayUp(shot, out ShotRequest layUp)) return layUp;

            return shot;
        }

        private bool TrySidestep(ShotRequest shot, out ShotRequest shifted)
        {
            foreach (float degrees in AvoidAngles)
            {
                shifted = new ShotRequest(Rotate(shot.Direction, degrees), shot.Club, shot.Power, 0f);
                if (!IsHazard(shifted)) return true;
            }

            shifted = shot;
            return false;
        }

        private bool TryLayUp(ShotRequest shot, out ShotRequest layUp)
        {
            for (float power = shot.Power - LayUpPowerStep; power > 0f; power -= LayUpPowerStep)
            {
                layUp = new ShotRequest(shot.Direction, shot.Club, power, 0f);
                if (!IsHazard(layUp)) return true;
            }

            layUp = shot;
            return false;
        }

        private bool IsHazard(ShotRequest shot)
        {
            return SimulateStraight(shot).IsInHazard;
        }

        /// <summary>風・傾斜を読まない試し打ち。狙いの基準は環境の影響を除いた「まっすぐ打った結果」にする</summary>
        private ShotOutcome SimulateStraight(ShotRequest shot)
        {
            return _ball.TrySimulate(shot, false, false);
        }

        /// <summary>風・傾斜ありで試し打ちし、止まりたい地点からずれた分だけ狙いを逆にずらす</summary>
        private ShotRequest CorrectForEnvironment(ClubConfig club, ShotRequest shot)
        {
            // 風は空中にしか効かず、傾斜は転がりにしか効かないので、クラブで読むものを分ける
            bool withWind = _difficulty.ReadWind && !club.IsPutter;
            bool withSlope = _difficulty.ReadSlope && club.IsPutter;
            if (!withWind && !withSlope) return shot;

            Vector2 desired = SimulateStraight(shot).Position;
            Vector2 aimPoint = desired;
            ShotRequest corrected = shot;

            for (int i = 0; i < CorrectionIterations; i++)
            {
                ShotOutcome actual = _ball.TrySimulate(corrected, withWind, withSlope);
                if (actual.IsInCup) break;

                aimPoint += desired - actual.Position;
                corrected = AimAt(club, aimPoint);
            }

            return corrected;
        }

        private ShotRequest AddError(ShotRequest shot)
        {
            Vector2 direction = Rotate(shot.Direction, RandomRange(_difficulty.DirectionErrorDegrees));
            float power = Math.Clamp(shot.Power * (1f + RandomRange(_difficulty.PowerErrorRate)), 0f, MaxPower);
            return new ShotRequest(direction, shot.Club, power, RandomRange(_difficulty.ImpactError));
        }

        /// <summary>-max〜max の一様乱数</summary>
        private float RandomRange(float max)
        {
            return (float)(_random.NextDouble() * 2.0 - 1.0) * max;
        }

        /// <summary>反時計回り（左）が正</summary>
        private static Vector2 Rotate(Vector2 direction, float degrees)
        {
            return Vector2.Transform(direction, Matrix3x2.CreateRotation(degrees * MathF.PI / 180f));
        }
    }
}
