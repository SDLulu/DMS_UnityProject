using UnityEngine;

// 역할:
// - 플레이어 손(Hand) 트랜스폼을 마우스 방향으로 회전시킵니다.
// - 이 오브젝트의 자식에 무기 스프라이트를 배치하면 자연스럽게 따라 회전합니다.
// - 무기 스프라이트의 로컬 위치로 손잡이(그립) 위치를 조절합니다.

[DisallowMultipleComponent]
public class PlayerHand : MonoBehaviour
{
    [Header("Pivot")]
    [Tooltip("Hand가 따라갈 기준점입니다. 플레이어 자식으로 빈 오브젝트를 놓고 연결하세요.")]
    [SerializeField] private Transform pivotTarget;

    [Header("Rotation")]
    [Tooltip("무기 스프라이트의 기본 각도입니다. 오른쪽 45도를 바라보는 아이콘이면 45로 설정하세요.")]
    [SerializeField] private float spriteAngleOffset = 45f;
    [Tooltip("회전 속도입니다. 높을수록 즉시 따라갑니다. 0이면 즉시 스냅.")]
    [SerializeField] private float rotationSpeed = 0f;

    [Header("Flip")]
    [Tooltip("마우스가 왼쪽에 있을 때 아이템을 뒤집습니다.")]
    [SerializeField] private bool flipWhenLeft = true;

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;

    private Camera _mainCamera;
    private SimplePlayerController _controller;
    private float _currentAngle;

    /// <summary>현재 손이 가리키는 월드 방향입니다.</summary>
    public Vector2 AimDirection { get; private set; } = Vector2.right;

    /// <summary>마우스가 플레이어 왼쪽에 있으면 true입니다.</summary>
    public bool IsAimingLeft { get; private set; }

    private void Awake()
    {
        _mainCamera = Camera.main;
        _controller = GetComponentInParent<SimplePlayerController>();
    }

    private void LateUpdate()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;
        }

        // pivotTarget 위치로 Hand를 이동
        if (pivotTarget != null)
        {
            transform.position = pivotTarget.position;
        }

        // 마우스 월드 좌표
        if (!GameInput.Instance.TryGetPointerScreenPosition(out Vector2 screenPos)) return;
        Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_mainCamera.transform.position.z));

        // 방향 계산
        Vector2 dir = (Vector2)mouseWorld - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        AimDirection = dir.normalized;
        IsAimingLeft = AimDirection.x < 0f;

        // 좌우 반전 먼저 결정 (각도 보정에 영향)
        bool flipped = flipWhenLeft && IsAimingLeft;
        if (flipWhenLeft)
        {
            Vector3 scale = transform.localScale;
            scale.y = flipped ? -Mathf.Abs(scale.y) : Mathf.Abs(scale.y);
            transform.localScale = scale;
        }

        // 목표 각도 (flip 시 스프라이트 각도가 반전되므로 보정도 반전)
        float aimAngle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;
        float targetAngle = flipped ? aimAngle + spriteAngleOffset : aimAngle - spriteAngleOffset;

        // 회전 적용 (unscaled — 슬로우 중에도 조준은 즉시 반응)
        if (rotationSpeed <= 0f)
        {
            _currentAngle = targetAngle;
        }
        else
        {
            _currentAngle = Mathf.MoveTowardsAngle(_currentAngle, targetAngle, rotationSpeed * Time.unscaledDeltaTime);
        }
        transform.rotation = Quaternion.Euler(0f, 0f, _currentAngle);

        // 플레이어 비주얼 방향을 에이밍에 맞춤
        if (_controller != null)
        {
            _controller.SetExternalFacing(AimDirection.x, true);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
        Vector3 pos = transform.position;
        Gizmos.DrawLine(pos, pos + (Vector3)(AimDirection * 1.5f));
        Gizmos.DrawWireSphere(pos, 0.08f);

        if (pivotTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pivotTarget.position, 0.06f);
        }
    }
}
