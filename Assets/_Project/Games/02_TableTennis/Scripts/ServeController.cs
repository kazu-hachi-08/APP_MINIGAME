using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// プレイヤーのサーブのボール出し（自動トス）だけを担当する。
    /// 仕様通りトスは自動で行い、プレイヤーはラケット操作に集中する。
    /// 相手のサーブは打球内容を自分で決める NpcController が受け持つ。
    /// </summary>
    public class ServeController : MonoBehaviour
    {
        [SerializeField] private BallMotion _ball;
        [SerializeField] private RacketController _racket;

        [Header("プレイヤーの自動トス")]
        [Tooltip("ラケットより奥にトスする距離。打ち頃の位置に上がるようにする")]
        [SerializeField] private float _tossDepthOffset = 0.2f;

        [SerializeField] private float _tossStartHeight = 0.1f;

        [Tooltip("トスの初速。大きいほど高く上がり、打てる時間が長くなる")]
        [SerializeField] private float _tossUpSpeed = 4.0f;

        /// <summary>プレイヤーが打てる位置へボールをトスする</summary>
        public void TossForPlayer()
        {
            Vector3 racket = _racket.CourtPosition;
            _ball.Launch(
                new Vector3(racket.x, _tossStartHeight, racket.z + _tossDepthOffset),
                new Vector3(0f, _tossUpSpeed, 0f),
                Vector2.zero);
        }
    }
}
