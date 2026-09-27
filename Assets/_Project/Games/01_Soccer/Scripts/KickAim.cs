using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// 操作選手のキック方向を補正する。
    /// スティック方向（＝向いている方向）にしか蹴れないと、走りながら横や後ろの味方へ出せないため、
    /// 入力はそのままに「狙いたいであろう相手」へ方向を寄せる（入力方式を変えないのでオンライン同期にも影響しない）。
    /// </summary>
    public static class KickAim
    {
        /// <summary>
        /// aimDirection から maxAngle 以内・maxRange 以内にいる味方のうち、
        /// 角度のずれが小さく近い選手の方向を返す。該当者がいなければ aimDirection のまま。
        /// </summary>
        public static Vector2 PassDirection(Vector2 from, Vector2 aimDirection, TeamMember self, float maxAngle, float maxRange)
        {
            if (self == null) return aimDirection;

            Vector2 best = aimDirection;
            float bestScore = float.MaxValue;

            var allMembers = Object.FindObjectsByType<TeamMember>(FindObjectsSortMode.None);
            foreach (var member in allMembers)
            {
                if (member == self || member.Team != self.Team) continue;

                Vector2 toMate = (Vector2)member.transform.position - from;
                float distance = toMate.magnitude;
                if (distance < Mathf.Epsilon || distance > maxRange) continue;

                float angle = Vector2.Angle(aimDirection, toMate);
                if (angle > maxAngle) continue;

                // 距離だけで選ぶとスティックと違う方向の近い味方に吸われるので、角度のずれも罰則にする
                float score = distance * (1f + angle / maxAngle);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = toMate / distance;
                }
            }

            return best;
        }

        /// <summary>
        /// 相手ゴールが shootRange 以内なら、ゴール枠内へ向けた方向を返す。
        /// スティックの上下成分で左右どちらの隅を狙うかを選べるようにする。圏外なら aimDirection のまま。
        /// </summary>
        public static Vector2 ShotDirection(Vector2 from, Vector2 aimDirection, float goalX, float cornerOffset, float shootRange)
        {
            Vector2 goalCenter = new Vector2(goalX, 0f);
            if (Vector2.Distance(from, goalCenter) > shootRange) return aimDirection;

            Vector2 target = goalCenter + new Vector2(0f, Mathf.Clamp(aimDirection.y, -1f, 1f) * cornerOffset);
            return (target - from).normalized;
        }

        /// <summary>
        /// 近くのボールが aimDirection から maxAngle 以内にあれば、その方向へ向きを寄せる（スライディングの空振り防止）
        /// </summary>
        public static Vector2 TowardBall(Vector2 from, Vector2 aimDirection, Vector2 ballPosition, float maxAngle, float maxRange)
        {
            Vector2 toBall = ballPosition - from;
            if (toBall.sqrMagnitude < Mathf.Epsilon || toBall.magnitude > maxRange) return aimDirection;
            if (Vector2.Angle(aimDirection, toBall) > maxAngle) return aimDirection;

            return toBall.normalized;
        }
    }
}
