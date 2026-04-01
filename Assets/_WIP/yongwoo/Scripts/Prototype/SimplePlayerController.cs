using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class SimplePlayerController : MonoBehaviour
{
    private const string DefaultSensorName = "Sensors";
    private const string LegacyGroundCheckName = "GroundCheck";
    private const string DefaultVisualName = "Visual";
    private const string LegacyVisualName = "RobotMaidVisual";
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
        config = PrototypePlayerConfigLoader.Sanitize(new PrototypePlayerConfig { movement = config }).movement;
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
        _moveInput = ReadHorizontal();
        _downHeld = ReadDownHeld();

        if (!IsActionLocked && !_downHeld && ReadJumpPressed())
        {
            _jumpBufferTimer = jumpBufferTime;
        }

        _jumpHeld = !IsActionLocked && ReadJumpHeld();

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
            _jumpBufferTimer -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (_dashCooldownTimer > 0f)
        {
            _dashCooldownTimer = Mathf.Max(0f, _dashCooldownTimer - Time.fixedDeltaTime);
        }

        bool detectedGround = IsGrounded();
        if (_jumpGroundIgnoreTimer > 0f)
        {
            _jumpGroundIgnoreTimer = Mathf.Max(0f, _jumpGroundIgnoreTimer - Time.fixedDeltaTime);
        }

        bool risingFromJump = _body.linearVelocity.y > UpwardUngroundedVelocity;
        _isGrounded = detectedGround && _jumpGroundIgnoreTimer <= 0f && !risingFromJump;
        _coyoteTimer = _isGrounded ? coyoteTime : Mathf.Max(0f, _coyoteTimer - Time.fixedDeltaTime);

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

        if (_isGrounded && _downHeld)
        {
            ApplyCrouch();
            return;
        }

        _actionState = PlayerActionState.Normal;
        TickNormalMovement();
    }

    private void TickNormalMovement()
    {
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

        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);

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

        if (Mathf.Abs(_moveInput) > 0.01f)
        {
            _facing = Mathf.Sign(_moveInput);
            ApplyFacingToVisual();
        }
    }

    private void ApplyCrouch()
    {
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
        _actionTimer = Mathf.Max(0f, _actionTimer - Time.fixedDeltaTime);
        _body.gravityScale = 0f;
        _body.linearVelocity = new Vector2(_actionDirection * dashSpeed, 0f);

        if (_actionTimer <= 0f)
        {
            FinishAction();
        }
    }

    private void StartRoll(float direction)
    {
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
        _actionTimer = Mathf.Max(0f, _actionTimer - Time.fixedDeltaTime);
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

    private bool IsGrounded()
    {
        if (groundCheck == null)
        {
            return false;
        }

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
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            float value = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                value -= 1f;
            }
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                value += 1f;
            }
            return value;
        }
#endif
        return Input.GetAxisRaw("Horizontal");
    }

    private bool ReadJumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.spaceKey.wasPressedThisFrame
                || Keyboard.current.wKey.wasPressedThisFrame
                || Keyboard.current.upArrowKey.wasPressedThisFrame;
        }
#endif
        return Input.GetButtonDown("Jump");
    }

    private bool ReadJumpHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.spaceKey.isPressed
                || Keyboard.current.wKey.isPressed
                || Keyboard.current.upArrowKey.isPressed;
        }
#endif
        return Input.GetButton("Jump");
    }

    private bool ReadDownHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed;
        }
#endif
        return Input.GetKey(KeyCode.S)
            || Input.GetKey(KeyCode.DownArrow)
            || Input.GetAxisRaw("Vertical") < -0.5f;
    }

    private bool ReadDownPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame;
        }
#endif
        return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
    }

    private bool ReadDashPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame;
        }
#endif
        return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
    }

    private bool ReadRollPressed(out float direction)
    {
        direction = 0f;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            bool leftHeld = Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed;
            bool rightHeld = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed;
            bool leftPressed = Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame;
            bool rightPressed = Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame;
            if (leftPressed != rightPressed)
            {
                direction = leftPressed ? -1f : 1f;
                return true;
            }

            if (ReadDownPressed() && leftHeld != rightHeld)
            {
                direction = leftHeld ? -1f : 1f;
                return true;
            }

            return false;
        }
#endif

        bool fallbackLeftHeld = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool fallbackRightHeld = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        bool fallbackLeftPressed = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
        bool fallbackRightPressed = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
        if (fallbackLeftPressed != fallbackRightPressed)
        {
            direction = fallbackLeftPressed ? -1f : 1f;
            return true;
        }

        if (ReadDownPressed() && fallbackLeftHeld != fallbackRightHeld)
        {
            direction = fallbackLeftHeld ? -1f : 1f;
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
}
