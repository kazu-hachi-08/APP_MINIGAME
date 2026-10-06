namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// ステージ「なだれの谷」のなだれ（仕様書 §3.4）。一定間隔で戦場の真ん中にいるユニットを敵味方関係なく押し戻し、
    /// 中央での押し合いが続いて試合が動かなくなるのを崩す
    /// </summary>
    public partial class BattleWorld
    {
        /// <summary>
        /// イベントの Amount は int なので、秒・幅を 1/1000 単位で入れる。
        /// 演出がイベントだけで範囲と予告時間を知れるようにして、ステージのデータを持たないあそびかたのデモやゲストでも同じように見せるため
        /// </summary>
        public const float AvalancheAmountScale = 1000f;

        // 前回のなだれ（開始時は試合開始）からの経過秒
        private float _avalancheTimer;
        private bool _avalancheWarned;

        private bool HasAvalanche => _settings.AvalancheInterval > 0f;
        private float AvalancheStartX => _settings.FieldLength * _settings.AvalancheStartRatio;
        private float AvalancheEndX => _settings.FieldLength * _settings.AvalancheEndRatio;
        private float AvalancheCenterX => (AvalancheStartX + AvalancheEndX) * 0.5f;

        private void TickAvalanche(float deltaTime)
        {
            if (!HasAvalanche || IsFinished) return;

            _avalancheTimer += deltaTime;
            if (!_avalancheWarned && _avalancheTimer >= _settings.AvalancheInterval - _settings.AvalancheWarningTime)
            {
                _avalancheWarned = true;
                _events.Add(new BattleEvent(BattleEventType.AvalancheWarning, Side.Left, BattleEvent.CastleId, AvalancheCenterX,
                    (int)(_settings.AvalancheWarningTime * AvalancheAmountScale)));
            }
            if (_avalancheTimer < _settings.AvalancheInterval) return;

            _avalancheTimer -= _settings.AvalancheInterval;
            _avalancheWarned = false;
            TriggerAvalanche();
        }

        /// <summary>城には当てない（ペンギン砲と同じく、城を削るのはユニットの役目にしておくため）</summary>
        private void TriggerAvalanche()
        {
            foreach (UnitState unit in _units)
            {
                if (unit.IsDead || unit.X < AvalancheStartX || unit.X > AvalancheEndX) continue;

                DamageUnit(unit, _settings.AvalancheDamage, false);
                StartKnockback(unit);
            }
            float halfWidth = (AvalancheEndX - AvalancheStartX) * 0.5f;
            _events.Add(new BattleEvent(BattleEventType.Avalanche, Side.Left, BattleEvent.CastleId, AvalancheCenterX,
                (int)(halfWidth * AvalancheAmountScale)));
        }
    }
}
