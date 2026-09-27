using System;

namespace MiniGame.Golf
{
    /// <summary>1種類の地面での転がり・跳ね返りと、そこから打つときの影響（§8.4）</summary>
    [Serializable]
    public class TerrainPhysics
    {
        /// <summary>転がり中の減速（ユニット/秒²）。大きいほどすぐ止まる</summary>
        public float RollDeceleration;

        /// <summary>着地したときに上向きに跳ね返る速さの割合</summary>
        public float BounceRestitution;

        /// <summary>着地したときに残す水平方向の速さの割合</summary>
        public float BounceSpeedRetention;

        // 後から足した値。既存のアセットに値が無くても影響なしになるよう、初期値は 1 にしておく

        /// <summary>このライから打つときの飛距離の割合</summary>
        public float ShotDistanceRate = 1f;

        /// <summary>このライから打つときのインパクトゾーンの幅の割合。小さいほどまっすぐ打つのが難しい</summary>
        public float ImpactZoneRate = 1f;

        public TerrainPhysics()
        {
        }

        public TerrainPhysics(float rollDeceleration, float bounceRestitution, float bounceSpeedRetention,
            float shotDistanceRate = 1f, float impactZoneRate = 1f)
        {
            RollDeceleration = rollDeceleration;
            BounceRestitution = bounceRestitution;
            BounceSpeedRetention = bounceSpeedRetention;
            ShotDistanceRate = shotDistanceRate;
            ImpactZoneRate = impactZoneRate;
        }
    }

    /// <summary>
    /// 地面の種類ごとの転がり・跳ね返り（§8.4）。
    /// BallPhysicsConfig と同じく UnityEngine に依存させず、ScriptableObject（GolfTerrainSettings）の中に表示する。
    /// </summary>
    [Serializable]
    public class TerrainPhysicsConfig
    {
        public TerrainPhysics Tee = new TerrainPhysics(4f, 0.35f, 0.7f);
        public TerrainPhysics Fairway = new TerrainPhysics(4f, 0.35f, 0.7f);
        public TerrainPhysics Rough = new TerrainPhysics(9f, 0.2f, 0.5f, 0.8f);

        // 跳ねずに砂に埋まる感じを出すため、跳ね返りなし・水平の速さもほとんど残さない。
        // 出すときは飛ばず、ゾーンも狭くして「入れたくない場所」にする
        public TerrainPhysics Bunker = new TerrainPhysics(25f, 0f, 0.2f, 0.6f, 0.5f);

        // パットが読めるように、よく転がる
        public TerrainPhysics Green = new TerrainPhysics(2.5f, 0.2f, 0.6f);

        public TerrainPhysics Get(GroundType ground)
        {
            switch (ground)
            {
                case GroundType.Tee: return Tee;
                case GroundType.Fairway: return Fairway;
                case GroundType.Bunker: return Bunker;
                case GroundType.Green: return Green;
                // 池・OB は入った瞬間にボールを止めるので、転がり・打つときの値は使わない
                default: return Rough;
            }
        }
    }
}
