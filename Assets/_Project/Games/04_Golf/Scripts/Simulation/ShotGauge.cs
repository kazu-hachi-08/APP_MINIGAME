namespace MiniGame.Golf
{
    /// <summary>
    /// 3タップゲージの動きと結果。
    /// マーカーの位置は 0（左端）〜1（右端）。入力やUIから切り離し、タップと経過時間だけで進めるので EditModeテストで検証できる。
    /// </summary>
    public sealed class ShotGauge
    {
        public enum GaugeState
        {
            Idle,
            Rising,
            Returning,
            Finished,
        }

        private readonly float _speed;
        private readonly float _zoneCenter;
        private readonly float _baseZoneHalfWidth;
        private float _zoneHalfWidth;

        /// <param name="speed">1秒あたりにマーカーが動く量（ゲージ全体＝1）</param>
        /// <param name="zoneCenter">インパクトゾーンの中心の位置</param>
        /// <param name="zoneHalfWidth">ライの影響が無いときのインパクトゾーンの半分の幅</param>
        public ShotGauge(float speed, float zoneCenter, float zoneHalfWidth)
        {
            _speed = speed;
            _zoneCenter = zoneCenter;
            _baseZoneHalfWidth = zoneHalfWidth;
            _zoneHalfWidth = zoneHalfWidth;
        }

        public GaugeState State { get; private set; }
        public float Marker { get; private set; }
        public float Power { get; private set; }

        /// <summary>ゾーンの中心からのずれ。±1 がゾーンの端（ShotRequest.ImpactOffset と同じ）</summary>
        public float ImpactOffset { get; private set; }

        /// <summary>1秒あたりにマーカーが動く量。NPC が狙った位置でぴったり止めるのに使う</summary>
        public float Speed => _speed;

        public float ZoneCenter => _zoneCenter;
        public float ZoneHalfWidth => _zoneHalfWidth;

        public bool IsSwinging => State == GaugeState.Rising || State == GaugeState.Returning;

        /// <summary>①タップ：マーカーが右へ動き出す</summary>
        public void Begin()
        {
            State = GaugeState.Rising;
            Marker = 0f;
            Power = 0f;
            ImpactOffset = 0f;
        }

        /// <summary>ライでインパクトゾーンの幅を変える（バンカーは狭い）。構えるたびに呼ぶ</summary>
        public void SetZoneScale(float scale)
        {
            _zoneHalfWidth = _baseZoneHalfWidth * scale;
        }

        public void Reset()
        {
            State = GaugeState.Idle;
            Marker = 0f;
        }

        /// <summary>②タップでパワー、③タップでインパクトを決める</summary>
        public void Tap()
        {
            if (State == GaugeState.Rising)
            {
                Turn(Marker);
            }
            else if (State == GaugeState.Returning)
            {
                Finish();
            }
        }

        public void Tick(float deltaTime)
        {
            if (State == GaugeState.Rising)
            {
                Marker += _speed * deltaTime;
                // 押し遅れても打てるように、右端まで来たらパワー100%として折り返す
                if (Marker >= 1f)
                {
                    Marker = 1f;
                    Turn(1f);
                }
            }
            else if (State == GaugeState.Returning)
            {
                Marker -= _speed * deltaTime;
                // タップしなかったら、ゾーンを通り過ぎた最後の位置（左端）で打ったことにする
                if (Marker <= 0f)
                {
                    Marker = 0f;
                    Finish();
                }
            }
        }

        private void Turn(float power)
        {
            Power = power;
            State = GaugeState.Returning;
        }

        private void Finish()
        {
            ImpactOffset = (Marker - _zoneCenter) / _zoneHalfWidth;
            State = GaugeState.Finished;
        }
    }
}
