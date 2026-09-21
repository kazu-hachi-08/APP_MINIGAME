using MiniGame.Common.Input;
using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// 操作対象選手の移動を制御する（Phase 1: 移動のみ）
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 4f;

        private Rigidbody2D _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            Vector2 moveInput = InputManager.HasInstance ? InputManager.Instance.MoveVector : Vector2.zero;
            _rigidbody.linearVelocity = moveInput * _moveSpeed;
        }
    }
}
