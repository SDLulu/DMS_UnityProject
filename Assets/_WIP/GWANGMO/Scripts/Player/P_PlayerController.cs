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

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float jumpForce = 9f;

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

    private Rigidbody2D body;
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
    private Vector2 moveInput;
    private bool isGrounded;
    private bool isDashing;
    private bool runMode;
    private bool isRunning;
    private Vector2 dashDirection = Vector2.right;
    private float activeDashSpeed;
    private float dashTimer;
    private float frontDashCooldownTimer;
    private float backDashCooldownTimer;
    private float facing = 1f;
    private float originalGravityScale;
    private bool wasGrounded;
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

    public bool IsDashing => isDashing;
    public bool IsRunning => isRunning;
    public Transform VisualRoot => visualRoot;
    public float FacingDirection => facing;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        selfColliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
        originalGravityScale = body.gravityScale;
        ResolveInputActions();
        CacheAnimatorParameters();

        if (groundLayer.value == 0)
        {
            groundLayer = LayerMask.GetMask("Ground");
        }

        if (visualRoot == null)
        {
            visualRoot = transform.Find("Visual");
        }
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
        UpdateRunMode(WasPressedThisFrame(runToggleAction));
        isRunning = runMode && Mathf.Abs(moveInput.x) > 0.01f;

        if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            facing = Mathf.Sign(moveInput.x);
            ApplyFacing();
        }

        bool jumpPressed = WasPressedThisFrame(jumpAction);
        bool frontDashPressed = WasPressedThisFrame(frontDashAction);
        bool evadePressed = WasPressedThisFrame(evadeAction);
        bool attackPressed = WasPressedThisFrame(attackAction);
        bool iaidoAttackPressed = WasPressedThisFrame(iaidoAttackAction);
        bool interactPressed = WasPressedThisFrame(interactAction);

        if (jumpPressed)
        {
            DebugJumpInput();
        }

        if (jumpPressed && isGrounded)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
        }

        if (frontDashPressed && frontDashCooldownTimer <= 0f && !isDashing)
        {
            StartDash(GetFrontDashDirection(), frontDashSpeed, frontDashDuration, frontDashAnimationName);
            frontDashCooldownTimer = frontDashCooldown;
        }

        if (evadePressed && backDashCooldownTimer <= 0f && !isDashing)
        {
            StartDash(GetEvadeDirection(), backDashSpeed, backDashDuration, backDashAnimationName);
            backDashCooldownTimer = backDashCooldown;
        }

        if (iaidoAttackPressed && frontDashCooldownTimer <= 0f && !isDashing)
        {
            StartDash(GetFrontDashDirection(), frontDashSpeed, frontDashDuration, frontDashAnimationName);
            frontDashCooldownTimer = frontDashCooldown;
        }

        if (debugGroundCheck)
        {
            DebugActionInput(attackPressed, evadePressed, iaidoAttackPressed, interactPressed);
        }

        UpdateAnimatorParameters();
    }

    private void FixedUpdate()
    {
        isGrounded = IsGrounded();

        DebugGroundedChanged();

        if (frontDashCooldownTimer > 0f)
        {
            frontDashCooldownTimer -= Time.fixedDeltaTime;
        }

        if (backDashCooldownTimer > 0f)
        {
            backDashCooldownTimer -= Time.fixedDeltaTime;
        }

        if (isDashing)
        {
            TickDash();
            return;
        }

        float currentMoveSpeed = isRunning ? runSpeed : moveSpeed;
        body.linearVelocity = new Vector2(moveInput.x * currentMoveSpeed, body.linearVelocity.y);
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
    }

    private Vector2 ReadMoveInput()
    {
        return moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    }

    private static bool WasPressedThisFrame(InputAction action)
    {
        return action != null && action.WasPressedThisFrame();
    }

    private void UpdateRunMode(bool runTogglePressed)
    {
        if (runTogglePressed)
        {
            runMode = !runMode;
        }
    }

    private void StartDash(Vector2 direction, float speed, float duration, string animationName)
    {
        isDashing = true;
        dashDirection = direction.sqrMagnitude > 0.01f
            ? direction.normalized
            : new Vector2(facing, 0f);
        activeDashSpeed = speed;
        dashTimer = duration;

        body.gravityScale = 0f;
        body.linearVelocity = dashDirection * activeDashSpeed;
        PlayAnimation(animationName);
    }

    private void TickDash()
    {
        dashTimer -= Time.fixedDeltaTime;
        body.linearVelocity = dashDirection * activeDashSpeed;

        if (dashTimer <= 0f)
        {
            isDashing = false;
            body.gravityScale = originalGravityScale;
            PlayCurrentLocomotionAnimation();
        }
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

    private void UpdateAnimatorParameters()
    {
        if (animator == null)
        {
            return;
        }

        if (hasMoveSpeedParameter)
        {
            animator.SetFloat(MoveSpeedHash, Mathf.Abs(moveInput.x));
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
            animator.SetBool(IsDashingHash, isDashing);
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

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
