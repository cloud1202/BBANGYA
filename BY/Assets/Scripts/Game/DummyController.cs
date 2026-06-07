using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 테스트용 더미 컨트롤러
/// 좌우로 왔다갔다하며 주기적으로 총을 쏨
/// </summary>
public class DummyController : MonoBehaviour
{
    [SerializeField] private float _moveRange = 3f;
    [SerializeField] private float _shootInterval = 2f;

    private PlayerController _controller;
    private Vector3 _startPos;
    private int _moveDir = 1;

    void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }

    void Start()
    {
        _startPos = transform.position;

        // 더미는 항상 땅에 있는 것처럼 처리
        _controller.isGround = true;
        _controller.isAction = false;

        AutoBehavior().Forget();
    }

    void Update()
    {
        if (_controller == null) return;

        // 땅 상태 유지 (무중력이라 isGround가 false될 수 있음)
        _controller.isGround = true;

        // 좌우 왔다갔다
        float distFromStart = transform.position.x - _startPos.x;
        if (distFromStart >= _moveRange) _moveDir = -1;
        else if (distFromStart <= -_moveRange) _moveDir = 1;

        // SetMoveDir 대신 직접 goalPos 설정
        _controller.SetDummyMoveDir(new Vector2(_moveDir, 0));
    }

    private async UniTaskVoid AutoBehavior()
    {
        while (this != null && gameObject != null)
        {
            await UniTask.WaitForSeconds(_shootInterval);
            if (_controller == null) break;

            // 공격
            _controller.isAction = false;
            _controller.DoAttack();
        }
    }
}
