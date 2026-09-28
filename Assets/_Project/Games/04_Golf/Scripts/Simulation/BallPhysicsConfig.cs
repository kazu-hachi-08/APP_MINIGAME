using System;

namespace MiniGame.Golf
{
    /// <summary>
    /// BallSimulator の調整値。
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

        /// <summary>
        /// 傾斜の強さ → 転がり中の加速度。グリーンの転がりの減速より小さく保つこと。
        /// 超えると急な傾斜でボールが止まらず転がり続ける
        /// </summary>
        public float SlopeAccelerationScale = 0.4f;

        /// <summary>インパクトのずれ × クラブの曲がりやすさ → 横向きの加速度</summary>
        public float CurveAccelerationScale = 3f;

        /// <summary>風の強さ（m）→ 空中での加速度。滞空時間の長い高い球ほど流される</summary>
        public float WindAccelerationScale = 0.3f;

        /// <summary>インパクトゾーンの外で打ったときの曲がり（ゾーンの端で打ったときの何倍か）</summary>
        public float MissShotCurve = 2.5f;

        /// <summary>インパクトゾーンの外で打ったときに残すパワーの割合</summary>
        public float MissShotPowerRate = 0.7f;

        /// <summary>バックスピンのとき、最初の着地で残す地面方向の速さの割合。小さいほどピタッと止まる</summary>
        public float BackSpinRollRate = 0.3f;

        /// <summary>トップスピンのとき、最初の着地で地面方向の速さに掛ける倍率。1 より大きいほどよく転がる</summary>
        public float TopSpinRollRate = 1.6f;

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
