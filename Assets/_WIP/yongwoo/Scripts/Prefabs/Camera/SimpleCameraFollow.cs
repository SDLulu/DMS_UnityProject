using UnityEngine;

[DisallowMultipleComponent]
// 역할:
// - 메인 카메라 오브젝트나 카메라 리그 프리팹에 붙어 플레이어 추적, 오프셋, 룩어헤드 보정을 담당합니다.
// - 대화나 컷신 중에는 다른 계층이 이 컴포넌트를 끄고 켜며 카메라 모드를 전환합니다.
//
// 구조 포인트:
// - 시스템 매니저가 아니라 카메라 오브젝트에 직접 붙는 런타임 추적 컴포넌트입니다.
public class SimpleCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);
    [SerializeField] private float followSpeed = 7.5f;
    [SerializeField] private float horizontalLookAhead = 1.45f;
    [SerializeField] private float lookAheadSmoothing = 8f;
    [SerializeField] private float verticalFollowSpeed = 5.5f;

    private Rigidbody2D _targetBody;
    private float _currentLookAhead;

    public PlayerCameraConfig CreateConfigSnapshot()
    {
        return new PlayerCameraConfig
        {
            offset = new SerializableVector3(offset.x, offset.y, offset.z),
            followSpeed = followSpeed,
            horizontalLookAhead = horizontalLookAhead,
            lookAheadSmoothing = lookAheadSmoothing,
            verticalFollowSpeed = verticalFollowSpeed
        };
    }

    public void ApplyConfig(PlayerCameraConfig config)
    {
        config = PlayerConfigLoader.Sanitize(new PlayerConfig { camera = config }).camera;
        offset = config.offset.ToVector3();
        followSpeed = config.followSpeed;
        horizontalLookAhead = config.horizontalLookAhead;
        lookAheadSmoothing = config.lookAheadSmoothing;
        verticalFollowSpeed = config.verticalFollowSpeed;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        _targetBody = target != null ? target.GetComponent<Rigidbody2D>() : null;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                return;
            }

            target = playerObject.transform;
            _targetBody = target.GetComponent<Rigidbody2D>();
        }

        if (_targetBody == null && target != null)
        {
            _targetBody = target.GetComponent<Rigidbody2D>();
        }

        float horizontalVelocity = _targetBody != null ? _targetBody.linearVelocity.x : 0f;
        float lookAheadTarget = Mathf.Abs(horizontalVelocity) > 0.1f ? Mathf.Sign(horizontalVelocity) * horizontalLookAhead : 0f;
        _currentLookAhead = Mathf.Lerp(_currentLookAhead, lookAheadTarget, lookAheadSmoothing * Time.deltaTime);

        Vector3 desiredPosition = target.position + offset + new Vector3(_currentLookAhead, 0f, 0f);
        Vector3 currentPosition = transform.position;
        currentPosition.x = Mathf.Lerp(currentPosition.x, desiredPosition.x, followSpeed * Time.deltaTime);
        currentPosition.y = Mathf.Lerp(currentPosition.y, desiredPosition.y, verticalFollowSpeed * Time.deltaTime);
        currentPosition.z = desiredPosition.z;
        transform.position = currentPosition;
    }
}
