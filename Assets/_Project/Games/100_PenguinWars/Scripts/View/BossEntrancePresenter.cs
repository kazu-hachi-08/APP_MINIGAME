using System.Collections;
using UnityEngine;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ボス登場の演出（ステージ計画 Phase 7）: WARNING の帯と警報 → カメラが敵の城の前へ寄って戻る → 画面揺れ。
    /// ボスがいる間は BGM をボス用に切り替える。
    /// 演出中もゲームの時間は止めず操作も奪わない（出撃が遅れて負けると理不尽に感じるため）
    /// </summary>
    public class BossEntrancePresenter : MonoBehaviour
    {
        [SerializeField] private BattleRunner _battleRunner;
        [SerializeField] private BattleCamera _battleCamera;
        [SerializeField] private BossWarningBanner _banner;
        [SerializeField] private PenguinWarsAudio _audio;

        [Header("カメラ")]
        [SerializeField] private float _peekDuration = 0.8f;
        [Tooltip("城から自城側へずらした所を映す。城の真上だと、出てきたボスが画面の端に寄ってしまう")]
        [SerializeField] private float _peekCastleOffset = 4f;

        [Header("画面揺れ（カメラが寄り切ったところで揺らす）")]
        [SerializeField] private float _shakeDelay = 0.25f;
        [SerializeField] private float _shakeDuration = 0.6f;
        [SerializeField] private float _shakeStrength = 0.3f;

        // ボスが何体も出る最終ボスで毎回カメラを動かすとうるさいので、カメラは1回目だけ
        private bool _hasPeeked;
        private bool _bossPresent;
        private bool _stopped;

        /// <param name="castleX">ボスが出た敵の城の X</param>
        public void Play(float castleX)
        {
            if (_stopped) return;

            StopAllCoroutines();
            StartCoroutine(Entrance(castleX));
        }

        /// <summary>城が落ちたら演出をやめる（崩れる城を映すカメラを横取りしないように）</summary>
        public void Stop()
        {
            _stopped = true;
            StopAllCoroutines();
            _banner.Hide();
        }

        private IEnumerator Entrance(float castleX)
        {
            _banner.Play();
            _audio.PlayAlarm();
            yield return new WaitForSeconds(_banner.Duration);

            if (!_hasPeeked)
            {
                // あそびかたのデモは毎ループ寄って見せる。ここで使い切ると、同じシーンで遊ぶ本番の1回目で寄らなくなる
                _hasPeeked = !_battleRunner.IsDemo;
                float direction = castleX > _battleRunner.FieldLength * 0.5f ? -1f : 1f;
                _battleCamera.Peek(castleX + direction * _peekCastleOffset, _peekDuration);
                yield return new WaitForSeconds(_shakeDelay);
            }
            _battleCamera.Shake(_shakeDuration, _shakeStrength);
        }

        private void Update()
        {
            if (_stopped || !_battleRunner.IsRunning) return;

            bool present = BossHpBar.FindBoss(_battleRunner.World) != null;
            if (present == _bossPresent) return;

            _bossPresent = present;
            _audio.SetBossBgm(present);
        }
    }
}
