using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// サーブのボール出しだけを担当する。
    /// プレイヤー側は仕様通り自動トス（プレイヤーはラケット操作に集中する）、
    /// 相手側は Phase 6 で NPC に置き換えるまでの暫定として、そのまま台へ送り出す。
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
        [SerializeField] private float _tossUpSpeed = 3.2f;

        [Header("相手のサーブ（Phase 6 で NPC に置き換える）")]
        [SerializeField] private float _opponentServeZ = 1.5f;
        [SerializeField] private float _opponentServeHeight = 0.4f;
        [SerializeField] private float _opponentServeSpeed = 5.0f;
        [SerializeField] private float _opponentServeUpSpeed = 1.1f;

        [Tooltip("サーブのコースを毎回変える左右の振れ幅")]
        [SerializeField] private float _opponentServeSpreadX = 0.5f;

        /// <summary>プレイヤーが打てる位置へボールをトスする</summary>
        public void TossForPlayer()
        {
            Vector3 racket = _racket.CourtPosition;
            _ball.Launch(
                new Vector3(racket.x, _tossStartHeight, racket.z + _tossDepthOffset),
                new Vector3(0f, _tossUpSpeed, 0f),
                Vector2.zero);
        }

        /// <summary>相手側からサーブを送り出す</summary>
        public void ServeByOpponent()
        {
            float startX = Random.Range(-_opponentServeSpreadX, _opponentServeSpreadX);
            _ball.Launch(
                new Vector3(startX, _opponentServeHeight, _opponentServeZ),
                new Vector3(-startX * 0.5f, _opponentServeUpSpeed, -_opponentServeSpeed),
                Vector2.zero);
        }
    }
}
