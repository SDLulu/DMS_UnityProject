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

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.18f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private bool invertVisualFacing = false;

    private Rigidbody2D _body;
    private float _moveInput;
    private bool _isGrounded;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _jumpHeld;
    private float _facing = 1f;
    private float _jumpGroundIgnoreTimer;
    private SpriteRenderer _visualRenderer;
    private Collider2D[] _selfColliders = System.Array.Empty<Collider2D>();

    public Vector2 CurrentVelocity => _body != null ? _body.linearVelocity : Vector2.zero;
    public bool IsGroundedNow => _isGrounded;
    public float FacingDirection => _facing;
    public Transform VisualRoot => visualRoot;
    public Transform GroundSensor => groundCheck;

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
            groundCheckRadius = groundCheckRadius
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
            groundLayer = LayerMask.GetMask("Default");
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

        if (ReadJumpPressed())
        {
            _jumpBufferTimer = jumpBufferTime;
        }

        _jumpHeld = ReadJumpHeld();

        if (_jumpBufferTimer > 0f)
        {
            _jumpBufferTimer -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        bool detectedGround = IsGrounded();
        if (_jumpGroundIgnoreTimer > 0f)
        {
            _jumpGroundIgnoreTimer = Mathf.Max(0f, _jumpGroundIgnoreTimer - Time.fixedDeltaTime);
        }

        bool risingFromJump = _body.linearVelocity.y > UpwardUngroundedVelocity;
        _isGrounded = detectedGround && _jumpGroundIgnoreTimer <= 0f && !risingFromJump;
        _coyoteTimer = _isGrounded ? coyoteTime : Mathf.Max(0f, _coyoteTimer - Time.fixedDeltaTime);

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
