using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 역할:
// - 플레이어 위치와 지형 센서를 보고 Blind Huntress 적의 행동을 고릅니다.
// - 실제 공격/대시는 Combat에 위임하고, 여기서는 추적과 상태 결정만 담당합니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BlindHuntressEnemyCombat))]
[RequireComponent(typeof(BlindHuntressEnemyInteraction))]
public class BlindHuntressEnemyBrain : MonoBehaviour
{
    [Header("References")]
    [Tooltip("좌우 반전과 애니메이션을 적용할 비주얼 루트입니다. 보통 Visual 자식 오브젝트를 넣습니다.")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("현재 바닥 위에 서 있는지 검사하는 전용 센서입니다. 발 바로 아래 자식 오브젝트를 넣습니다.")]
    [SerializeField] private Transform groundCheck;
    [Tooltip("앞쪽 벽을 감지하는 센서입니다. 벽이 있으면 대시/추적을 멈춥니다.")]
    [SerializeField] private Transform wallCheck;
    [Tooltip("앞쪽 발밑 바닥을 감지하는 센서입니다. 바닥이 없으면 낭떠러지로 판단합니다.")]
    [SerializeField] private Transform ledgeCheck;
    [Tooltip("특정 플레이어를 강제로 쫓게 하고 싶을 때 넣습니다. 비워두면 씬의 플레이어를 자동 탐색합니다.")]
    [SerializeField] private Transform targetOverride;

    [Header("Movement")]
    [Tooltip("바닥에서 플레이어를 추적할 때의 최대 좌우 속도입니다.")]
    [SerializeField] private float groundMoveSpeed = 4.2f;
    [Tooltip("공중에 뜬 상태에서 좌우로 보정하는 최대 속도입니다.")]
    [SerializeField] private float airMoveSpeed = 3.2f;
    [Tooltip("바닥에서 목표 속도까지 붙는 속도입니다. 높을수록 즉시 방향을 바꿉니다.")]
    [SerializeField] private float groundAcceleration = 34f;
    [Tooltip("공중에서 목표 속도까지 붙는 속도입니다.")]
    [SerializeField] private float airAcceleration = 22f;
    [Tooltip("플레이어와 이 거리보다 가까워지면 추적 이동을 멈추고 패턴 판단만 합니다.")]
    [SerializeField] private float stopDistance = 0.9f;

    [Header("Detection")]
    [Tooltip("플레이어를 인식하는 최대 거리입니다. 이 밖으로 나가면 추적을 멈춥니다.")]
    [SerializeField] private float detectionRange = 7.5f;
    [Tooltip("같은 층으로 볼 수 있는 높이 차 허용치입니다. 공격/대시 발동 조건에 같이 씁니다.")]
    [SerializeField] private float sameLevelTolerance = 0.7f;
    [Tooltip("기본 공격을 쓰기 시작하는 가로 거리입니다.")]
    [SerializeField] private float attackRange = 1.25f;
    [Tooltip("대시 공격을 쓰기 시작하는 최소 거리입니다. 너무 가까우면 기본 공격을 우선합니다.")]
    [SerializeField] private float dashAttackMinRange = 1.9f;
    [Tooltip("대시 공격을 쓰는 최대 거리입니다. 이보다 멀면 일반 대시나 달리기로 접근합니다.")]
    [SerializeField] private float dashAttackMaxRange = 4.1f;
    [Tooltip("일반 대시로 간격을 줄이기 시작하는 거리입니다.")]
    [SerializeField] private float dashApproachMinRange = 4.8f;
    [Tooltip("플레이어가 이 높이 이상 위에 있으면 점프 접근을 시도합니다.")]
    [SerializeField] private float jumpTriggerHeight = 1.35f;
    [Tooltip("위 공격을 허용하는 가로 거리입니다. 플레이어가 이 안쪽에 있어야 위로 벱니다.")]
    [SerializeField] private float upAttackXRange = 1.2f;
    [Tooltip("위 공격을 허용하는 최소 높이 차입니다.")]
    [SerializeField] private float upAttackYMin = 0.7f;
    [Tooltip("위 공격을 허용하는 최대 높이 차입니다. 너무 높으면 점프를 더 우선합니다.")]
    [SerializeField] private float upAttackYMax = 2.2f;

