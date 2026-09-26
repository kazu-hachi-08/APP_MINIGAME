using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 12本のピンの生成・初期配置・立て直し・倒れたピンの集計（§6.2 / §6.8）。
    /// ピンは Prefab にせずコードで作る。Prefab/Scene の競合を減らし、本数や配置をここだけで管理するため。
    /// </summary>
    public class PinRack : MonoBehaviour
    {
        // 手前の列から順に並べる（§6.2 の図と同じ並び）
        private static readonly int[][] InitialRows =
        {
            new[] { 1, 2 },
            new[] { 3, 10, 4 },
            new[] { 5, 11, 12, 6 },
            new[] { 7, 9, 8 },
        };

        // 六角形に詰めたときの列の間隔（ピン間隔に対する比率）
        private const float HexRowRatio = 0.866f;

        // 立て直しで重なったピンを引き離す反復回数。数本の重なりならこの回数で十分に解ける
        private const int SeparationIterations = 6;

        [SerializeField] private MolkkyPhysicsSettings _settings;

        private readonly List<Pin> _pins = new List<Pin>();

        public IReadOnlyList<Pin> Pins => _pins;

        private void Awake()
        {
            CreatePins();
            ResetToInitial();
        }

        private void CreatePins()
        {
            var material = new PhysicsMaterial2D("Pin") { bounciness = _settings.Bounciness, friction = 0f };

            foreach (int[] row in InitialRows)
            {
                foreach (int number in row)
                {
                    var obj = new GameObject($"Pin_{number}", typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(Pin));
                    obj.transform.SetParent(transform, false);

                    var pin = obj.GetComponent<Pin>();
                    pin.Initialize(number, _settings, material);
                    _pins.Add(pin);
                }
            }
        }

        /// <summary>全ピンを初期配置に戻す</summary>
        public void ResetToInitial()
        {
            int pinIndex = 0;
            for (int row = 0; row < InitialRows.Length; row++)
            {
                int count = InitialRows[row].Length;
                float z = _settings.PinSetDistance + row * _settings.PinSpacing * HexRowRatio;

                for (int i = 0; i < count; i++)
                {
                    float x = (i - (count - 1) * 0.5f) * _settings.PinSpacing;
                    _pins[pinIndex].StandAt(new Vector2(x, z));
                    pinIndex++;
                }
            }
        }

        public void ArmAll()
        {
            foreach (Pin pin in _pins) pin.Arm();
        }

        public void DisarmAll()
        {
            foreach (Pin pin in _pins) pin.Disarm();
        }

        public List<int> CollectFallenNumbers()
        {
            var numbers = new List<int>();
            foreach (Pin pin in _pins)
            {
                if (pin.IsFallen) numbers.Add(pin.Number);
            }

            return numbers;
        }

        public bool AreAllSlowerThan(float speed)
        {
            foreach (Pin pin in _pins)
            {
                if (pin.Speed > speed) return false;
            }

            return true;
        }

        /// <summary>倒れたピンを倒れた場所で立て直す（フィールド外はフィールド端に寄せる）</summary>
        public void StandUpFallen()
        {
            Rect bounds = _settings.FieldBounds;

            foreach (Pin pin in _pins)
            {
                pin.Stop();
                if (!pin.IsFallen) continue;

                Vector2 p = pin.GroundPosition;
                pin.StandAt(new Vector2(
                    Mathf.Clamp(p.x, bounds.xMin, bounds.xMax),
                    Mathf.Clamp(p.y, bounds.yMin, bounds.yMax)));
            }

            SeparateOverlaps();
        }

        /// <summary>
        /// 立て直したピン同士が重なっていると、次の投擲開始時に物理の押し出しで勝手に動いてしまう。
        /// そうならないよう、立て直しの時点で重ならない位置まで引き離しておく。
        /// </summary>
        private void SeparateOverlaps()
        {
            float minDistance = _settings.PinRadius * 2f;

            for (int iteration = 0; iteration < SeparationIterations; iteration++)
            {
                for (int a = 0; a < _pins.Count; a++)
                {
                    for (int b = a + 1; b < _pins.Count; b++)
                    {
                        Vector2 delta = _pins[b].GroundPosition - _pins[a].GroundPosition;
                        float distance = delta.magnitude;
                        if (distance >= minDistance) continue;

                        Vector2 dir = distance > 0f ? delta / distance : Vector2.right;
                        Vector2 push = dir * (minDistance - distance) * 0.5f;
                        _pins[a].StandAt(_pins[a].GroundPosition - push);
                        _pins[b].StandAt(_pins[b].GroundPosition + push);
                    }
                }
            }
        }
    }
}
