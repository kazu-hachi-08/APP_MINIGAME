using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// NPCの投擲（§9）。狙うピンを決め、そこへ届く ThrowRequest を作り、難易度に応じたブレを加える。
    /// 人間と同じ ThrowRequest を返すので、投げる処理（StickThrower）は共通のまま使える。
    /// </summary>
    public class NpcThrower : MonoBehaviour
    {
        [SerializeField] private MolkkyPhysicsSettings _settings;
        [SerializeField] private PinRack _pinRack;

        [Tooltip("よわい・ふつう・つよい の順（PlayerKind の並びと合わせる）")]
        [SerializeField] private MolkkyNpcDifficulty[] _difficulties;

        [Tooltip("この距離以内に他の立っているピンがあれば「混んでいる」とみなす（地面単位）")]
        [SerializeField] private float _crowdRadius = 0.6f;

        [Tooltip("1本を狙うとき、ピンをどれだけ越える強さで投げるか。小さいほど当たった後に他を巻き込みにくい")]
        [SerializeField] private float _singleOvershoot = 0.4f;

        [Tooltip("密集地を狙うとき、ピンをどれだけ越える強さで投げるか。強めに投げてまとめて倒す")]
        [SerializeField] private float _denseOvershoot = 1.5f;

        public ThrowRequest CreateRequest(PlayerSlot player)
        {
            MolkkyNpcDifficulty difficulty = GetDifficulty(player.Kind);
            Pin target = ChooseTarget(player.Remaining, difficulty, out bool single);
            // 1本狙いは当たり幅の細い縦投げ、密集地はまとめて倒せる横投げにする
            return single
                ? Aim(target.GroundPosition, _singleOvershoot, ThrowStyle.Vertical, difficulty)
                : Aim(target.GroundPosition, _denseOvershoot, ThrowStyle.Horizontal, difficulty);
        }

        private MolkkyNpcDifficulty GetDifficulty(PlayerKind kind)
        {
            // PlayerKind は Human の次から よわい・ふつう・つよい と並ぶ
            int index = Mathf.Clamp((int)kind - 1, 0, _difficulties.Length - 1);
            return _difficulties[index];
        }

        /// <summary>§9.2 の狙い決め。single は「1本だけ倒したい狙い」かどうか</summary>
        private Pin ChooseTarget(int remaining, MolkkyNpcDifficulty difficulty, out bool single)
        {
            single = true;

            if (difficulty.AimExactPin)
            {
                Pin exact = FindStanding(remaining);
                if (exact != null && !(difficulty.AvoidOverflow && IsCrowded(exact))) return exact;

                if (difficulty.AvoidOverflow)
                {
                    Pin safe = FindSafePin(remaining);
                    if (safe != null) return safe;
                }

                if (exact != null) return exact;
            }

            single = false;
            return FindDensest();
        }

        private Pin FindStanding(int number)
        {
            foreach (Pin pin in _pinRack.Pins)
            {
                if (!pin.IsFallen && pin.Number == number) return pin;
            }

            return null;
        }

        /// <summary>残り点数より小さい数字で、周りに他のピンがないもの。25点に戻らず確実に近づける中で一番大きい数字を選ぶ</summary>
        private Pin FindSafePin(int remaining)
        {
            Pin best = null;
            foreach (Pin pin in _pinRack.Pins)
            {
                if (pin.IsFallen || pin.Number >= remaining || IsCrowded(pin)) continue;
                if (best == null || pin.Number > best.Number) best = pin;
            }

            return best;
        }

        /// <summary>周りに立っているピンが最も多いピン。同数なら手前（届きやすい）を選ぶ</summary>
        private Pin FindDensest()
        {
            Pin best = null;
            int bestCount = -1;
            foreach (Pin pin in _pinRack.Pins)
            {
                if (pin.IsFallen) continue;

                int count = CountNeighbors(pin);
                bool better = count > bestCount
                    || (count == bestCount && pin.GroundPosition.y < best.GroundPosition.y);
                if (!better) continue;

                best = pin;
                bestCount = count;
            }

            return best;
        }

        private bool IsCrowded(Pin pin)
        {
            return CountNeighbors(pin) > 0;
        }

        private int CountNeighbors(Pin pin)
        {
            float sqrRadius = _crowdRadius * _crowdRadius;
            int count = 0;
            foreach (Pin other in _pinRack.Pins)
            {
                if (other == pin || other.IsFallen) continue;
                if ((other.GroundPosition - pin.GroundPosition).sqrMagnitude <= sqrRadius) count++;
            }

            return count;
        }

        /// <summary>§9.3：狙うピンに近い位置から、ピンの少し先まで届く強さで投げる</summary>
        private ThrowRequest Aim(Vector2 target, float overshoot, ThrowStyle style, MolkkyNpcDifficulty difficulty)
        {
            float x = Mathf.Clamp(target.x, -_settings.ThrowLineHalfWidth, _settings.ThrowLineHalfWidth);
            Vector2 toTarget = target - new Vector2(x, 0f);

            float angle = Mathf.Atan2(toTarget.x, toTarget.y) * Mathf.Rad2Deg;
            // linearDamping で減速する物体は、初速 ÷ 減速率 のあたりで止まる。そこから逆算して必要な初速を出す
            float speed = _settings.StickDamping * (toTarget.magnitude + overshoot);

            angle += Random.Range(-difficulty.AngleNoise, difficulty.AngleNoise);
            speed *= 1f + Random.Range(-difficulty.SpeedNoise, difficulty.SpeedNoise);

            angle = Mathf.Clamp(angle, -_settings.MaxThrowAngle, _settings.MaxThrowAngle);
            speed = Mathf.Clamp(speed, _settings.MinThrowSpeed, _settings.MaxThrowSpeed);
            return new ThrowRequest(x, angle, speed, style);
        }
    }
}
