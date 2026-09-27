using NUnit.Framework;

namespace MiniGame.Golf.Tests
{
    public class ShotGaugeTests
    {
        private const float Speed = 1f;
        private const float ZoneCenter = 0.1f;
        private const float ZoneHalfWidth = 0.05f;

        private static ShotGauge CreateStarted()
        {
            var gauge = new ShotGauge(Speed, ZoneCenter, ZoneHalfWidth);
            gauge.Begin();
            return gauge;
        }

        [Test]
        public void 三回目のタップで打てる()
        {
            ShotGauge gauge = CreateStarted();

            gauge.Tick(0.6f);
            gauge.Tap();
            Assert.AreEqual(ShotGauge.GaugeState.Returning, gauge.State);

            gauge.Tick(0.5f);
            gauge.Tap();

            Assert.AreEqual(ShotGauge.GaugeState.Finished, gauge.State);
            Assert.AreEqual(0.6f, gauge.Power, 0.0001f);
            Assert.AreEqual(0f, gauge.ImpactOffset, 0.0001f);
        }

        [Test]
        public void インパクトのずれはゾーンの幅を1とした値になる()
        {
            ShotGauge gauge = CreateStarted();
            gauge.Tick(0.5f);
            gauge.Tap();

            // ゾーンの中心より半分の幅だけ早く（右で）押す
            gauge.Tick(0.5f - ZoneCenter - ZoneHalfWidth * 0.5f);
            gauge.Tap();

            Assert.AreEqual(0.5f, gauge.ImpactOffset, 0.0001f);
        }

        [Test]
        public void パワーを押さなければ右端で100パーセントとして折り返す()
        {
            ShotGauge gauge = CreateStarted();

            gauge.Tick(1.2f);

            Assert.AreEqual(ShotGauge.GaugeState.Returning, gauge.State);
            Assert.AreEqual(1f, gauge.Power);
        }

        [Test]
        public void インパクトを押さなければ左端で打ったことになりミスショットになる()
        {
            ShotGauge gauge = CreateStarted();
            gauge.Tick(0.5f);
            gauge.Tap();

            gauge.Tick(1f);

            Assert.AreEqual(ShotGauge.GaugeState.Finished, gauge.State);
            Assert.AreEqual(0f, gauge.Marker);
            Assert.Less(gauge.ImpactOffset, -1f);
        }

        [Test]
        public void 開始前のタップは無視する()
        {
            var gauge = new ShotGauge(Speed, ZoneCenter, ZoneHalfWidth);

            gauge.Tap();

            Assert.AreEqual(ShotGauge.GaugeState.Idle, gauge.State);
        }
    }
}
