using System;
using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// オンライン対戦の相手ラケット。届いた位置へ滑らかに寄せて見せるだけの表示用で、当たり判定は持たない
    /// （相手の打球結果は相手端末で確定して OnlineMatchLink 経由で届くため）。
    /// NPC用の表示コンポーネント（NpcRacketView / CharacterView）をそのまま使えるよう IOpponentRacket を実装する。
    /// </summary>
    public class RemoteOpponent : MonoBehaviour, IOpponentRacket
    {
        [SerializeField] private OnlineMatchLink _link;
        [SerializeField] private NpcRacketView _racketView;
        [SerializeField] private CharacterView _characterView;

        [Tooltip("届いた位置へ追いつく速さ。位置は約20回/秒でしか届かないため、補間してカクつきを隠す")]
        [SerializeField] private float _followSpeed = 15f;

        [Tooltip("位置が届く前の構え位置")]
        [SerializeField] private Vector3 _readyPosition = new Vector3(0f, 0.25f, 1.7f);

        public event Action OnSwing;

        public Vector3 CourtPosition { get; private set; }

        private Vector3 _target;

        private void Awake()
        {
            CourtPosition = _readyPosition;
            _target = _readyPosition;
        }

        private void OnEnable()
        {
            _link.OnRacketReceived += HandleRacketReceived;
        }

        private void OnDisable()
        {
            _link.OnRacketReceived -= HandleRacketReceived;
        }

        /// <summary>相手側の表示をNPCからこのラケットへ切り替える</summary>
        public void TakeOverViews()
        {
            _racketView.SetSource(this);
            _characterView.SetActor(this);
        }

        /// <summary>相手が打った。打点へ即座に合わせてからスイングを見せる</summary>
        public void PlaySwing(Vector3 hitPosition)
        {
            CourtPosition = hitPosition;
            _target = hitPosition;
            OnSwing?.Invoke();
        }

        private void HandleRacketReceived(Vector3 position)
        {
            _target = position;
        }

        private void Update()
        {
            CourtPosition = Vector3.Lerp(CourtPosition, _target, 1f - Mathf.Exp(-_followSpeed * Time.deltaTime));
        }
    }
}
