using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 必殺技の「獲得 → 発動 → 効果」をまとめる。
    /// 打球の数値は ShotCalculator / NpcController / BallMotion が必殺技データを見て変えるので、
    /// ここは「誰が持っているか」「いつ乗せるか」と、ボールの見た目・相手への効果の受け渡しだけを扱う。
    ///
    /// 弱必殺技は台上キャラに当てて、強必殺技は黄色ボールのラリーを取って獲得する。ストックは弱・強それぞれ1個まで。
    /// プレイヤーは SP ボタンで次の打球に乗せ、NPCは持っていれば次の返球で使う（使いどころの判断は持たせない）。
    /// ストックは実際に打ったときに減らす。乗せたまま得点が決まっても失わないようにするため。
    /// </summary>
    public class SpecialController : MonoBehaviour
    {
        /// <summary>弱・強それぞれの SP ボタン</summary>
        [Serializable]
        private class SpecialButton
        {
            public Button Button;
            public Text Label;
        }

        [SerializeField] private BallMotion _ball;
        [SerializeField] private SpriteRenderer _ballRenderer;
        [SerializeField] private PlayerSwing _playerSwing;
        [SerializeField] private ShotCalculator _shotCalculator;
        [SerializeField] private RacketController _playerRacket;
        [SerializeField] private NpcController _npc;
        [SerializeField] private TableMascot _mascot;

        [Header("黄色ボール（強必殺技の獲得ラリー）")]
        [Tooltip("サーブ前の合計得点がこの値のラリーを黄色ボールにする。11点先取なので、試合の序盤・中盤・終盤に1回ずつ置いている")]
        [SerializeField] private int[] _chancePointTotals = { 4, 9, 14 };

        [SerializeField] private Color _chanceBallColor = new Color(1f, 0.9f, 0.1f);

        [Header("UI")]
        [SerializeField] private SpecialButton _weakButton;
        [SerializeField] private SpecialButton _strongButton;

        [SerializeField] private Color _readyColor = new Color(0.95f, 0.45f, 0.1f);
        [SerializeField] private Color _strongReadyColor = new Color(0.85f, 0.7f, 0.05f);
        [SerializeField] private Color _armedColor = new Color(0.9f, 0.2f, 0.2f);

        /// <summary>必殺技を獲得した（獲得した側と技）</summary>
        public event Action<CourtSide, SpecialData> OnGranted;

        /// <summary>NPCが必殺技を使った</summary>
        public event Action<SpecialData> OnNpcUsed;

        /// <summary>台上キャラを出してよいか。ラリー中だけ true にする</summary>
        public bool CanAppear
        {
            set => _mascot.CanAppear = value && enabled;
        }

        private SpecialData _playerWeak;
        private SpecialData _playerStrong;
        private SpecialData _npcWeak;
        private SpecialData _npcStrong;

        private bool _playerHasWeak;
        private bool _playerHasStrong;
        private bool _npcHasWeak;
        private bool _npcHasStrong;

        /// <summary>ラリーが終わるまで効果が続く技（スターラリーなど）を使用中なら、その技</summary>
        private SpecialData _playerRallySpecial;
        private SpecialData _npcRallySpecial;

        private bool _chanceRally;

        /// <summary>消える魔球が飛行中で、まだ相手コートでバウンドしていない</summary>
        private bool _invisibleShot;

        private Color _ballBaseColor;

        private void Awake()
        {
            _ballBaseColor = _ballRenderer.color;
            _weakButton.Button.onClick.AddListener(() => Arm(_playerWeak, _playerHasWeak));
            _strongButton.Button.onClick.AddListener(() => Arm(_playerStrong, _playerHasStrong));
            RefreshButtons();
        }

        private void OnEnable()
        {
            _mascot.OnHit += HandleMascotHit;
            _playerSwing.OnShot += HandlePlayerShot;
            _npc.OnReturned += HandleNpcReturned;
            _npc.OnSpecialShot += HandleNpcSpecialShot;
            _ball.OnBounced += HandleBounced;
        }

        private void OnDisable()
        {
            _mascot.OnHit -= HandleMascotHit;
            _playerSwing.OnShot -= HandlePlayerShot;
            _npc.OnReturned -= HandleNpcReturned;
            _npc.OnSpecialShot -= HandleNpcSpecialShot;
            _ball.OnBounced -= HandleBounced;
        }

        /// <summary>選んだ選手の必殺技を設定する（試合開始前に呼ぶ）</summary>
        public void SetSpecials(CharacterData player, CharacterData npc)
        {
            _playerWeak = player.WeakSpecial;
            _playerStrong = player.StrongSpecial;
            _npcWeak = npc.WeakSpecial;
            _npcStrong = npc.StrongSpecial;
        }

        /// <summary>サーブ前に呼ぶ。このラリーが黄色ボール（強必殺技の獲得ラリー）なら true を返す</summary>
        public bool PrepareRally(int totalPoints)
        {
            _chanceRally = enabled && Array.IndexOf(_chancePointTotals, totalPoints) >= 0;
            ResetBallLook();
            return _chanceRally;
        }

        /// <summary>得点が決まったときに呼ぶ。黄色ボールなら得点した側へ強必殺技を渡し、ラリー限定の効果を切る</summary>
        public void EndRally(CourtSide scorer)
        {
            if (_chanceRally)
            {
                GrantStrong(scorer);
            }

            _chanceRally = false;
            EndRallySpecials();
            ResetBallLook();
        }

        private void LateUpdate()
        {
            // 必殺技の球が決着したら、次のラリーへ見た目を持ち越さない
            if (!_ball.IsFlying)
            {
                ResetBallLook();
            }

            // 色は技ごとに変わるので、見え隠れは不透明度だけで表す
            Color color = _ballRenderer.color;
            color.a = _invisibleShot && IsOverReceiverCourt() ? 0f : 1f;
            _ballRenderer.color = color;
        }

        /// <summary>ネットを越えて、打った相手側のコートの上にいるか</summary>
        private bool IsOverReceiverCourt()
        {
            return _ball.CourtPosition.z * _ball.Velocity.z > 0f;
        }

        private void HandleBounced(Vector3 contact)
        {
            // 相手コートで跳ねたら見せる。サーブの自陣バウンドでは消えたままにしない（まだネットを越えていない）
            if (contact.z * _ball.Velocity.z > 0f)
            {
                _invisibleShot = false;
            }
        }

        // ---- 獲得 ----

        private void HandleMascotHit(CourtSide hitter)
        {
            if (hitter == CourtSide.Player)
            {
                if (_playerWeak == null || _playerHasWeak) return;

                _playerHasWeak = true;
                RefreshButtons();
                OnGranted?.Invoke(CourtSide.Player, _playerWeak);
                return;
            }

            if (_npcWeak == null || _npcHasWeak) return;

            _npcHasWeak = true;
            RefillNpc();
            OnGranted?.Invoke(CourtSide.Opponent, _npcWeak);
        }

        private void GrantStrong(CourtSide scorer)
        {
            if (scorer == CourtSide.Player)
            {
                if (_playerStrong == null || _playerHasStrong) return;

                _playerHasStrong = true;
                RefreshButtons();
                OnGranted?.Invoke(CourtSide.Player, _playerStrong);
                return;
            }

            if (_npcStrong == null || _npcHasStrong) return;

            _npcHasStrong = true;
            RefillNpc();
            OnGranted?.Invoke(CourtSide.Opponent, _npcStrong);
        }

        // ---- プレイヤーの発動 ----

        /// <summary>SP ボタン。次の打球に乗せる（空振りしても次の打球まで持ち越す）</summary>
        private void Arm(SpecialData special, bool hasStock)
        {
            if (!hasStock || special == null) return;

            _shotCalculator.PendingSpecial = special;
            RefreshButtons();
        }

        private void HandlePlayerShot(FlickData flick, ShotResult shot)
        {
            if (shot.Special == null)
            {
                ResetBallLook();
                return;
            }

            ConsumePlayerStock(shot.Special);
            ApplyShotLook(shot.Special);
            _npc.ReceiveSpecial(shot.Special);

            // ラリー中ずっと続く技は、次の打球にも乗せ直す
            if (shot.Special.LastsForRally)
            {
                _playerRallySpecial = shot.Special;
            }

            if (_playerRallySpecial != null && _shotCalculator.PendingSpecial == null)
            {
                _shotCalculator.PendingSpecial = _playerRallySpecial;
            }

            RefreshButtons();
        }

        private void ConsumePlayerStock(SpecialData special)
        {
            if (special == _playerRallySpecial) return;

            if (special == _playerStrong)
            {
                _playerHasStrong = false;
            }
            else if (special == _playerWeak)
            {
                _playerHasWeak = false;
            }
        }

        // ---- NPCの発動 ----

        /// <summary>NPCに次の返球で使う技を持たせる。強必殺技を優先する</summary>
        private void RefillNpc()
        {
            if (_npc.PendingSpecial != null) return;

            if (_npcRallySpecial != null)
            {
                _npc.PendingSpecial = _npcRallySpecial;
            }
            else if (_npcHasStrong)
            {
                _npc.PendingSpecial = _npcStrong;
            }
            else if (_npcHasWeak)
            {
                _npc.PendingSpecial = _npcWeak;
            }
        }

        private void HandleNpcReturned()
        {
            ResetBallLook();
        }

        private void HandleNpcSpecialShot(SpecialData special)
        {
            if (special != _npcRallySpecial)
            {
                if (special == _npcStrong) _npcHasStrong = false;
                else if (special == _npcWeak) _npcHasWeak = false;

                OnNpcUsed?.Invoke(special);
            }

            if (special.LastsForRally)
            {
                _npcRallySpecial = special;
            }

            ApplyShotLook(special);
            if (special.StunDuration > 0f)
            {
                _playerRacket.Stun(special.StunDuration, special.StunMoveMultiplier);
            }

            RefillNpc();
        }

        // ---- 共通 ----

        /// <summary>技の球だと分かる見た目と、相手コートでの跳ね方を反映する</summary>
        private void ApplyShotLook(SpecialData special)
        {
            _ballRenderer.color = special.BallColor;
            _invisibleShot = special.Invisible;
            _ball.NextBounceHeightMultiplier = special.BounceHeightMultiplier;
        }

        private void EndRallySpecials()
        {
            if (_playerRallySpecial != null && _shotCalculator.PendingSpecial == _playerRallySpecial)
            {
                _shotCalculator.PendingSpecial = null;
            }

            if (_npcRallySpecial != null && _npc.PendingSpecial == _npcRallySpecial)
            {
                _npc.PendingSpecial = null;
            }

            _playerRallySpecial = null;
            _npcRallySpecial = null;
            RefillNpc();
            RefreshButtons();
        }

        private void ResetBallLook()
        {
            _ballRenderer.color = _chanceRally ? _chanceBallColor : _ballBaseColor;
            _invisibleShot = false;
        }

        private void RefreshButtons()
        {
            RefreshButton(_weakButton, _playerWeak, _playerHasWeak, _readyColor);
            RefreshButton(_strongButton, _playerStrong, _playerHasStrong, _strongReadyColor);
        }

        private void RefreshButton(SpecialButton button, SpecialData special, bool hasStock, Color readyColor)
        {
            bool visible = hasStock && special != null;
            button.Button.gameObject.SetActive(visible);
            if (!visible) return;

            bool armed = _shotCalculator.PendingSpecial == special;
            button.Button.GetComponent<Image>().color = armed ? _armedColor : readyColor;
            button.Label.text = armed ? $"{special.DisplayName}\n準備OK" : $"SP\n{special.DisplayName}";
        }
    }
}
