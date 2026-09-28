using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// オンライン対戦で打球に乗った技を伝えるための番号。
    /// 両端末とも相手の選手を知っているので、技のアセットではなく「弱か強か」だけを送れば足りる。
    /// </summary>
    public enum SpecialSlot
    {
        None,
        Weak,
        Strong
    }

    /// <summary>
    /// 必殺技の「獲得 → 発動 → 効果」をまとめる。
    /// 打球の数値は ShotCalculator / NpcController / BallMotion が必殺技データを見て変えるので、
    /// ここは「誰が持っているか」「いつ乗せるか」と、ボールの見た目・相手への効果の受け渡しだけを扱う。
    ///
    /// 弱必殺技は台上キャラに当てて、強必殺技は黄色ボールのラリーを取って獲得する。ストックは弱・強それぞれ1個まで。
    /// プレイヤーは SP ボタンで次の打球に乗せ、NPCは持っていれば次の返球で使う（使いどころの判断は持たせない）。
    /// ストックは実際に打ったときに減らす。乗せたまま得点が決まっても失わないようにするため。
    ///
    /// オンラインでは「NPC」側を相手プレイヤーとして扱う。相手の技は打球と一緒に届くので（ReceiveOpponentShot）、
    /// NPCへ技を持たせる処理は止める。台上キャラは両端末で出現をそろえる仕組みが無いため出さない（強必殺技だけ使える）。
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

        /// <summary>
        /// 片側（プレイヤー / NPC）の技とストック。
        /// 両者で同じ「獲得・消費・ラリー継続」の処理を書き分けずに済むよう、1つにまとめている。
        /// </summary>
        private class SpecialStock
        {
            public SpecialData Weak;
            public SpecialData Strong;
            public bool HasWeak;
            public bool HasStrong;

            /// <summary>ラリーが終わるまで効果が続く技（スターラリーなど）を使用中なら、その技</summary>
            public SpecialData RallySpecial;

            public void SetCharacter(CharacterData character)
            {
                Weak = character.WeakSpecial;
                Strong = character.StrongSpecial;
            }

            /// <summary>技を持っていない枠なら獲得して true を返す（ストックは弱・強それぞれ1個まで）</summary>
            public bool TryGrant(bool strong, out SpecialData special)
            {
                special = strong ? Strong : Weak;
                if (special == null || (strong ? HasStrong : HasWeak)) return false;

                if (strong)
                {
                    HasStrong = true;
                }
                else
                {
                    HasWeak = true;
                }

                return true;
            }

            /// <summary>
            /// 実際に打った技のストックを減らす。
            /// ラリー継続中の技は最初の1球で消費済みなので、2球目以降は減らさない
            /// </summary>
            public void Consume(SpecialData special)
            {
                if (special == RallySpecial) return;

                if (special == Strong)
                {
                    HasStrong = false;
                }
                else if (special == Weak)
                {
                    HasWeak = false;
                }
            }

            /// <summary>ラリー中ずっと続く技なら、次の打球にも乗せ直せるよう覚えておく</summary>
            public void KeepIfLastsForRally(SpecialData special)
            {
                if (special.LastsForRally)
                {
                    RallySpecial = special;
                }
            }

            /// <summary>NPCが次に使う技。ラリー継続中の技、強必殺技、弱必殺技の順に優先する</summary>
            public SpecialData PickNext()
            {
                if (RallySpecial != null) return RallySpecial;
                if (HasStrong) return Strong;
                if (HasWeak) return Weak;
                return null;
            }
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
            set => _mascot.CanAppear = value && enabled && !_isOnline;
        }

        private bool _isOnline;

        private readonly SpecialStock _playerStock = new SpecialStock();
        private readonly SpecialStock _npcStock = new SpecialStock();

        private bool _chanceRally;

        /// <summary>消える魔球が飛行中で、まだ相手コートでバウンドしていない</summary>
        private bool _invisibleShot;

        private Color _ballBaseColor;

        private void Awake()
        {
            _ballBaseColor = _ballRenderer.color;
            _weakButton.Button.onClick.AddListener(() => Arm(_playerStock.Weak, _playerStock.HasWeak));
            _strongButton.Button.onClick.AddListener(() => Arm(_playerStock.Strong, _playerStock.HasStrong));
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
        public void SetSpecials(CharacterData player, CharacterData npc, bool isOnline = false)
        {
            _isOnline = isOnline;
            _playerStock.SetCharacter(player);
            _npcStock.SetCharacter(npc);
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
                Grant(scorer, strong: true);
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

            UpdateBallVisibility();
        }

        /// <summary>消える魔球の見え隠れ。色は技ごとに変わるので、不透明度だけで表す</summary>
        private void UpdateBallVisibility()
        {
            Color color = _ballRenderer.color;
            color.a = _invisibleShot && IsOverReceiverSide(_ball.CourtPosition.z) ? 0f : 1f;
            _ballRenderer.color = color;
        }

        /// <summary>ネットを越えて、打たれた側（受ける側）のコートにいるか</summary>
        private bool IsOverReceiverSide(float courtZ)
        {
            return courtZ * _ball.Velocity.z > 0f;
        }

        private void HandleBounced(Vector3 contact)
        {
            // 相手コートで跳ねたら見せる。サーブの自陣バウンドでは消えたままにしない（まだネットを越えていない）
            if (IsOverReceiverSide(contact.z))
            {
                _invisibleShot = false;
            }
        }

        // ---- 獲得 ----

        private void HandleMascotHit(CourtSide hitter)
        {
            Grant(hitter, strong: false);
        }

        private void Grant(CourtSide side, bool strong)
        {
            bool isPlayer = side == CourtSide.Player;
            SpecialStock stock = isPlayer ? _playerStock : _npcStock;
            if (!stock.TryGrant(strong, out SpecialData special)) return;

            if (isPlayer)
            {
                RefreshButtons();
            }
            else
            {
                RefillNpc();
            }

            OnGranted?.Invoke(side, special);
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
            SpecialData special = shot.Special;
            if (special == null)
            {
                ResetBallLook();
                return;
            }

            _playerStock.Consume(special);
            ApplyShotLook(special);

            // オンラインでは相手端末が打球と一緒に受け取って足止めなどを反映する
            if (!_isOnline)
            {
                _npc.ReceiveSpecial(special);
            }

            _playerStock.KeepIfLastsForRally(special);
            RearmPlayerRallySpecial();
            RefreshButtons();
        }

        /// <summary>ラリー中ずっと続く技は、次の打球にも乗せ直す</summary>
        private void RearmPlayerRallySpecial()
        {
            if (_playerStock.RallySpecial != null && _shotCalculator.PendingSpecial == null)
            {
                _shotCalculator.PendingSpecial = _playerStock.RallySpecial;
            }
        }

        // ---- NPCの発動 ----

        /// <summary>NPCに次の返球で使う技を持たせる</summary>
        private void RefillNpc()
        {
            // オンラインでは相手が自分で使いどころを決める
            if (_isOnline) return;
            if (_npc.PendingSpecial != null) return;

            _npc.PendingSpecial = _npcStock.PickNext();
        }

        private void HandleNpcReturned()
        {
            ResetBallLook();
        }

        private void HandleNpcSpecialShot(SpecialData special)
        {
            // ラリー継続中の技の2球目以降は、新たに使ったことにしない
            if (special != _npcStock.RallySpecial)
            {
                _npcStock.Consume(special);
                OnNpcUsed?.Invoke(special);
            }

            _npcStock.KeepIfLastsForRally(special);
            ApplyShotLook(special);

            if (special.StunDuration > 0f)
            {
                _playerRacket.Stun(special.StunDuration, special.StunMoveMultiplier);
            }

            RefillNpc();
        }

        // ---- オンライン ----

        /// <summary>送信用に、自分の打球に乗った技を番号へ変える</summary>
        public SpecialSlot SlotOf(SpecialData special)
        {
            if (special == null) return SpecialSlot.None;
            return special == _playerStock.Strong ? SpecialSlot.Strong : SpecialSlot.Weak;
        }

        /// <summary>
        /// 相手の打球が届いたときに呼ぶ（ボールを発射した直後・早送りの前）。
        /// 技の球ならNPCが使ったときと同じく、見た目・跳ね方・自分の足止めを反映する
        /// </summary>
        public void ReceiveOpponentShot(SpecialSlot slot)
        {
            SpecialData special = OpponentSpecialOf(slot);
            if (special == null)
            {
                ResetBallLook();
                return;
            }

            // RefillNpc はオンラインだと何もしないので、NPCの打球と同じ処理をそのまま使える
            HandleNpcSpecialShot(special);
        }

        private SpecialData OpponentSpecialOf(SpecialSlot slot)
        {
            switch (slot)
            {
                case SpecialSlot.Strong: return _npcStock.Strong;
                case SpecialSlot.Weak: return _npcStock.Weak;
                default: return null;
            }
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
            if (_playerStock.RallySpecial != null && _shotCalculator.PendingSpecial == _playerStock.RallySpecial)
            {
                _shotCalculator.PendingSpecial = null;
            }

            if (_npcStock.RallySpecial != null && _npc.PendingSpecial == _npcStock.RallySpecial)
            {
                _npc.PendingSpecial = null;
            }

            _playerStock.RallySpecial = null;
            _npcStock.RallySpecial = null;
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
            RefreshButton(_weakButton, _playerStock.Weak, _playerStock.HasWeak, _readyColor);
            RefreshButton(_strongButton, _playerStock.Strong, _playerStock.HasStrong, _strongReadyColor);
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
