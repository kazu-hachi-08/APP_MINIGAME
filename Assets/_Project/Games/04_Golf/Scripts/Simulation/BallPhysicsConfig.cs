using System;

namespace MiniGame.Golf
{
    /// <summary>
    /// BallSimulator の調整値（§8.7）。
    /// UnityEngine に依存させず EditModeテストや dotnet で検証できるよう、ScriptableObject（GolfPhysicsSettings）とは分けて
    /// public フィールドの Serializable クラスにしている。インスペクタでは GolfPhysicsSettings の中に表示される。
    /// 距離は1タイル＝1ユニット、時間は秒。
    /// </summary>
    [Serializable]
    public class BallPhysicsConfig
    {
        /// <summary>計算の時間刻み。固定にすることで、同じ入力から毎回同じ結果になる</summary>
        public float SimulationStep = 0.02f;

        /// <summary>高さ方向の重力。小さいほど滞空時間が長く、ふわっと飛ぶ</summary>
        public float Gravity = 6f;

        /// <summary>パワー100%のときの初速</summary>
        public float MaxLaunchSpeed = 12f;

        /// <summary>打ち出し角（度）。Phase 3 でクラブごとの値に置き換える</summary>
        public float LaunchAngleDegrees = 25f;

        /// <summary>仮のパットの最大初速。Phase 3 でパタークラブの値に置き換える</summary>
        public float PuttMaxSpeed = 5f;

        /// <summary>跳ね返りがこれより遅ければバウンドをやめて転がりに移る。小さな跳ねが延々と続くのを防ぐ</summary>
        public float MinBounceSpeed = 0.8f;

        /// <summary>カップの中心からこの距離以内ならカップの上とみなす</summary>
        public float CupRadius = 0.25f;

        /// <summary>転がりがこの速さより速いとカップを通り過ぎる</summary>
        public float CupInMaxSpeed = 2.5f;

        /// <summary>転がりがこの速さ以下になったら停止とみなす</summary>
        public float StopSpeed = 0.05f;

        /// <summary>1打の最大時間。止まらない設定にしてしまっても手番が進むようにする保険</summary>
        public float MaxSimulationTime = 10f;
    }
}