    [Header("Sensors")]
    [Tooltip("바닥/벽/낭떠러지 센서가 검사할 레이어입니다. 보통 Ground만 넣습니다.")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("바닥 감지 반지름입니다. 너무 작으면 접지 판정이 흔들리고, 너무 크면 공중에서도 접지로 잡힙니다.")]
    [SerializeField] private float groundCheckRadius = 0.12f;
    [Tooltip("벽 감지 반지름입니다. 너무 크면 벽을 과하게 피하고, 너무 작으면 박고 달립니다.")]
    [SerializeField] private float wallCheckRadius = 0.12f;
    [Tooltip("낭떠러지 감지 반지름입니다. 너무 작으면 떨어질 수 있습니다.")]
    [SerializeField] private float ledgeCheckRadius = 0.1f;

    [Header("Visual")]
    [Tooltip("원본 스프라이트의 좌우 방향이 반대로 보일 때 켭니다.")]
    [SerializeField] private bool invertVisualFacing = false;

    private Rigidbody2D _body;
    private BlindHuntressEnemyCombat _combat;
    private BlindHuntressEnemyInteraction _interaction;
    private SpriteRenderer _visualRenderer;
    private Transform _target;
    private PlayerInteraction _targetInteraction;
    private float _targetRefreshTimer;
    private float _facing = 1f;
    private bool _isGrounded;

    public float FacingDirection => _facing;
    public bool IsGroundedNow => _isGrounded;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _combat = GetComponent<BlindHuntressEnemyCombat>();
        _interaction = GetComponent<BlindHuntressEnemyInteraction>();

        if (groundLayer.value == 0)
        {
            groundLayer = LayerMask.GetMask("Ground");
        }

        if (visualRoot == null)
        {
            visualRoot = transform.Find("Visual");
        }

        if (visualRoot != null)
        {
            _visualRenderer = visualRoot.GetComponent<SpriteRenderer>();
        }

