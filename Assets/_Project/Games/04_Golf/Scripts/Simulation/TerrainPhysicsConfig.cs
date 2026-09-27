using System;

namespace MiniGame.Golf
{
    /// <summary>1種類の地面での転がり・跳ね返り（§8.4）</summary>
    [Serializable]
    public class TerrainPhysics
    {
        /// <summary>転がり中の減速（ユニット/秒²）。大きいほどすぐ止まる</summary>
        public float RollDeceleration;

        /// <summary>着地したときに上向きに跳ね返る速さの割合</summary>
        public float BounceRestitution;

        /// <summary>着地したときに残す水平方向の速さの割合</summary>
        public float BounceSpeedRetention;

        public TerrainPhysics()
        {
        }

        public TerrainPhysics(float rollDeceleration, float bounceRestitution, float bounceSpeedRetention)
        {
            RollDeceleration = rollDeceleration;
            BounceRestitution = bounceRestitution;
            BounceSpeedRetention = bounceSpeedRetention;
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
        public TerrainPhysics Rough = new TerrainPhysics(9f, 0.2f, 0.5f);

        // 跳ねずに砂に埋まる感じを出すため、跳ね返りなし・水平の速さもほとんど残さない
        public TerrainPhysics Bunker = new TerrainPhysics(25f, 0f, 0.2f);

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
                // 池・OB でボールを止める罰打の処理は Phase 4。それまではラフと同じに転がす
                default: return Rough;
            }
        }
    }
}
