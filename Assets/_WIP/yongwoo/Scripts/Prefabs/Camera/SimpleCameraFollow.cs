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
    private float _shakeTimer;
    private float _shakeDuration;
    private float _shakeStrength;
    private bool _arenaLocked;
    private Vector3 _arenaFixedPosition;
    private Vector3 _parallaxLockPlayerReference;
    private Vector3 _parallaxLockCameraReference;

    public bool IsArenaLocked => _arenaLocked;

    /// <summary>
    /// 패럴럭스 기준점. 평소는 camera.position과 동일하고,
    /// 보스전 고정 시에는 입장 시 카메라·캐릭터 오차를 유지한 채 좌우 이동만 반영합니다.
    /// (reference = player + parallaxOffsetFromPlayer)
    /// </summary>
    public Vector3 GetParallaxReferencePosition()
    {
        Transform player = ResolveTarget();
        if (player == null)
        {
            return transform.position;
        }

        return player.position + GetParallaxOffsetFromPlayer(player);
    }

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

    public void LockToArenaPosition(Vector3 worldPosition)
    {
        Transform player = ResolveTarget();
        _parallaxLockCameraReference = transform.position;

        _arenaLocked = true;
        _arenaFixedPosition = worldPosition;
        _currentLookAhead = 0f;
        transform.position = worldPosition;

        if (player != null)
        {
            _parallaxLockPlayerReference = player.position;
        }
    }

    public void UnlockArenaFollow()
    {
        _arenaLocked = false;
    }

    private Vector3 GetParallaxOffsetFromPlayer(Transform player)
    {
        if (!_arenaLocked)
        {
            // 매 프레임 실제 카메라-캐릭터 오차(offset + lookAhead + lerp) → camera.position 과 동일
            return transform.position - player.position;
        }

        // 입장 직전 카메라 Y 유지 + 캐릭터 좌우 delta + 쉐이크
        Vector3 playerHorizontalDelta = new Vector3(player.position.x - _parallaxLockPlayerReference.x, 0f, 0f);
        Vector3 cameraShake = transform.position - _arenaFixedPosition;
        return _parallaxLockCameraReference + playerHorizontalDelta + cameraShake - player.position;
    }

    private Transform ResolveTarget()
    {
        if (target != null)
        {
            return target;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            return null;
        }

        target = playerObject.transform;
        _targetBody = target.GetComponent<Rigidbody2D>();
        return target;
    }

    private void LateUpdate()
    {
        if (_arenaLocked)
        {
            Vector3 lockedPosition = _arenaFixedPosition;
            if (_shakeTimer > 0f)
            {
                float normalized = _shakeDuration <= 0f ? 0f : _shakeTimer / _shakeDuration;
                Vector2 shake = Random.insideUnitCircle * (_shakeStrength * normalized);
                lockedPosition += new Vector3(shake.x, shake.y, 0f);
                _shakeTimer = Mathf.Max(0f, _shakeTimer - Time.unscaledDeltaTime);
            }

            transform.position = lockedPosition;
            return;
        }

        if (target == null)
        {
            if (ResolveTarget() == null)
            {
                return;
            }
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

        if (_shakeTimer > 0f)
        {
            float normalized = _shakeDuration <= 0f ? 0f : _shakeTimer / _shakeDuration;
            Vector2 shake = Random.insideUnitCircle * (_shakeStrength * normalized);
            currentPosition += new Vector3(shake.x, shake.y, 0f);
            _shakeTimer = Mathf.Max(0f, _shakeTimer - Time.unscaledDeltaTime);
        }

        transform.position = currentPosition;
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 snapped = target.position + offset;
        snapped.z = offset.z;
        transform.position = snapped;
        _currentLookAhead = 0f;
    }

    public void AddShake(float strength, float duration)
    {
        _shakeStrength = Mathf.Max(_shakeStrength, Mathf.Max(0f, strength));
        _shakeDuration = Mathf.Max(_shakeDuration, Mathf.Max(0.01f, duration));
        _shakeTimer = Mathf.Max(_shakeTimer, _shakeDuration);
    }
}
