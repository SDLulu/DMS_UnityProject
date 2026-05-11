using UnityEngine;

// 역할:
// - 플레이어 이동, 점프, 대시, 구르기, 지면 판정과 방향 전환을 관리합니다.
// - 입력은 GameInput에서 읽고, 실제 물리 상태 변화만 이 파일에서 만듭니다.
//
// 구조 포인트:
// - 전투와 표현에서 분리된 플레이어 이동 계층의 중심 파일입니다.

[RequireComponent(typeof(Rigidbody2D))]
public class SimplePlayerController : MonoBehaviour
{
    private const string DefaultSensorName = "Sensors";
    private const string LegacyGroundCheckName = "GroundCheck";
    private const string DefaultVisualName = "Visual";
    private const string LegacyVisualName = "RobotMaidVisual";
    private const string PlayerLayerName = "Player";
    private const string EnemyLayerName = "Enemy";
    private const float JumpGroundIgnoreDuration = 0.08f;
    private const float UpwardUngroundedVelocity = 0.05f;

    private enum PlayerActionState
    {
        Normal,
        Crouch,
        Dash,
        Roll
    }

    [Header("Movement")]
    [SerializeField] private float groundMoveSpeed = 6.25f;
    [SerializeField] private float airMoveSpeed = 6f;
    [SerializeField] private float groundAcceleration = 72f;
    [SerializeField] private float groundDeceleration = 84f;
    [SerializeField] private float airAcceleration = 54f;
    [SerializeField] private float airDeceleration = 42f;
    [SerializeField] private float turnaroundAccelerationMultiplier = 1.65f;
    [SerializeField] private float jumpForce = 9.35f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float fallGravityMultiplier = 2.85f;
    [SerializeField] private float jumpCutGravityMultiplier = 3.25f;
    [SerializeField] private float apexGravityMultiplier = 0.92f;
    [SerializeField] private float apexMoveSpeedMultiplier = 1.08f;
    [SerializeField] private float apexThreshold = 1.2f;
    [SerializeField] private float maxFallSpeed = 22f;
    [SerializeField] private float groundedStickForce = 1.5f;
    [SerializeField] private float baseGravityScale = 3f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.14f;
    [SerializeField] private float dashCooldown = 2f;

    [Header("Roll")]
    [SerializeField] private float rollSpeed = 8.5f;
    [SerializeField] private float rollDuration = 0.36f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.18f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private bool invertVisualFacing = false;

    private Rigidbody2D _body;
    private float _moveInput;
    private bool _downHeld;
    private bool _isGrounded;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _jumpHeld;
    private float _facing = 1f;
    private float _jumpGroundIgnoreTimer;
    private SpriteRenderer _visualRenderer;
    private Collider2D[] _selfColliders = System.Array.Empty<Collider2D>();

    private PlayerActionState _actionState;
    private float _actionTimer;
    private float _actionDirection = 1f;
    private float _dashCooldownTimer;
    private bool _dashQueued;
    private bool _rollQueued;
    private float _queuedRollDirection;
    private bool _useExternalFacing;

    public Vector2 CurrentVelocity => _body != null ? _body.linearVelocity : Vector2.zero;
    public bool IsGroundedNow => _isGrounded;
    public float FacingDirection => _facing;
    public Transform VisualRoot => visualRoot;
    public Transform GroundSensor => groundCheck;
    public bool IsDashing => _actionState == PlayerActionState.Dash;
    public bool IsRolling => _actionState == PlayerActionState.Roll;
    public bool IsCrouching => _actionState == PlayerActionState.Crouch;
    public bool IsActionLocked => IsDashing || IsRolling;
    public float DashCooldownRemaining => _dashCooldownTimer;

    public void SetExternalFacing(float direction, bool active)
    {
        _useExternalFacing = active;
        if (!active || Mathf.Abs(direction) <= 0.01f)
        {
            return;
        }

        _facing = Mathf.Sign(direction);
        ApplyFacingToVisual();
    }

