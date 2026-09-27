using System;
using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 棒と全ピンが止まったかどうかの判定（§8.5）。止まらない場合に備えて最大時間でも打ち切る。
    /// </summary>
    public class ThrowSettleWatcher : MonoBehaviour
    {
        [SerializeField] private MolkkyPhysicsSettings _settings;
        [SerializeField] private PinRack _pinRack;
        [SerializeField] private StickThrower _stick;

        private bool _watching;
        private float _elapsed;
        private float _stillTime;

        public event Action Settled;

        public void Begin()
        {
            _watching = true;
            _elapsed = 0f;
            _stillTime = 0f;
        }

        /// <summary>相手の投擲では相手端末の結果が届いた時点で打ち切る（自分の物理の静止は待たない）</summary>
        public void Cancel()
        {
            _watching = false;
        }

        private void FixedUpdate()
        {
            if (!_watching) return;

            _elapsed += Time.fixedDeltaTime;

            float threshold = _settings.SettleSpeedThreshold;
            bool still = _stick.Speed <= threshold && _pinRack.AreAllSlowerThan(threshold);
            _stillTime = still ? _stillTime + Time.fixedDeltaTime : 0f;

            if (_stillTime >= _settings.SettleDuration || _elapsed >= _settings.MaxThrowDuration)
            {
                _watching = false;
                Settled?.Invoke();
            }
        }
    }
}
