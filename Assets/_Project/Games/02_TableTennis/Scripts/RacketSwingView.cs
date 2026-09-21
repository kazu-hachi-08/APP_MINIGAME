using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// プレイヤーのラケットのスイング演出だけを担当する表示部品。
    /// 位置は RacketController が Update で決めるため、ここは LateUpdate で振り抜き分を上乗せする。
    /// 演出なので当たり判定・打球計算には一切影響しない。
    /// </summary>
    public class RacketSwingView : MonoBehaviour
    {
        [SerializeField] private TableLayout _table;
        [SerializeField] private RacketController _racket;
        [SerializeField] private PlayerSwing _playerSwing;

        [Header("Swing Animation")]
        [SerializeField] private float _swingDuration = 0.18f;

        [Tooltip("フリック方向へ振り抜く距離（ワールド単位）")]
        [SerializeField] private float _swingDistance = 0.5f;

        [SerializeField] private float _swingAngle = 55f;

        [Tooltip("空振りしたときの控えめな振り（ワールド単位・度）")]
        [SerializeField] private float _missDistanceScale = 0.6f;

        /// <summary>負の値なら再生していない</summary>
        private float _swingTime = -1f;

        private Vector2 _swingDirection = Vector2.up;
        private float _swingScale = 1f;

        private void OnEnable()
        {
            _playerSwing.OnShot += HandleShot;
            _playerSwing.OnMissed += HandleMissed;
        }

        private void OnDisable()
        {
            _playerSwing.OnShot -= HandleShot;
            _playerSwing.OnMissed -= HandleMissed;
        }

        private void HandleShot(FlickData flick, ShotResult shot)
        {
            // 強く振るほど大きく振り抜いて見えるようにする
            PlaySwing(flick.Direction, Mathf.Lerp(0.7f, 1.2f, shot.Strength));
        }

        private void HandleMissed(SwingJudgement judgement)
        {
            PlaySwing(Vector2.up, _missDistanceScale);
        }

        private void PlaySwing(Vector2 direction, float scale)
        {
            _swingDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
            _swingScale = scale;
            _swingTime = 0f;
        }

        private void LateUpdate()
        {
            float swing = AdvanceSwing();
            if (swing <= 0f)
            {
                transform.rotation = Quaternion.identity;
                return;
            }

            // ラケットの表示倍率と揃えて、奥行きが変わっても振り幅の見え方を一定にする
            float depthScale = _table.ScaleAt(_racket.RacketZ) / _table.NearScale;
            Vector2 offset = _swingDirection * (_swingDistance * _swingScale * swing * depthScale);

            transform.position += new Vector3(offset.x, offset.y, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, -_swingAngle * _swingScale * swing * Mathf.Sign(_swingDirection.x + 0.0001f));
        }

        /// <summary>スイングの進み具合を 0→1→0 の山で返す</summary>
        private float AdvanceSwing()
        {
            if (_swingTime < 0f) return 0f;

            float progress = _swingTime / Mathf.Max(0.01f, _swingDuration);
            if (progress >= 1f)
            {
                _swingTime = -1f;
                return 0f;
            }

            _swingTime += Time.deltaTime;
            return Mathf.Sin(progress * Mathf.PI);
        }
    }
}