    public PlayerMovementConfig CreateConfigSnapshot()
    {
        return new PlayerMovementConfig
        {
            groundMoveSpeed = groundMoveSpeed,
            airMoveSpeed = airMoveSpeed,
            groundAcceleration = groundAcceleration,
            groundDeceleration = groundDeceleration,
            airAcceleration = airAcceleration,
            airDeceleration = airDeceleration,
            turnaroundAccelerationMultiplier = turnaroundAccelerationMultiplier,
            jumpForce = jumpForce,
            coyoteTime = coyoteTime,
            jumpBufferTime = jumpBufferTime,
            fallGravityMultiplier = fallGravityMultiplier,
            jumpCutGravityMultiplier = jumpCutGravityMultiplier,
            apexGravityMultiplier = apexGravityMultiplier,
            apexMoveSpeedMultiplier = apexMoveSpeedMultiplier,
            apexThreshold = apexThreshold,
            maxFallSpeed = maxFallSpeed,
            groundedStickForce = groundedStickForce,
            gravityScale = baseGravityScale,
            groundCheckRadius = groundCheckRadius,
            dashSpeed = dashSpeed,
            dashDuration = dashDuration,
            dashCooldown = dashCooldown,
            rollSpeed = rollSpeed,
            rollDuration = rollDuration
        };
    }

    public void ApplyConfig(PlayerMovementConfig config)
    {
        config = PlayerConfigLoader.Sanitize(new PlayerConfig { movement = config }).movement;
        groundMoveSpeed = config.groundMoveSpeed;
        airMoveSpeed = config.airMoveSpeed;
        groundAcceleration = config.groundAcceleration;
        groundDeceleration = config.groundDeceleration;
        airAcceleration = config.airAcceleration;
        airDeceleration = config.airDeceleration;
        turnaroundAccelerationMultiplier = config.turnaroundAccelerationMultiplier;
        jumpForce = config.jumpForce;
        coyoteTime = config.coyoteTime;
        jumpBufferTime = config.jumpBufferTime;
        fallGravityMultiplier = config.fallGravityMultiplier;
        jumpCutGravityMultiplier = config.jumpCutGravityMultiplier;
        apexGravityMultiplier = config.apexGravityMultiplier;
        apexMoveSpeedMultiplier = config.apexMoveSpeedMultiplier;
        apexThreshold = config.apexThreshold;
        maxFallSpeed = config.maxFallSpeed;
        groundedStickForce = config.groundedStickForce;
        baseGravityScale = config.gravityScale;
        groundCheckRadius = config.groundCheckRadius;
        dashSpeed = config.dashSpeed;
        dashDuration = config.dashDuration;
        dashCooldown = config.dashCooldown;
        rollSpeed = config.rollSpeed;
        rollDuration = config.rollDuration;

        if (_body != null)
        {
            _body.gravityScale = baseGravityScale;
        }
    }

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _selfColliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
        gameObject.tag = "Player";
        EnsurePlayerEnemyCollision();
        _body.freezeRotation = true;

        if (_body.gravityScale == 0f)
        {
            _body.gravityScale = baseGravityScale;
        }

        if (groundCheck == null)
        {
            Transform existingGroundCheck = transform.Find(DefaultSensorName) ?? transform.Find(LegacyGroundCheckName);
            if (existingGroundCheck != null)
            {
                groundCheck = existingGroundCheck;
            }
            else
            {
                Debug.LogWarning("Player prefab is missing Sensors transform. Fix the prefab instead of relying on runtime fallback.", this);
            }
        }

        if (groundLayer.value == 0)
        {
            groundLayer = LayerMask.GetMask("Ground");
        }

        if (visualRoot == null)
        {
            Transform existingVisual = transform.Find(DefaultVisualName) ?? transform.Find(LegacyVisualName);
            if (existingVisual != null)
            {
                visualRoot = existingVisual;
            }
            else
            {
                Debug.LogWarning("Player prefab is missing Visual transform. Fix the prefab instead of relying on runtime fallback.", this);
            }
        }

