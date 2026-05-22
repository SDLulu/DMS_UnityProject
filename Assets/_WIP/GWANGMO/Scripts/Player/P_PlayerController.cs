using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class P_PlayerController : MonoBehaviour
{
    private const string PlayerActionMapName = "Player";
    private const string MoveActionName = "Move";
    private const string JumpActionName = "Jump";
    private const string RunToggleActionName = "RunToggle";
    private const string AttackActionName = "Attack";
    private const string FrontDashActionName = "FrontDash";
    private const string EvadeActionName = "BackDash";
    private const string IaidoAttackActionName = "IaidoAttack";
    private const string InteractActionName = "Interact";
    private const string WallGrabActionName = "Wall_Grab";
    private const string PlayerLayerName = "Player";
    private const string GroundLayerName = "Ground";
    private const string VisualName = "Visual";
    private const float JumpGroundIgnoreDuration = 0.08f;
    private const float UpwardUngroundedVelocity = 0.05f;
    private const float GroundContactVerticalMargin = 0.05f;
    private const float MinGroundContactNormalY = 0.55f;

    private enum PlayerActionState
    {
        Normal,
        FrontDash,
        BackDash,
        IaidoDash,
        WallGrab
    }

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float groundAcceleration = 72f;
    [SerializeField] private float groundDeceleration = 84f;
    [SerializeField] private float airAcceleration = 54f;
    [SerializeField] private float airDeceleration = 42f;
    [SerializeField] private float turnaroundAccelerationMultiplier = 1.65f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 9f;
    [SerializeField] private int extraAirJumps;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float fallGravityMultiplier = 2.35f;
    [SerializeField] private float jumpCutGravityMultiplier = 2.9f;
    [SerializeField] private float apexGravityMultiplier = 0.92f;
    [SerializeField] private float apexMoveSpeedMultiplier = 1.08f;
    [SerializeField] private float apexThreshold = 1.2f;
    [SerializeField] private float maxFallSpeed = 18f;
    [SerializeField] private float groundedStickForce = 1.5f;
    [SerializeField] private float baseGravityScale = 3f;

    [Header("Front Dash")]
    [SerializeField] private float frontDashSpeed = 14f;
    [SerializeField] private float frontDashDuration = 0.15f;
    [SerializeField] private float frontDashCooldown = 0.5f;

    [Header("Back Dash")]
    [SerializeField] private float backDashSpeed = 10f;
    [SerializeField] private float backDashDuration = 0.18f;
    [SerializeField] private float backDashCooldown = 0.5f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Wall Grab")]
    [SerializeField] private Transform wallCheckLeft;
    [SerializeField] private Transform wallCheckRight;
    [SerializeField] private float wallCheckRadius = 0.15f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallSlideSpeed = -1.5f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(7f, 9f);
    [SerializeField] private string wallGrabAnimationName = "Wall_Grab";

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;

    [Header("Animation States")]
    [SerializeField] private string idleAnimationName = "Idle";
    [SerializeField] private string walkAnimationName = "Blade_Walk";
    [SerializeField] private string runAnimationName = "Blade_Run";
    [SerializeField] private string jumpAnimationName = "Jump";
    [SerializeField] private string fallAnimationName = "Fall";
    [SerializeField] private string frontDashAnimationName = "Front_Dash";
    [SerializeField] private string backDashAnimationName = "Back_Dash";

    [Header("Debug")]
    [SerializeField] private bool debugGroundCheck;
    [SerializeField] private bool debugWallCheck;

    private readonly ContactPoint2D[] groundContacts = new ContactPoint2D[8];
    private Rigidbody2D body;
    private CapsuleCollider2D bodyCollider;
    private Animator animator;
    private Collider2D[] selfColliders;
    private InputActionMap playerActionMap;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction runToggleAction;
    private InputAction attackAction;
    private InputAction frontDashAction;
    private InputAction evadeAction;
    private InputAction iaidoAttackAction;
    private InputAction interactAction;
    private InputAction wallGrabAction;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool wallGrabPressed;
    private bool touchingLeftWall;
    private bool touchingRightWall;
    private bool wasTouchingLeftWall;
    private bool wasTouchingRightWall;
    private bool runMode;
    private bool isRunning;
    private bool jumpHeld;
    private bool wasGrounded;
    private int airJumpsRemaining;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float jumpGroundIgnoreTimer;
    private Vector2 dashDirection = Vector2.right;
    private float activeDashSpeed;
    private float dashTimer;
    private float dashDistanceRemaining;
    private float frontDashCooldownTimer;
    private float backDashCooldownTimer;
    private float facing = 1f;
    private int wallDirection;
    private PlayerActionState actionState;
    private bool hasMoveSpeedParameter;
    private bool hasIsRunningParameter;
    private bool hasIsGroundedParameter;
    private bool hasIsDashingParameter;
    private bool hasVerticalSpeedParameter;

    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsDashingHash = Animator.StringToHash("IsDashing");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");

    public Vector2 CurrentVelocity => body != null ? body.linearVelocity : Vector2.zero;
    public bool IsGroundedNow => isGrounded;
    public bool IsDashing => actionState == PlayerActionState.FrontDash ||
        actionState == PlayerActionState.BackDash ||
        actionState == PlayerActionState.IaidoDash;
    public bool IsWallGrabbing => actionState == PlayerActionState.WallGrab;
    public bool IsRunning => isRunning;
    public bool IsActionLocked => IsDashing || IsWallGrabbing;
    public bool InteractPressedThisFrame { get; private set; }
    public Transform VisualRoot => visualRoot;
    public Transform GroundSensor => groundCheck;
    public float FacingDirection => facing;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<CapsuleCollider2D>();
        animator = GetComponent<Animator>();
        selfColliders = GetComponentsInChildren<Collider2D>(includeInactive: true);

        EnsurePlayerIdentity();
        ResolveInputActions();
        CacheAnimatorParameters();

        body.freezeRotation = true;
        body.gravityScale = baseGravityScale;

        if (groundLayer.value == 0)
        {
            groundLayer = LayerMask.GetMask(GroundLayerName);
        }

        if (wallLayer.value == 0)
        {
            wallLayer = groundLayer;
        }

        if (visualRoot == null)
        {
            visualRoot = transform.Find(VisualName);
        }

        ApplyFacing();
    }

    private void OnEnable()
    {
        playerActionMap?.Enable();
    }

    private void OnDisable()
    {
        playerActionMap?.Disable();
    }

    private void Update()
    {
        moveInput = ReadMoveInput();
        wallGrabPressed = WasPressedThisFrame(wallGrabAction);
        UpdateRunMode(WasPressedThisFrame(runToggleAction));
        isRunning = runMode && Mathf.Abs(moveInput.x) > 0.01f;
        jumpHeld = IsPressed(jumpAction) && !IsDashing;
        InteractPressedThisFrame = WasPressedThisFrame(interactAction);

        if (!IsDashing && WasPressedThisFrame(jumpAction))
        {
            jumpBufferTimer = jumpBufferTime;
            DebugJumpInput();
        }

        bool frontDashPressed = WasPressedThisFrame(frontDashAction);
        bool evadePressed = WasPressedThisFrame(evadeAction);
        bool attackPressed = WasPressedThisFrame(attackAction);
        bool iaidoAttackPressed = WasPressedThisFrame(iaidoAttackAction);

        if (frontDashPressed && frontDashCooldownTimer <= 0f && !IsActionLocked)
        {
            StartDash(PlayerActionState.FrontDash, GetFrontDashDirection(), frontDashSpeed, frontDashDuration, frontDashAnimationName);
            frontDashCooldownTimer = frontDashCooldown;
        }

        if (evadePressed && backDashCooldownTimer <= 0f && !IsActionLocked)
        {
            StartDash(PlayerActionState.BackDash, GetEvadeDirection(), backDashSpeed, backDashDuration, backDashAnimationName);
            backDashCooldownTimer = backDashCooldown;
        }

        if (iaidoAttackPressed && frontDashCooldownTimer <= 0f && !IsActionLocked)
        {
            StartDash(PlayerActionState.IaidoDash, GetFrontDashDirection(), frontDashSpeed, frontDashDuration, frontDashAnimationName);
            frontDashCooldownTimer = frontDashCooldown;
        }

        if (debugGroundCheck)
        {
            DebugActionInput(attackPressed, evadePressed, iaidoAttackPressed, InteractPressedThisFrame);
        }

        UpdateAnimatorParameters();
    }

    private void FixedUpdate()
    {
        float realDt = Time.fixedUnscaledDeltaTime;
        TickCooldowns(realDt);
        TickGroundState(realDt);
        TickWallContactState();

        if (IsDashing)
        {
            TickDash();
            UpdateAnimatorParameters();
            return;
        }

        if (IsWallGrabbing)
        {
            TickWallGrab();
            UpdateAnimatorParameters();
            return;
        }

        if (ShouldStartWallGrab())
        {
            StartWallGrab();
            UpdateAnimatorParameters();
            return;
        }

        TickNormalMovement(realDt);
        UpdateAnimatorParameters();
    }

    private void TickCooldowns(float realDt)
    {
        if (frontDashCooldownTimer > 0f)
        {
            frontDashCooldownTimer = Mathf.Max(0f, frontDashCooldownTimer - realDt);
        }

        if (backDashCooldownTimer > 0f)
        {
            backDashCooldownTimer = Mathf.Max(0f, backDashCooldownTimer - realDt);
        }

        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - realDt);
        }
    }

    private void TickGroundState(float realDt)
    {
        bool detectedGround = IsGrounded();
        if (jumpGroundIgnoreTimer > 0f)
        {
            jumpGroundIgnoreTimer = Mathf.Max(0f, jumpGroundIgnoreTimer - realDt);
        }

        bool risingFromJump = body.linearVelocity.y > UpwardUngroundedVelocity;
        isGrounded = detectedGround && jumpGroundIgnoreTimer <= 0f && !risingFromJump;
        coyoteTimer = isGrounded ? coyoteTime : Mathf.Max(0f, coyoteTimer - realDt);

        if (isGrounded)
        {
            airJumpsRemaining = extraAirJumps;
        }

        DebugGroundedChanged();
    }

    private void TickNormalMovement(float realDt)
    {
        float baseMoveSpeed = isRunning ? runSpeed : moveSpeed;
        float targetSpeed = moveInput.x * baseMoveSpeed;
        bool nearApex = !isGrounded && Mathf.Abs(body.linearVelocity.y) <= apexThreshold;
        if (nearApex)
        {
            targetSpeed *= apexMoveSpeedMultiplier;
        }

        float currentSpeed = body.linearVelocity.x;
        float acceleration = Mathf.Abs(targetSpeed) > 0.01f
            ? (isGrounded ? groundAcceleration : airAcceleration)
            : (isGrounded ? groundDeceleration : airDeceleration);

        bool reversingDirection = Mathf.Abs(currentSpeed) > 0.05f &&
            Mathf.Abs(targetSpeed) > 0.05f &&
            Mathf.Sign(currentSpeed) != Mathf.Sign(targetSpeed);
        if (reversingDirection)
        {
            acceleration *= turnaroundAccelerationMultiplier;
        }

        Vector2 velocity = body.linearVelocity;
        velocity.x = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * realDt);

        if (jumpBufferTimer > 0f && CanJump())
        {
            velocity.y = jumpForce;
            ConsumeJump();
            isGrounded = false;
            jumpBufferTimer = 0f;
            jumpGroundIgnoreTimer = JumpGroundIgnoreDuration;
        }

        float gravityScale = baseGravityScale;
        if (velocity.y < -0.01f)
        {
            gravityScale *= fallGravityMultiplier;
        }
        else if (!jumpHeld && velocity.y > 0.01f)
        {
            gravityScale *= jumpCutGravityMultiplier;
        }
        else if (nearApex)
        {
            gravityScale *= apexGravityMultiplier;
        }

        body.gravityScale = gravityScale;
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -groundedStickForce;
        }

        velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        body.linearVelocity = velocity;

        if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            facing = Mathf.Sign(moveInput.x);
            ApplyFacing();
        }
    }

    private bool CanJump()
    {
        return coyoteTimer > 0f || (!isGrounded && airJumpsRemaining > 0);
    }

    private void ConsumeJump()
    {
        if (coyoteTimer <= 0f && !isGrounded)
        {
            airJumpsRemaining = Mathf.Max(0, airJumpsRemaining - 1);
        }

        coyoteTimer = 0f;
    }

    private void ResolveInputActions()
    {
        if (inputActions == null)
        {
            inputActions = UnityEngine.InputSystem.InputSystem.actions;
        }

        if (inputActions == null)
        {
            Debug.LogError("P_PlayerController could not find Player_Action input actions.", this);
            return;
        }

        playerActionMap = inputActions.FindActionMap(PlayerActionMapName, throwIfNotFound: false);
        if (playerActionMap == null)
        {
            Debug.LogError($"Input Actions is missing action map '{PlayerActionMapName}'.", this);
            return;
        }

        moveAction = playerActionMap.FindAction(MoveActionName, throwIfNotFound: false);
        jumpAction = playerActionMap.FindAction(JumpActionName, throwIfNotFound: false);
        runToggleAction = playerActionMap.FindAction(RunToggleActionName, throwIfNotFound: false);
        attackAction = playerActionMap.FindAction(AttackActionName, throwIfNotFound: false);
        frontDashAction = playerActionMap.FindAction(FrontDashActionName, throwIfNotFound: false);
        evadeAction = playerActionMap.FindAction(EvadeActionName, throwIfNotFound: false);
        iaidoAttackAction = playerActionMap.FindAction(IaidoAttackActionName, throwIfNotFound: false);
        interactAction = playerActionMap.FindAction(InteractActionName, throwIfNotFound: false);
        wallGrabAction = playerActionMap.FindAction(WallGrabActionName, throwIfNotFound: false);
    }

    private Vector2 ReadMoveInput()
    {
        return moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    }

    private static bool WasPressedThisFrame(InputAction action)
    {
        return action != null && action.WasPressedThisFrame();
    }

    private static bool IsPressed(InputAction action)
    {
        return action != null && action.IsPressed();
    }

    private void UpdateRunMode(bool runTogglePressed)
    {
        if (runTogglePressed)
        {
            runMode = !runMode;
        }
    }

    private void StartDash(PlayerActionState dashState, Vector2 direction, float speed, float duration, string animationName)
    {
        actionState = dashState;
        dashDirection = direction.sqrMagnitude > 0.01f
            ? direction.normalized
            : new Vector2(facing, 0f);
        activeDashSpeed = speed;
        dashTimer = duration;
        dashDistanceRemaining = speed * duration;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;

        body.gravityScale = 0f;
        body.linearVelocity = Vector2.zero;
        PlayAnimation(animationName);
    }

    private void TickDash()
    {
        float scaledDt = Time.fixedDeltaTime;
        dashTimer = Mathf.Max(0f, dashTimer - scaledDt);
        body.gravityScale = 0f;

        float stepDistance = Mathf.Min(activeDashSpeed * scaledDt, dashDistanceRemaining);
        dashDistanceRemaining = Mathf.Max(0f, dashDistanceRemaining - stepDistance);
        body.MovePosition(body.position + dashDirection * stepDistance);

        if (dashDistanceRemaining <= 0.001f || dashTimer <= 0f)
        {
            FinishAction();
        }
    }

    private bool ShouldStartWallGrab()
    {
        return wallGrabPressed &&
            !isGrounded &&
            !IsDashing &&
            (touchingLeftWall || touchingRightWall);
    }

    private void StartWallGrab()
    {
        actionState = PlayerActionState.WallGrab;
        wallDirection = ResolveWallDirection();
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        body.gravityScale = 0f;
        body.linearVelocity = Vector2.zero;
        PlayAnimation(wallGrabAnimationName);
    }

    private void TickWallGrab()
    {
        if (wallGrabPressed || isGrounded || (!touchingLeftWall && !touchingRightWall))
        {
            FinishWallGrab();
            return;
        }

        if (jumpBufferTimer > 0f)
        {
            WallJump();
            return;
        }

        body.gravityScale = 0f;
        body.linearVelocity = new Vector2(0f, Mathf.Min(0f, wallSlideSpeed));
    }

    private void WallJump()
    {
        int jumpDirection = wallDirection == 0 ? -(int)Mathf.Sign(facing) : -wallDirection;
        if (jumpDirection == 0)
        {
            jumpDirection = 1;
        }

        actionState = PlayerActionState.Normal;
        body.gravityScale = baseGravityScale;
        body.linearVelocity = new Vector2(wallJumpForce.x * jumpDirection, wallJumpForce.y);
        facing = jumpDirection;
        ApplyFacing();
        jumpBufferTimer = 0f;
        jumpGroundIgnoreTimer = JumpGroundIgnoreDuration;
        PlayAnimation(jumpAnimationName);
    }

    private void FinishWallGrab()
    {
        actionState = PlayerActionState.Normal;
        wallDirection = 0;
        body.gravityScale = baseGravityScale;
        PlayCurrentLocomotionAnimation();
    }

    private void FinishAction()
    {
        actionState = PlayerActionState.Normal;
        dashTimer = 0f;
        dashDistanceRemaining = 0f;
        body.gravityScale = baseGravityScale;
        body.linearVelocity = Vector2.zero;
        PlayCurrentLocomotionAnimation();
    }

    private void ApplyFacing()
    {
        if (visualRoot == null)
        {
            return;
        }

        Vector3 scale = visualRoot.localScale;
        scale.x = Mathf.Abs(scale.x) * facing;
        visualRoot.localScale = scale;
    }

    private Vector2 GetFrontDashDirection()
    {
        return new Vector2(facing, 0f);
    }

    private Vector2 GetEvadeDirection()
    {
        return new Vector2(-facing, 0f);
    }

    private void TickWallContactState()
    {
        touchingLeftWall = IsTouchingWall(wallCheckLeft);
        touchingRightWall = IsTouchingWall(wallCheckRight);
        DebugWallContactChanged();
    }

    private int ResolveWallDirection()
    {
        if (touchingLeftWall && !touchingRightWall)
        {
            return -1;
        }

        if (touchingRightWall && !touchingLeftWall)
        {
            return 1;
        }

        return facing >= 0f ? 1 : -1;
    }

    private void PlayAnimation(string animationName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(animationName))
        {
            return;
        }

        animator.Play(animationName);
    }

    private void PlayCurrentLocomotionAnimation()
    {
        if (!isGrounded)
        {
            PlayAnimation(body.linearVelocity.y >= 0f ? jumpAnimationName : fallAnimationName);
            return;
        }

        if (Mathf.Abs(moveInput.x) <= 0.01f)
        {
            PlayAnimation(idleAnimationName);
            return;
        }

        PlayAnimation(isRunning ? runAnimationName : walkAnimationName);
    }

    private bool IsGrounded()
    {
        if (body == null)
        {
            return false;
        }

        float bodyCenterY = bodyCollider != null ? bodyCollider.bounds.center.y : body.position.y;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);
        filter.useTriggers = false;

        int contactCount = body.GetContacts(filter, groundContacts);
        for (int i = 0; i < contactCount; i++)
        {
            ContactPoint2D contact = groundContacts[i];
            bool contactBelowBodyCenter = contact.point.y <= bodyCenterY + GroundContactVerticalMargin;
            bool contactFacesUpOrDown = Mathf.Abs(contact.normal.y) >= MinGroundContactNormalY;
            if (contactBelowBodyCenter && contactFacesUpOrDown)
            {
                return true;
            }
        }

        return IsGroundedBySensorFallback();
    }

    private bool IsGroundedBySensorFallback()
    {
        if (groundCheck == null)
        {
            return false;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            groundCheck.position,
            groundCheckRadius,
            groundLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            bool isSelfCollider = false;
            for (int selfIndex = 0; selfIndex < selfColliders.Length; selfIndex++)
            {
                if (selfColliders[selfIndex] == hit)
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

    private bool IsTouchingWall(Transform wallCheck)
    {
        if (wallCheck == null)
        {
            return false;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            wallCheck.position,
            wallCheckRadius,
            wallLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            bool isSelfCollider = false;
            for (int selfIndex = 0; selfIndex < selfColliders.Length; selfIndex++)
            {
                if (selfColliders[selfIndex] == hit)
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

    private void UpdateAnimatorParameters()
    {
        if (animator == null)
        {
            return;
        }

        if (hasMoveSpeedParameter)
        {
            animator.SetFloat(MoveSpeedHash, Mathf.Abs(body.linearVelocity.x));
        }

        if (hasIsRunningParameter)
        {
            animator.SetBool(IsRunningHash, isRunning);
        }

        if (hasIsGroundedParameter)
        {
            animator.SetBool(IsGroundedHash, isGrounded);
        }

        if (hasIsDashingParameter)
        {
            animator.SetBool(IsDashingHash, IsDashing);
        }

        if (hasVerticalSpeedParameter)
        {
            animator.SetFloat(VerticalSpeedHash, body.linearVelocity.y);
        }
    }

    private void DebugJumpInput()
    {
        if (!debugGroundCheck)
        {
            return;
        }

        string groundCheckPosition = groundCheck != null
            ? groundCheck.position.ToString()
            : "None";

        Debug.Log(
            $"Jump pressed. isGrounded={isGrounded}, groundCheck={groundCheckPosition}, " +
            $"groundLayer={groundLayer.value}, velocity={body.linearVelocity}",
            this);
    }

    private void DebugGroundedChanged()
    {
        if (!debugGroundCheck || wasGrounded == isGrounded)
        {
            return;
        }

        wasGrounded = isGrounded;

        string groundCheckPosition = groundCheck != null
            ? groundCheck.position.ToString()
            : "None";

        Debug.Log(
            $"Grounded changed: {isGrounded}. groundCheck={groundCheckPosition}, " +
            $"radius={groundCheckRadius}, groundLayer={groundLayer.value}",
            this);
    }

    private void DebugWallContactChanged()
    {
        if (!debugWallCheck ||
            (wasTouchingLeftWall == touchingLeftWall && wasTouchingRightWall == touchingRightWall))
        {
            return;
        }

        wasTouchingLeftWall = touchingLeftWall;
        wasTouchingRightWall = touchingRightWall;

        string leftPosition = wallCheckLeft != null
            ? wallCheckLeft.position.ToString()
            : "None";
        string rightPosition = wallCheckRight != null
            ? wallCheckRight.position.ToString()
            : "None";

        Debug.Log(
            $"Wall contact changed: left={touchingLeftWall}, right={touchingRightWall}. " +
            $"leftCheck={leftPosition}, rightCheck={rightPosition}, " +
            $"radius={wallCheckRadius}, wallLayer={wallLayer.value}, isGrounded={isGrounded}",
            this);
    }

    private void DebugActionInput(
        bool attackPressed,
        bool evadePressed,
        bool iaidoAttackPressed,
        bool interactPressed)
    {
        if (attackPressed)
        {
            Debug.Log("Attack pressed.", this);
        }

        if (evadePressed)
        {
            Debug.Log("Evade pressed.", this);
        }

        if (iaidoAttackPressed)
        {
            Debug.Log("IaidoAttack pressed.", this);
        }

        if (interactPressed)
        {
            Debug.Log("Interact pressed.", this);
        }
    }

    private void CacheAnimatorParameters()
    {
        if (animator == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == MoveSpeedHash)
            {
                hasMoveSpeedParameter = true;
            }
            else if (parameter.nameHash == IsRunningHash)
            {
                hasIsRunningParameter = true;
            }
            else if (parameter.nameHash == IsGroundedHash)
            {
                hasIsGroundedParameter = true;
            }
            else if (parameter.nameHash == IsDashingHash)
            {
                hasIsDashingParameter = true;
            }
            else if (parameter.nameHash == VerticalSpeedHash)
            {
                hasVerticalSpeedParameter = true;
            }
        }
    }

    private void EnsurePlayerIdentity()
    {
        if (!CompareTag("Player"))
        {
            gameObject.tag = "Player";
        }

        int playerLayer = LayerMask.NameToLayer(PlayerLayerName);
        if (playerLayer >= 0)
        {
            gameObject.layer = playerLayer;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Gizmos.color = Color.yellow;
        if (wallCheckLeft != null)
        {
            Gizmos.DrawWireSphere(wallCheckLeft.position, wallCheckRadius);
        }

        if (wallCheckRight != null)
        {
            Gizmos.DrawWireSphere(wallCheckRight.position, wallCheckRadius);
        }
    }
}
