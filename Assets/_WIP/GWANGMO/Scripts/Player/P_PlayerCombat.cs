using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class P_PlayerCombat : MonoBehaviour
{
    private const string PlayerActionMapName = "Player";
    private const string AttackActionName = "Attack";
    private const string DashAttackHitboxPath = "Hitboxes/DashAttackHitbox";

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private P_PlayerController controller;
    [SerializeField] private P_PlayerAttackHitbox attack1Hitbox;
    [SerializeField] private P_PlayerAttackHitbox dashAttackHitbox;

    [Header("Attack 1")]
    [SerializeField] private string attack1AnimationName = "Attack_1";
    [SerializeField] private string idleAnimationName = "Idle";
    [SerializeField] private float attack1Duration = 0.5f;
    [SerializeField] private bool lockControllerDuringAttack = true;

    [Header("Dash Attack")]
    [SerializeField] private float dashAttackDamage = 20f;
    [SerializeField] private float dashAttackKnockbackForce = 8f;
    [SerializeField] private float dashAttackKnockbackUpForce = 2.5f;
    [SerializeField] private LayerMask dashAttackHitLayers;

    private InputActionMap playerActionMap;
    private InputAction attackAction;
    private float attackTimer;
    private bool isAttacking;

    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        animator ??= GetComponent<Animator>();
        controller ??= GetComponent<P_PlayerController>();

        if (attack1Hitbox == null)
        {
            attack1Hitbox = GetComponentInChildren<P_PlayerAttackHitbox>(includeInactive: true);
        }

        ResolveDashAttackHitbox();
        ResolveInputActions();
    }

    private void OnEnable()
    {
        playerActionMap?.Enable();
    }

    private void OnDisable()
    {
        EndAttack1Hitbox();
        EndDashAttackHitbox();

        if (lockControllerDuringAttack && controller != null)
        {
            controller.enabled = true;
        }

        isAttacking = false;
        attackTimer = 0f;
    }

    private void Update()
    {
        if (WasPressedThisFrame(attackAction))
        {
            TryStartAttack1();
        }

        TickAttack();
    }

    public void BeginAttack1Hitbox()
    {
        Vector2 direction = GetFacingDirection();
        attack1Hitbox?.BeginHitbox(gameObject, direction);
    }

    public void EndAttack1Hitbox()
    {
        attack1Hitbox?.EndHitbox();

        if (isAttacking)
        {
            FinishAttack();
        }
    }

    public void BeginDashAttackHitbox()
    {
        Vector2 direction = GetFacingDirection();
        dashAttackHitbox?.BeginHitbox(gameObject, direction);
    }

    public void EndDashAttackHitbox()
    {
        dashAttackHitbox?.EndHitbox();
    }

    private void TryStartAttack1()
    {
        if (isAttacking || animator == null || !CanStartGroundAttack())
        {
            return;
        }

        isAttacking = true;
        attackTimer = GetAttack1Duration();
        attack1Hitbox?.EndHitbox();

        if (lockControllerDuringAttack && controller != null)
        {
            controller.enabled = false;
        }

        animator.Play(attack1AnimationName, 0, 0f);
    }

    private void TickAttack()
    {
        if (!isAttacking)
        {
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f)
        {
            return;
        }

        FinishAttack();
    }

    private float GetAttack1Duration()
    {
        float duration = Mathf.Max(0f, attack1Duration);

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return duration;
        }

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && clip.name == attack1AnimationName)
            {
                duration = Mathf.Max(duration, clip.length);
            }
        }

        return duration;
    }

    private void FinishAttack()
    {
        isAttacking = false;
        attackTimer = 0f;
        attack1Hitbox?.EndHitbox();

        if (lockControllerDuringAttack && controller != null)
        {
            controller.enabled = true;
        }

        PlayIdleAnimation();
    }

    private Vector2 GetFacingDirection()
    {
        float x = transform.localScale.x >= 0f ? 1f : -1f;

        if (controller != null)
        {
            x = controller.FacingDirection;
        }

        return new Vector2(Mathf.Sign(x), 0f);
    }

    private bool CanStartGroundAttack()
    {
        return controller == null || controller.IsGroundedNow;
    }

    private void ResolveDashAttackHitbox()
    {
        if (dashAttackHitbox == null)
        {
            Transform hitboxTransform = transform.Find(DashAttackHitboxPath);
            if (hitboxTransform != null)
            {
                dashAttackHitbox = hitboxTransform.GetComponent<P_PlayerAttackHitbox>();
                if (dashAttackHitbox == null && hitboxTransform.GetComponent<Collider2D>() != null)
                {
                    dashAttackHitbox = hitboxTransform.gameObject.AddComponent<P_PlayerAttackHitbox>();
                }
            }
        }

        if (dashAttackHitbox != null)
        {
            dashAttackHitbox.Configure(
                dashAttackDamage,
                dashAttackKnockbackForce,
                dashAttackKnockbackUpForce,
                dashAttackHitLayers);
            dashAttackHitbox.EndHitbox();
        }
    }

    private void ResolveInputActions()
    {
        if (inputActions == null)
        {
            inputActions = UnityEngine.InputSystem.InputSystem.actions;
        }

        if (inputActions == null)
        {
            Debug.LogError("P_PlayerCombat could not find Player_Action input actions.", this);
            return;
        }

        playerActionMap = inputActions.FindActionMap(PlayerActionMapName, throwIfNotFound: false);
        if (playerActionMap == null)
        {
            Debug.LogError($"P_PlayerCombat could not find action map '{PlayerActionMapName}'.", this);
            return;
        }

        attackAction = playerActionMap.FindAction(AttackActionName, throwIfNotFound: false);
        if (attackAction == null)
        {
            Debug.LogError($"P_PlayerCombat could not find action '{AttackActionName}'.", this);
        }
    }

    private static bool WasPressedThisFrame(InputAction action)
    {
        return action != null && action.WasPressedThisFrame();
    }

    private void PlayIdleAnimation()
    {
        if (animator == null || string.IsNullOrWhiteSpace(idleAnimationName))
        {
            return;
        }

        animator.Play(idleAnimationName, 0, 0f);
    }
}