        if (_visualRenderer == null && visualRoot != null)
        {
            _visualRenderer = visualRoot.GetComponent<SpriteRenderer>();
        }

        ApplyFacingToVisual();
    }

    private void Update()
    {
        // 입력은 프레임 단위로만 읽고, 실제 물리 적용은 FixedUpdate에서 소비합니다.
        _moveInput = ReadHorizontal();
        _downHeld = ReadDownHeld();

        // 점프는 즉시 실행하지 않고 버퍼에 적재해 코요테 타임과 함께 판정합니다.
        if (!IsActionLocked && !_downHeld && ReadJumpPressed())
        {
            _jumpBufferTimer = jumpBufferTime;
        }

        _jumpHeld = !IsActionLocked && ReadJumpHeld();

        // 특수 액션은 예약만 해두고, 실제 실행 여부는 물리 틱에서 확정합니다.
        if (!IsActionLocked && ReadDashPressed())
        {
            _dashQueued = true;
        }

        if (!IsActionLocked && _isGrounded && _downHeld && ReadRollPressed(out float rollDirection))
        {
            _rollQueued = true;
            _queuedRollDirection = rollDirection;
        }

        if (_jumpBufferTimer > 0f)
        {
            _jumpBufferTimer -= Time.unscaledDeltaTime;
        }
    }

    private void FixedUpdate()
    {
        // 이번 틱에서 사용할 쿨다운과 지면 상태를 먼저 최신값으로 맞춥니다.
        float realDt = Time.fixedUnscaledDeltaTime;
        if (_dashCooldownTimer > 0f)
        {
            _dashCooldownTimer = Mathf.Max(0f, _dashCooldownTimer - realDt);
        }

        bool detectedGround = IsGrounded();
        if (_jumpGroundIgnoreTimer > 0f)
        {
            _jumpGroundIgnoreTimer = Mathf.Max(0f, _jumpGroundIgnoreTimer - realDt);
        }

        bool risingFromJump = _body.linearVelocity.y > UpwardUngroundedVelocity;
        _isGrounded = detectedGround && _jumpGroundIgnoreTimer <= 0f && !risingFromJump;
        _coyoteTimer = _isGrounded ? coyoteTime : Mathf.Max(0f, _coyoteTimer - realDt);

        // 대시/롤은 일반 이동보다 우선하며, 실행 중에는 해당 루틴만 계속 유지합니다.
        if (IsDashing)
        {
            TickDash();
            return;
        }

        if (IsRolling)
        {
            TickRoll();
            return;
        }

        // 예약된 액션은 조건이 맞는 첫 물리 틱에서 시작합니다.
        if (_rollQueued)
        {
            _rollQueued = false;
            if (_isGrounded && Mathf.Abs(_queuedRollDirection) > 0.01f)
            {
                StartRoll(_queuedRollDirection);
                return;
            }
        }

        if (_dashQueued)
        {
            _dashQueued = false;
            if (_dashCooldownTimer <= 0f)
            {
                StartDash(ResolveActionDirection());
                return;
            }
        }

        // 아래 입력을 누른 채 착지했으면 이동 대신 웅크리기 상태를 유지합니다.
        if (_isGrounded && _downHeld)
        {
            ApplyCrouch();
            return;
        }

        _actionState = PlayerActionState.Normal;
        TickNormalMovement(realDt);
    }

    private void TickNormalMovement(float realDt)
    {
        // 목표 속도는 지상/공중 상태와 점프 정점 보정값을 반영해 계산합니다.
        float targetSpeed = _moveInput * (_isGrounded ? groundMoveSpeed : airMoveSpeed);
        bool nearApex = !_isGrounded && Mathf.Abs(_body.linearVelocity.y) <= apexThreshold;
        if (nearApex)
        {
            targetSpeed *= apexMoveSpeedMultiplier;
        }

        float currentSpeed = _body.linearVelocity.x;
        float acceleration = Mathf.Abs(targetSpeed) > 0.01f
            ? (_isGrounded ? groundAcceleration : airAcceleration)
            : (_isGrounded ? groundDeceleration : airDeceleration);
        bool reversingDirection = Mathf.Abs(currentSpeed) > 0.05f && Mathf.Abs(targetSpeed) > 0.05f && Mathf.Sign(currentSpeed) != Mathf.Sign(targetSpeed);
        if (reversingDirection)
        {
            acceleration *= turnaroundAccelerationMultiplier;
        }

        // 슬로우 모션 중에도 플레이어 가속/감속 체감은 유지해야 하므로 실제 시간 기준으로 보간합니다.
        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * realDt);

        // 수평 속도와 점프 발동을 먼저 확정하고, 중력과 낙하 제한은 마지막에 적용합니다.
        Vector2 velocity = _body.linearVelocity;
        velocity.x = newSpeed;

        if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
        {
            velocity.y = jumpForce;
            _isGrounded = false;
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
            _jumpGroundIgnoreTimer = JumpGroundIgnoreDuration;
        }

        float gravityScale = baseGravityScale;
        if (velocity.y < -0.01f)
        {
            gravityScale *= fallGravityMultiplier;
        }
        else if (!_jumpHeld && velocity.y > 0.01f)
        {
            gravityScale *= jumpCutGravityMultiplier;
        }
        else if (nearApex)
        {
            gravityScale *= apexGravityMultiplier;
        }

        _body.gravityScale = gravityScale;
        if (_isGrounded && velocity.y < 0f)
        {
            velocity.y = -groundedStickForce;
        }
        velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        _body.linearVelocity = velocity;

        // 외부 시스템이 방향을 강제하지 않을 때만 이동 입력으로 비주얼 방향을 갱신합니다.
        if (!_useExternalFacing && Mathf.Abs(_moveInput) > 0.01f)
        {
            _facing = Mathf.Sign(_moveInput);
            ApplyFacingToVisual();
        }
    }

    private void ApplyCrouch()
    {
        // 웅크리기 중에는 수평 이동을 끊고 지면에 붙은 상태를 유지합니다.
        _actionState = PlayerActionState.Crouch;
        _jumpBufferTimer = 0f;
        _body.gravityScale = baseGravityScale;

        Vector2 velocity = _body.linearVelocity;
        velocity.x = 0f;
        if (_isGrounded && velocity.y < 0f)
        {
            velocity.y = -groundedStickForce;
        }

        velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        _body.linearVelocity = velocity;
    }

    private void StartDash(float direction)
    {
        // 대시 시작 시점에 방향과 쿨다운을 고정하고 점프 버퍼를 비웁니다.
        _actionState = PlayerActionState.Dash;
        _actionTimer = dashDuration;
        _actionDirection = direction;
        _dashCooldownTimer = dashCooldown;
        _jumpBufferTimer = 0f;
        _coyoteTimer = 0f;
        _facing = direction;
        ApplyFacingToVisual();
    }

    private void TickDash()
    {
        // 대시는 중력을 끄고 정해진 시간 동안 수평 속도를 강제로 유지합니다.
        _actionTimer = Mathf.Max(0f, _actionTimer - Time.fixedUnscaledDeltaTime);
        _body.gravityScale = 0f;
        _body.linearVelocity = new Vector2(_actionDirection * dashSpeed, 0f);

        if (_actionTimer <= 0f)
        {
            FinishAction();
        }
    }

    private void StartRoll(float direction)
    {
        // 롤은 대시와 비슷하게 시작 순간에 방향을 고정하지만 중력은 유지합니다.
        _actionState = PlayerActionState.Roll;
        _actionTimer = rollDuration;
        _actionDirection = direction;
        _jumpBufferTimer = 0f;
        _coyoteTimer = 0f;
        _facing = direction;
        ApplyFacingToVisual();
    }

    private void TickRoll()
    {
        // 구르기 중에는 수평 속도를 강제로 유지하고 낙하 속도만 안전 범위로 제한합니다.
        _actionTimer = Mathf.Max(0f, _actionTimer - Time.fixedUnscaledDeltaTime);
        _body.gravityScale = baseGravityScale;

        Vector2 velocity = _body.linearVelocity;
        velocity.x = _actionDirection * rollSpeed;
        if (_isGrounded && velocity.y < 0f)
        {
            velocity.y = -groundedStickForce;
        }

        velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        _body.linearVelocity = velocity;

        if (_actionTimer <= 0f)
        {
            FinishAction();
        }
    }

    private void FinishAction()
    {
        // 액션 종료 후에는 현재 입력 상태를 다시 읽어 일반 이동 또는 웅크리기로 복귀합니다.
        _actionState = _isGrounded && _downHeld ? PlayerActionState.Crouch : PlayerActionState.Normal;
        _actionTimer = 0f;
        _body.gravityScale = baseGravityScale;
    }

    private float ResolveActionDirection()
    {
        if (Mathf.Abs(_moveInput) > 0.01f)
        {
            return Mathf.Sign(_moveInput);
        }

        return _facing >= 0f ? 1f : -1f;
    }

    private void ApplyFacingToVisual()
    {
        if (_visualRenderer == null && visualRoot != null)
        {
            _visualRenderer = visualRoot.GetComponent<SpriteRenderer>();
        }

        if (visualRoot == null)
        {
            return;
        }

        Vector3 scale = visualRoot.localScale;
        float facingSign = _facing >= 0f ? 1f : -1f;
        if (invertVisualFacing)
        {
            facingSign *= -1f;
        }

        scale.x = Mathf.Abs(scale.x) * facingSign;
        visualRoot.localScale = scale;

        if (_visualRenderer != null)
        {
            _visualRenderer.flipX = false;
        }
    }

    private bool IsGrounded()
    {
        if (groundCheck == null)
        {
            return false;
        }

        // 오버랩 결과 중 자기 자신의 콜라이더만 걸러서 진짜 지면 접촉만 판정합니다.
        Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            bool isSelfCollider = false;
            for (int selfIndex = 0; selfIndex < _selfColliders.Length; selfIndex++)
            {
                if (_selfColliders[selfIndex] == hit)
                {
                    isSelfCollider = true;
                    break;
                }
            }

            if (!isSelfCollider)
            {
                return true;
            }
        }

        return false;
    }

    private float ReadHorizontal()
    {
        return Mathf.Clamp(GameInput.Instance.Move.x, -1f, 1f);
    }

    private bool ReadJumpPressed()
    {
        return GameInput.Instance.JumpPressed;
    }

    private bool ReadJumpHeld()
    {
        return GameInput.Instance.JumpHeld;
    }

    private bool ReadDownHeld()
    {
        return GameInput.Instance.Move.y < -0.5f;
    }

    private bool ReadDownPressed()
    {
        return GameInput.Instance.MoveTriggeredThisFrame && GameInput.Instance.Move.y < -0.5f;
    }

    private bool ReadDashPressed()
    {
        return GameInput.Instance.DashPressed;
    }

    private bool ReadRollPressed(out float direction)
    {
        direction = 0f;
        Vector2 move = GameInput.Instance.Move;
        bool leftHeld = move.x < -0.5f;
        bool rightHeld = move.x > 0.5f;
        bool horizontalTriggered = GameInput.Instance.MoveTriggeredThisFrame && (leftHeld || rightHeld);

        if (horizontalTriggered && leftHeld != rightHeld)
        {
            direction = leftHeld ? -1f : 1f;
            return true;
        }

        if (ReadDownPressed() && leftHeld != rightHeld)
        {
            direction = leftHeld ? -1f : 1f;
            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

    private static void EnsurePlayerEnemyCollision()
    {
        int playerLayer = LayerMask.NameToLayer(PlayerLayerName);
        int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
        if (playerLayer >= 0 && enemyLayer >= 0)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        }
    }
}
