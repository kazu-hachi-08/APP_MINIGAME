using System.Collections;
using MiniGame.Common.Audio;
using MiniGame.Common.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 卓球ゲームの進行管理（Phase 1〜3 時点）。
    /// 得点・サーブ・NPC はまだ無いため、ラリーが切れたら仮の球出しでラリーを再開する。
    /// Phase 5 で得点・サーブへ、Phase 6 で NPC の返球へ置き換える。
    /// </summary>
    public class TableTennisGameManager : BaseMiniGameManager
    {
        [Header("References")]
        [SerializeField] private BallMotion _ball;
        [SerializeField] private PlayerSwing _playerSwing;

        [Header("UI")]
        [SerializeField] private Text _messageText;

        [Tooltip("フリック→打球・回転の対応を確認するための開発用表示（Phase 8 で整理する）")]
        [SerializeField] private Text _shotInfoText;

        [Header("仮の球出し（Phase 5/6 でサーブ・NPCに置き換える）")]
        [SerializeField] private float _feedDelay = 1.0f;
        [SerializeField] private float _feedStartZ = 1.2f;
        [SerializeField] private float _feedHeight = 0.35f;
        [SerializeField] private float _feedForwardSpeed = 5.5f;
        [SerializeField] private float _feedUpSpeed = 1.2f;
        [SerializeField] private float _feedSpreadX = 0.45f;

        private void OnEnable()
        {
            _ball.OnRallyEnded += HandleRallyEnded;
            _ball.OnBounced += HandleBounced;
            _playerSwing.OnShot += HandleShot;
        }

        private void OnDisable()
        {
            _ball.OnRallyEnded -= HandleRallyEnded;
            _ball.OnBounced -= HandleBounced;
            _playerSwing.OnShot -= HandleShot;
        }

        protected override void OnGameReady()
        {
            _ball.Stop();
            SetMessage(string.Empty);
            SetShotInfo("フリックして打つ");
            StartGame();
            StartCoroutine(FeedBallRoutine("READY"));
        }

        protected override void Update()
        {
            base.Update();

            // ポーズ中やラリー間にフリックが打球として通らないようにする
            _playerSwing.CanSwing = IsPlaying && _ball.IsFlying;
        }

        /// <summary>
        /// 相手側からボールを送り出す。ラリーを続けて操作感を確かめるための暫定処理。
        /// </summary>
        private IEnumerator FeedBallRoutine(string message)
        {
            SetMessage(message);
            yield return new WaitForSeconds(_feedDelay);
            SetMessage(string.Empty);

            float startX = Random.Range(-_feedSpreadX, _feedSpreadX);
            _ball.Launch(
                new Vector3(startX, _feedHeight, _feedStartZ),
                new Vector3(-startX * 0.5f, _feedUpSpeed, -_feedForwardSpeed),
                Vector2.zero);
        }

        private void HandleRallyEnded(RallyEndReason reason)
        {
            if (!IsPlaying) return;

            StartCoroutine(FeedBallRoutine(ToMessage(reason)));
        }

        private static string ToMessage(RallyEndReason reason)
        {
            switch (reason)
            {
                case RallyEndReason.Net: return "NET";
                case RallyEndReason.OutOfTable: return "OUT";
                case RallyEndReason.PastPlayer: return "MISS";
                default: return "RALLY END";
            }
        }

        private void HandleBounced(Vector3 contact)
        {
            // Phase 8 でバウンドSEに差し替える
        }

        private void HandleShot(FlickData flick, ShotResult shot)
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlaySe(SeId.Kick);
            }

            SetShotInfo(BuildShotInfo(flick, shot));
        }

        /// <summary>
        /// フリック方向・速度が打球方向・速度・回転へどう分配されたかを可視化する
        /// </summary>
        private static string BuildShotInfo(FlickData flick, ShotResult shot)
        {
            string spinLabel = DescribeSpin(shot.Spin);
            return $"FLICK 方向({flick.Direction.x:0.00}, {flick.Direction.y:0.00})  速度 {flick.Speed:0.00} (強さ {shot.Strength:0.00})\n"
                 + $"打球  前方 {shot.Velocity.z:0.0} m/s  左右 {shot.Velocity.x:+0.0;-0.0;0.0}  打ち上げ {shot.Velocity.y:0.0}\n"
                 + $"回転  {spinLabel}  (top {shot.Spin.y:+0.00;-0.00;0.00} / side {shot.Spin.x:+0.00;-0.00;0.00})";
        }

        private static string DescribeSpin(Vector2 spin)
        {
            const float deadZone = 0.1f;

            string vertical = spin.y > deadZone ? "トップスピン"
                : spin.y < -deadZone ? "バックスピン"
                : "";
            string horizontal = spin.x > deadZone ? "右サイドスピン"
                : spin.x < -deadZone ? "左サイドスピン"
                : "";

            if (vertical.Length > 0 && horizontal.Length > 0) return vertical + " + " + horizontal;
            if (vertical.Length > 0) return vertical;
            if (horizontal.Length > 0) return horizontal;
            return "ほぼ無回転";
        }

        private void SetMessage(string message)
        {
            if (_messageText == null) return;

            _messageText.text = message;
            _messageText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        private void SetShotInfo(string info)
        {
            if (_shotInfoText != null)
            {
                _shotInfoText.text = info;
            }
        }
    }
}
