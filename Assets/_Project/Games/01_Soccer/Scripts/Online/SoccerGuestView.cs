using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// オンライン対戦のゲスト端末で、ホストから届いた状態どおりに選手とボールを表示する。
    /// ゲストでは物理・AI・操作を止め、ここが位置を書き込むだけにする（判定はすべてホストが行う）。
    ///
    /// 状態は毎秒約30回しか届かないので、届いた速度で少し先読みした位置へ滑らかに寄せてカクつきを隠す。
    /// </summary>
    public class SoccerGuestView : MonoBehaviour
    {
        [SerializeField] private SoccerOnlineLink _link;
        [SerializeField] private Ball _ball;
        [SerializeField] private PlayerSwitcher[] _switchers;
        [SerializeField] private CameraFollow _cameraFollow;
        [SerializeField] private Transform _controlMarker;

        [Tooltip("届いた位置へ追いつく速さ")]
        [SerializeField] private float _followSpeed = 20f;

        [Tooltip("これ以上離れていたら補間せず瞬時に合わせる（ゴール後のリセットなど）")]
        [SerializeField] private float _snapDistance = 3f;

        [Tooltip("先読みする時間の上限（秒）。長いと壁際でボールが壁の外へはみ出して見える")]
        [SerializeField] private float _maxLead = 0.12f;

        private TeamMember[] _players;
        private Transform[] _transforms;
        private PlayerSpriteAnimator[] _animators;
        private TackleReaction[] _reactions;
        private bool[] _wasStaggered;

        private SoccerSnapshot _latest;
        private float _receivedAt;
        private int _controlledIndex = -1;

        /// <summary>ゲストとして試合に入ったときに呼ぶ。以降、この端末では試合を計算しない</summary>
        public void Begin()
        {
            _players = _link.Players;
            CacheComponents();
            StopLocalSimulation();

            _link.OnSnapshotReceived += HandleSnapshot;
        }

        private void OnDestroy()
        {
            if (_link != null)
            {
                _link.OnSnapshotReceived -= HandleSnapshot;
            }
        }

        private void CacheComponents()
        {
            int count = _players.Length;
            _transforms = new Transform[count];
            _animators = new PlayerSpriteAnimator[count];
            _reactions = new TackleReaction[count];
            _wasStaggered = new bool[count];

            for (int i = 0; i < count; i++)
            {
                _transforms[i] = _players[i].transform;
                _animators[i] = _players[i].GetComponent<PlayerSpriteAnimator>();
                _reactions[i] = _players[i].GetComponent<TackleReaction>();
            }
        }

        /// <summary>
        /// ゲスト側で物理やAIが動くと、ホストと違う結果（ゴール判定など）を勝手に出してしまうため全て止める
        /// </summary>
        private void StopLocalSimulation()
        {
            // 切り替え役を先に止めないと、止めた操作コンポーネントを再び有効にされてしまう
            foreach (var switcher in _switchers)
            {
                if (switcher != null) switcher.enabled = false;
            }

            foreach (var player in _players)
            {
                SetEnabled<AIPlayerController>(player, false);
                SetEnabled<PlayerController>(player, false);
                SetEnabled<SlidingTackle>(player, false);
                player.GetComponent<Rigidbody2D>().simulated = false;
            }

            _ball.GetComponent<Rigidbody2D>().simulated = false;
        }

        private static void SetEnabled<T>(Component owner, bool enabled) where T : Behaviour
        {
            if (owner.TryGetComponent<T>(out var behaviour)) behaviour.enabled = enabled;
        }

        private void HandleSnapshot(SoccerSnapshot snapshot)
        {
            _latest = snapshot;
            _receivedAt = Time.time;

            ApplyStagger(snapshot);
            ApplyControlledPlayer(snapshot.AwayControlledIndex);
        }

        /// <summary>転倒は開始の瞬間だけ伝え、起き上がりは TackleReaction 自身のタイマーに任せる</summary>
        private void ApplyStagger(SoccerSnapshot snapshot)
        {
            int count = Mathf.Min(snapshot.Players.Length, _players.Length);
            for (int i = 0; i < count; i++)
            {
                bool staggered = snapshot.Players[i].IsStaggered;
                if (staggered && !_wasStaggered[i] && _reactions[i] != null)
                {
                    _reactions[i].Stagger();
                }

                _wasStaggered[i] = staggered;
            }
        }

        /// <summary>ゲストはAWAYを操作するので、ホストが選んだAWAYの操作選手をカメラとマーカーで追う</summary>
        private void ApplyControlledPlayer(int index)
        {
            if (index < 0 || index >= _transforms.Length || index == _controlledIndex) return;

            _controlledIndex = index;
            if (_cameraFollow != null)
            {
                _cameraFollow.SetTarget(_transforms[index]);
            }
        }

        private void Update()
        {
            if (_latest == null) return;

            // 届いてからの経過時間ぶん先読みして、通信遅延による遅れを小さくする
            float lead = Mathf.Min(_latest.Age + (Time.time - _receivedAt), _maxLead);
            float t = 1f - Mathf.Exp(-_followSpeed * Time.deltaTime);

            int count = Mathf.Min(_latest.Players.Length, _transforms.Length);
            for (int i = 0; i < count; i++)
            {
                PlayerSnapshot state = _latest.Players[i];
                MoveToward(_transforms[i], state.Position + state.Velocity * lead, t);

                if (_animators[i] != null)
                {
                    _animators[i].SetExternalVelocity(state.Velocity);
                }
            }

            MoveToward(_ball.transform, _latest.BallPosition + _latest.BallVelocity * lead, t);
        }

        private void LateUpdate()
        {
            if (_controlMarker == null || _controlledIndex < 0) return;

            Vector3 position = _transforms[_controlledIndex].position;
            _controlMarker.position = new Vector3(position.x, position.y, _controlMarker.position.z);
        }

        private void MoveToward(Transform target, Vector2 destination, float t)
        {
            Vector2 current = target.position;
            Vector2 next = Vector2.Distance(current, destination) > _snapDistance
                ? destination
                : Vector2.Lerp(current, destination, t);

            target.position = new Vector3(next.x, next.y, target.position.z);
        }
    }
}