        _target = targetOverride;
        CacheTargetInteraction();
        ApplyFacingToVisual();
    }

    private void Update()
    {
        _targetRefreshTimer -= Time.deltaTime;
        if (_targetRefreshTimer > 0f && _target != null)
        {
            return;
        }

        RefreshTarget();
        _targetRefreshTimer = 0.4f;
    }

    private void FixedUpdate()
    {
        UpdateGrounded();

        if (_interaction != null && _interaction.IsDead)
        {
            ApplyMove(0f, Time.fixedDeltaTime);
            return;
        }

        if (_target == null || (_targetInteraction != null && _targetInteraction.IsDead))
        {
            ApplyMove(0f, Time.fixedDeltaTime);
            return;
        }

        Vector2 toTarget = _target.position - transform.position;
        float absX = Mathf.Abs(toTarget.x);
        float absY = Mathf.Abs(toTarget.y);

        if (toTarget.sqrMagnitude > detectionRange * detectionRange)
        {
            ApplyMove(0f, Time.fixedDeltaTime);
            return;
        }

        if (!_combat.IsBusy && absX > 0.05f)
        {
            _facing = Mathf.Sign(toTarget.x);
            ApplyFacingToVisual();
        }

        if (_combat.IsBusy)
        {
            return;
        }

        if (_isGrounded && ShouldUseUpAttack(absX, toTarget.y) && _combat.CanUseUpAttack)
        {
            _combat.TryStartUpAttack(_facing);
            return;
        }

        if (_isGrounded && ShouldUseAttack(absX, absY) && _combat.CanUseAttack)
        {
            _combat.TryStartAttack(_facing);
            return;
        }

        if (_isGrounded && ShouldUseDashAttack(absX, absY) && HasForwardRunway(_facing) && _combat.CanUseDashAttack)
        {
            _combat.TryStartDashAttack(_facing);
            return;
        }

        if (_isGrounded && ShouldUseJump(absX, toTarget.y) && _combat.CanUseJump)
        {
            _combat.TryStartJump(_facing);
            return;
        }

        if (_isGrounded && ShouldUseDash(absX, absY) && HasForwardRunway(_facing) && _combat.CanUseDash)
        {
            _combat.TryStartDash(_facing);
            return;
        }

        float moveDirection = absX > stopDistance ? Mathf.Sign(toTarget.x) : 0f;
        if (_isGrounded && moveDirection != 0f)
        {
            if (!HasGroundAhead(moveDirection) || IsWallBlocked(moveDirection))
            {
                moveDirection = 0f;
            }
        }

        ApplyMove(moveDirection, Time.fixedDeltaTime);
    }

    private void RefreshTarget()
    {
        if (targetOverride != null)
        {
            _target = targetOverride;
            CacheTargetInteraction();
            return;
        }

        SimplePlayerCombat combat = Object.FindFirstObjectByType<SimplePlayerCombat>();
        _target = combat != null ? combat.transform : null;
        CacheTargetInteraction();
    }

    private void CacheTargetInteraction()
    {
        _targetInteraction = _target != null ? _target.GetComponent<PlayerInteraction>() : null;
    }

    private void UpdateGrounded()
    {
        Vector2 center = GetGroundCheckPosition();
        _isGrounded = Physics2D.OverlapCircle(center, groundCheckRadius, groundLayer) != null;
    }

    private void ApplyMove(float direction, float dt)
    {
        if (_body == null)
        {
            return;
        }

        float targetSpeed = direction * (_isGrounded ? groundMoveSpeed : airMoveSpeed);
        float acceleration = _isGrounded ? groundAcceleration : airAcceleration;
        Vector2 velocity = _body.linearVelocity;
        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, acceleration * dt);
        _body.linearVelocity = velocity;
    }

    private bool ShouldUseAttack(float absX, float absY)
    {
        return absX <= attackRange && absY <= sameLevelTolerance;
    }

    private bool ShouldUseDashAttack(float absX, float absY)
    {
        return absY <= sameLevelTolerance && absX >= dashAttackMinRange && absX <= dashAttackMaxRange;
    }

    private bool ShouldUseDash(float absX, float absY)
    {
        return absY <= sameLevelTolerance && absX >= dashApproachMinRange;
    }

    private bool ShouldUseJump(float absX, float yDelta)
    {
        return yDelta >= jumpTriggerHeight && absX <= 2.8f;
    }

    private bool ShouldUseUpAttack(float absX, float yDelta)
    {
        return absX <= upAttackXRange && yDelta >= upAttackYMin && yDelta <= upAttackYMax;
    }

    private bool HasGroundAhead(float direction)
    {
        if (ledgeCheck == null)
        {
            return true;
        }

        Vector2 center = GetSensorPosition(ledgeCheck, direction);
        return Physics2D.OverlapCircle(center, ledgeCheckRadius, groundLayer) != null;
    }

    private bool IsWallBlocked(float direction)
    {
        if (wallCheck == null)
        {
            return false;
        }

        Vector2 center = GetSensorPosition(wallCheck, direction);
        return Physics2D.OverlapCircle(center, wallCheckRadius, groundLayer) != null;
    }

    private bool HasForwardRunway(float direction)
    {
        return HasGroundAhead(direction) && !IsWallBlocked(direction);
    }

    private Vector2 GetGroundCheckPosition()
    {
        return groundCheck != null ? groundCheck.position : transform.position;
    }

    private Vector2 GetSensorPosition(Transform sensor, float direction)
    {
        if (sensor == null)
        {
            return transform.position;
        }

        Vector3 local = sensor.localPosition;
        local.x = Mathf.Abs(local.x) * Mathf.Sign(direction == 0f ? 1f : direction);
        Transform parent = sensor.parent != null ? sensor.parent : transform;
        return parent.TransformPoint(local);
    }

    private void ApplyFacingToVisual()
    {
        if (_visualRenderer == null && visualRoot != null)
        {
            _visualRenderer = visualRoot.GetComponent<SpriteRenderer>();
        }

        if (_visualRenderer != null)
        {
            bool faceRight = _facing >= 0f;
            _visualRenderer.flipX = invertVisualFacing ? faceRight : !faceRight;
            return;
        }

        if (visualRoot == null)
        {
            return;
        }

        Vector3 scale = visualRoot.localScale;
        scale.x = Mathf.Abs(scale.x) * (_facing >= 0f ? 1f : -1f);
        visualRoot.localScale = scale;
    }

    private void OnDrawGizmos()
    {
        if (!ShouldDrawGizmos())
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(GetGroundCheckPosition(), groundCheckRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetSensorPosition(wallCheck, _facing), wallCheckRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(GetSensorPosition(ledgeCheck, _facing), ledgeCheckRadius);
    }

    private bool ShouldDrawGizmos()
    {
#if UNITY_EDITOR
        Transform selected = Selection.activeTransform;
        return selected != null && (selected == transform || selected.IsChildOf(transform));
#else
        return false;
#endif
    }
}
