using System;
using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// BattleEvent を演出と音に振り分ける唯一の入口（仕様書 §9）。
    /// オンライン（Phase 10）ではゲストも届いたイベントを Present に流すので、BattleWorld の中身には頼らず、イベントの値だけで演出する
    /// </summary>
    public class BattleEventPresenter : MonoBehaviour
    {
        [SerializeField] private BattleRunner _battleRunner;
        [SerializeField] private BattleCamera _battleCamera;
        [SerializeField] private BattleHud _hud;
        [SerializeField] private PenguinWarsAudio _audio;
        [SerializeField] private CastleCollapse _leftCollapse;
        [SerializeField] private CastleCollapse _rightCollapse;

        [Header("演出の見本（非アクティブ。複製して使い回す）")]
        [SerializeField] private SpawnSmokeEffect _smokeTemplate;
        [SerializeField] private HitSparkEffect _sparkTemplate;
        [SerializeField] private SoulRiseEffect _soulTemplate;
        [SerializeField] private CannonBeamEffect _beamTemplate;

        [Header("同時に出せる数。超えた分は出さない（大量の敵でもカクつかないように）")]
        [SerializeField] private int _maxSmokes = 16;
        [SerializeField] private int _maxSparks = 24;
        [SerializeField] private int _maxSouls = 16;
        [SerializeField] private int _maxBeams = 2;

        [Header("出す位置")]
        [Tooltip("ユニットのヒット位置の高さ（足元から）")]
        [SerializeField] private float _unitHitHeight = 0.6f;
        [Tooltip("城のヒット位置の高さの範囲")]
        [SerializeField] private Vector2 _castleHitHeight = new Vector2(1f, 3f);
        [Tooltip("ヒット位置のばらつき。同じ所にばかり出て1つに見えないように")]
        [SerializeField] private float _hitJitter = 0.25f;
        [Tooltip("やられた側の体の前面（攻撃してきた側）に寄せる量")]
        [SerializeField] private float _hitFrontOffset = 0.3f;
        [SerializeField] private float _smokeHeight = 0.5f;
        [SerializeField] private float _soulHeight = 0.6f;
        [Tooltip("ビームは城の絵の中からではなく、城の前から出す")]
        [SerializeField] private float _beamStartOffset = 1f;
        [SerializeField] private float _beamHeight = 1f;

        [Header("画面揺れ")]
        [SerializeField] private float _cannonShakeDuration = 0.4f;
        [SerializeField] private float _shakeStrength = 0.25f;

        [Header("なだれ")]
        [SerializeField] private string _avalancheWarningText = "なだれ注意！";
        [Tooltip("範囲に並べる煙の数。煙の同時数（_maxSmokes）より少なくして、出撃の煙が出なくならないようにする")]
        [SerializeField] private int _avalancheSmokeCount = 8;
        [SerializeField] private float _avalancheShakeDuration = 0.8f;

        [Header("城が崩れるときの煙")]
        [SerializeField] private int _collapseSmokeCount = 6;
        [SerializeField] private Vector2 _collapseSmokeArea = new Vector2(3f, 3f);

        // 撃破の数字・出撃音は自分の分だけ。オンラインのゲストも左右反転したイベントを受け取るので、自分は常に Left
        private readonly Side _localSide = Side.Left;

        private EffectPool<SpawnSmokeEffect> _smokes;
        private EffectPool<HitSparkEffect> _sparks;
        private EffectPool<SoulRiseEffect> _souls;
        private EffectPool<CannonBeamEffect> _beams;

        /// <summary>城の崩れる演出が終わった（Side は崩れた側）。リザルトはこの後に出す</summary>
        public event Action<Side> CastleCollapsed;

        private void Awake()
        {
            _smokes = new EffectPool<SpawnSmokeEffect>(_smokeTemplate, transform, _maxSmokes);
            _sparks = new EffectPool<HitSparkEffect>(_sparkTemplate, transform, _maxSparks);
            _souls = new EffectPool<SoulRiseEffect>(_soulTemplate, transform, _maxSouls);
            _beams = new EffectPool<CannonBeamEffect>(_beamTemplate, transform, _maxBeams);
        }

        private void OnEnable()
        {
            _battleRunner.EventRaised += Present;
        }

        private void OnDisable()
        {
            _battleRunner.EventRaised -= Present;
        }

        public void Present(BattleEvent battleEvent)
        {
            switch (battleEvent.Type)
            {
                case BattleEventType.Spawned:
                    PresentSpawn(battleEvent);
                    break;
                case BattleEventType.Hit:
                    PresentHit(battleEvent);
                    break;
                case BattleEventType.Died:
                    PresentDeath(battleEvent);
                    break;
                case BattleEventType.CannonFired:
                    PresentCannon(battleEvent);
                    break;
                case BattleEventType.BossAppeared:
                    _hud.ShowBossAppeared();
                    _audio.PlayLevelUp();
                    break;
                case BattleEventType.CastleDestroyed:
                    PresentCastleDestroyed(battleEvent.Side, battleEvent.X);
                    break;
                case BattleEventType.AvalancheWarning:
                    _hud.ShowNotice(_avalancheWarningText, battleEvent.Amount / BattleWorld.AvalancheAmountScale);
                    break;
                case BattleEventType.Avalanche:
                    PresentAvalanche(battleEvent);
                    break;
            }
        }

        private void PresentSpawn(BattleEvent battleEvent)
        {
            _smokes.Get()?.Play(new Vector3(battleEvent.X, _smokeHeight, 0f));
            // 敵の湧きまで鳴らすと、自分の操作の手応えが埋もれるので自分の出撃だけ
            if (battleEvent.Side == _localSide) _audio.PlaySpawn();
        }

        private void PresentHit(BattleEvent battleEvent)
        {
            float height = battleEvent.UnitId == BattleEvent.CastleId
                ? UnityEngine.Random.Range(_castleHitHeight.x, _castleHitHeight.y)
                : _unitHitHeight;
            float x = battleEvent.X + battleEvent.Side.Forward() * _hitFrontOffset;
            Vector3 jitter = UnityEngine.Random.insideUnitCircle * _hitJitter;
            _sparks.Get()?.Play(new Vector3(x, height, 0f) + jitter);
            _audio.PlayHit();
        }

        private void PresentDeath(BattleEvent battleEvent)
        {
            // Side はやられた側。相手がやられた＝自分が倒した
            int reward = battleEvent.Side != _localSide ? battleEvent.Amount : 0;
            _souls.Get()?.Play(new Vector3(battleEvent.X, _soulHeight, 0f), reward);
            _audio.PlayDeath();
        }

        /// <summary>X はビームの届いた先端。根元は撃った側の城（左は 0、右は戦場の長さ）</summary>
        private void PresentCannon(BattleEvent battleEvent)
        {
            int direction = battleEvent.Side.Forward();
            float castleX = battleEvent.Side == Side.Left ? 0f : _battleRunner.FieldLength;
            float startX = castleX + direction * _beamStartOffset;
            float length = Mathf.Max(0f, (battleEvent.X - startX) * direction);
            _beams.Get()?.Play(new Vector3(startX, _beamHeight, 0f), length, direction);
            _battleCamera.Shake(_cannonShakeDuration, _shakeStrength);
            _audio.PlayCannon();
        }

        /// <summary>
        /// 範囲いっぱいに煙を並べて「どこまで巻き込まれたか」を見せる。X は範囲の中央なので、ゲストの反転後もそのまま使える。
        /// 専用の絵は作らず、出撃の煙を流用する
        /// </summary>
        private void PresentAvalanche(BattleEvent battleEvent)
        {
            _hud.HideNotice();
            _battleCamera.Shake(_avalancheShakeDuration, _shakeStrength);
            _audio.PlayCollapse();

            float halfWidth = battleEvent.Amount / BattleWorld.AvalancheAmountScale;
            float startX = battleEvent.X - halfWidth;
            float endX = battleEvent.X + halfWidth;
            for (int i = 0; i < _avalancheSmokeCount; i++)
            {
                float rate = _avalancheSmokeCount > 1 ? (float)i / (_avalancheSmokeCount - 1) : 0.5f;
                _smokes.Get()?.Play(new Vector3(Mathf.Lerp(startX, endX, rate), _smokeHeight, 0f));
            }
        }

        private void PresentCastleDestroyed(Side side, float castleX)
        {
            CastleCollapse collapse = side == Side.Left ? _leftCollapse : _rightCollapse;
            // スクロールして別の場所を見ていても、崩れるところを見せる
            _battleCamera.LookAt(castleX);
            _battleCamera.Shake(collapse.Duration, _shakeStrength);
            SpawnCollapseSmoke(castleX);
            _audio.PlayCollapse();
            collapse.Play(() => CastleCollapsed?.Invoke(side));
        }

        private void SpawnCollapseSmoke(float castleX)
        {
            for (int i = 0; i < _collapseSmokeCount; i++)
            {
                var offset = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f) * _collapseSmokeArea.x,
                    UnityEngine.Random.value * _collapseSmokeArea.y, 0f);
                _smokes.Get()?.Play(new Vector3(castleX, _smokeHeight, 0f) + offset);
            }
        }
    }
}
