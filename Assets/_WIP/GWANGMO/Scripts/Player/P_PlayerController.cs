using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class P_PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float jumpForce = 9f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.5f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;

    [Header("Debug")]
    [SerializeField] private bool debugGroundCheck;

    private Rigidbody2D body;
    private Animator animator;
    private Collider2D[] selfColliders;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool isDashing;
    private bool runMode;
    private bool isRunning;
    private float dashTimer;
    private float dashCooldownTimer;
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

    private void Update()
    {
        moveInput = GameInput.Instance.Move;
        UpdateRunMode();
        isRunning = runMode && Mathf.Abs(moveInput.x) > 0.01f;

        if (Mathf.Abs(moveInput.x) > 0.01f)
        {
            facing = Mathf.Sign(moveInput.x);
            ApplyFacing();
        }

        if (GameInput.Instance.JumpPressed)
        {
            DebugJumpInput();
        }

        if (GameInput.Instance.JumpPressed && isGrounded)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
        }

        if (GameInput.Instance.DashPressed && dashCooldownTimer <= 0f && !isDashing)
        {
            StartDash();
        }

        UpdateAnimatorParameters();
    }

    private void FixedUpdate()
    {
        isGrounded = IsGrounded();

        DebugGroundedChanged();

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.fixedDeltaTime;
        }

        if (isDashing)
        {
            TickDash();
            return;
        }

        float currentMoveSpeed = isRunning ? runSpeed : moveSpeed;
        body.linearVelocity = new Vector2(moveInput.x * currentMoveSpeed, body.linearVelocity.y);
    }

    private void UpdateRunMode()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.leftCtrlKey.wasPressedThisFrame || keyboard.rightCtrlKey.wasPressedThisFrame)
        {
            runMode = !runMode;
        }
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        body.gravityScale = 0f;
        body.linearVelocity = new Vector2(facing * dashSpeed, 0f);
    }

    private void TickDash()
    {
        dashTimer -= Time.fixedDeltaTime;
        body.linearVelocity = new Vector2(facing * dashSpeed, 0f);

        if (dashTimer <= 0f)
        {
            isDashing = false;
            body.gravityScale = originalGravityScale;
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
