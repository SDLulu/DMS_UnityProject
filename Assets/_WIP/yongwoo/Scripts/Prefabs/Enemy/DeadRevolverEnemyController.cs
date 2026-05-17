using UnityEngine;

// 역할:
// - 점프 없이 바닥에서만 움직이는 DeadRevolver 적 4종의 추적/공격을 공통 처리합니다.
// - 비주얼은 Animator로만 바꾸고, 실제 판정은 여기서 직접 실행합니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyInteraction))]
public class DeadRevolverEnemyController : MonoBehaviour
{
    private const string ProjectileLayerName = "Projectile";
    private const string PlayerLayerName = "Player";
    private const string EnemyLayerName = "Enemy";

    public enum DeadRevolverArchetype
    {
        Gunner,
        Swordsman,
        Brawler,
        ShieldBearer
    }

    [Header("Identity")]
    [SerializeField] private DeadRevolverArchetype archetype = DeadRevolverArchetype.Brawler;

    [Header("References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Animator visualAnimator;
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private DeadRevolverEnemyMeleeHitbox punchHitbox;
    [SerializeField] private DeadRevolverEnemyMeleeHitbox swordHitbox;
    [SerializeField] private DeadRevolverEnemyMeleeHitbox shieldHitbox;
    [SerializeField] private EnemyInteraction interaction;
    [SerializeField] private Rigidbody2D body;

    [Header("Target")]
    [SerializeField] private Transform targetOverride;
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float verticalTolerance = 1.6f;
    [SerializeField] private float targetRefreshInterval = 0.35f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.4f;
    [SerializeField] private float acceleration = 26f;
    [SerializeField] private float stopDistance = 1f;
    [SerializeField] private float bodySeparationDistance = 0.72f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float ledgeProbeForward = 0.5f;
    [SerializeField] private float ledgeProbeHeight = 0.15f;
    [SerializeField] private float ledgeProbeDepth = 1.35f;
    [SerializeField] private bool invertVisualFacing;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 0.8f;
    [SerializeField] private float attackWindup = 0.14f;
    [SerializeField] private float attackRecovery = 0.18f;
    [SerializeField] private float meleeRange = 1.05f;
    [SerializeField] private Vector2 meleeHitboxSize = new Vector2(1.05f, 0.72f);
    [SerializeField] private float meleeDamage = 1f;
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackUpForce = 2f;

    [Header("Gun")]
    [SerializeField] private float gunRange = 5.2f;
    [SerializeField] private float projectileSpeed = 9.8f;
    [SerializeField] private float projectileLifetime = 1.4f;
    [SerializeField] private float projectileDamage = 1f;
    [SerializeField] private float projectileKnockback = 5f;
    [SerializeField] private float projectileRadius = 0.08f;
    [SerializeField] private Color projectileColor = new Color(1f, 0.72f, 0.28f, 1f);

    private Transform _target;
    private PlayerInteraction _targetInteraction;
    private float _targetRefreshTimer;
    private float _attackCooldownTimer;
    private float _attackTimer;
    private float _facing = 1f;

    private bool IsGunner => archetype == DeadRevolverArchetype.Gunner;
    private bool IsAttacking => _attackTimer > 0f;

    private void Awake()
    {
        CacheReferences();

        if (groundLayer.value == 0)
        {
            groundLayer = LayerMask.GetMask("Ground");
        }

        if (body != null)
        {
            body.freezeRotation = true;
        }

        EnsurePlayerEnemyCollision();
        ApplyFacingToVisual();
        SetMoveAnimation(false);
    }

    private void OnEnable()
    {
        CacheReferences();

        if (interaction != null)
        {
            interaction.Damaged += HandleDamaged;
            interaction.Died += HandleDied;
            interaction.Respawned += HandleRespawned;
        }
    }

    private void OnDisable()
    {
        if (interaction != null)
        {
            interaction.Damaged -= HandleDamaged;
            interaction.Died -= HandleDied;
            interaction.Respawned -= HandleRespawned;
        }
    }

    private void Update()
    {
        if (_attackCooldownTimer > 0f)
        {
            _attackCooldownTimer -= Time.deltaTime;
        }

        _targetRefreshTimer -= Time.deltaTime;
        if (_targetRefreshTimer > 0f)
        {
            return;
        }

        RefreshTarget();
        _targetRefreshTimer = targetRefreshInterval;
    }

    private void FixedUpdate()
    {
        if (interaction != null && interaction.IsDead)
        {
            ApplyHorizontalMovement(0f);
            return;
        }

        if (_target == null || (_targetInteraction != null && _targetInteraction.IsDead))
        {
            ApplyHorizontalMovement(0f);
            return;
        }

        Vector2 toTarget = _target.position - transform.position;
        float absX = Mathf.Abs(toTarget.x);
        float absY = Mathf.Abs(toTarget.y);

        if (toTarget.sqrMagnitude > detectionRange * detectionRange || absY > verticalTolerance)
        {
            ApplyHorizontalMovement(0f);
            return;
        }

        if (absX > 0.05f)
        {
            _facing = Mathf.Sign(toTarget.x);
            ApplyFacingToVisual();
        }

        if (IsAttacking)
        {
            TickAttack(Time.fixedDeltaTime);
            return;
        }

        if (ShouldSeparateFromTarget(absX, absY, toTarget.x))
        {
            ApplyHorizontalMovement(-Mathf.Sign(toTarget.x));
            return;
        }

        if (CanStartAttack(absX, absY))
        {
            StartAttack();
            return;
        }

        float desiredDirection = GetDesiredMoveDirection(absX, toTarget.x);
        if (desiredDirection != 0f && !HasGroundAhead(desiredDirection))
        {
            desiredDirection = 0f;
        }

        ApplyHorizontalMovement(desiredDirection);
    }

    private void StartAttack()
    {
        _attackTimer = attackWindup + attackRecovery;
        EndPrimaryHitboxWindow();
        ApplyHorizontalMovement(0f);
        TriggerAnimation("Attack");
    }

    private void TickAttack(float deltaTime)
    {
        _attackTimer -= deltaTime;
        ApplyHorizontalMovement(0f);

        if (_attackTimer > 0f)
        {
            return;
        }

        _attackTimer = 0f;
        _attackCooldownTimer = attackCooldown;
        EndPrimaryHitboxWindow();
    }

    public void FireProjectileFromAnimation()
    {
        if (!IsGunner || !IsAttacking)
        {
            return;
        }

        FireProjectile();
    }

    public void BeginPrimaryHitboxWindow()
    {
        if (IsGunner || !IsAttacking)
        {
            return;
        }

        DeadRevolverEnemyMeleeHitbox hitbox = GetPrimaryHitbox();
        if (hitbox == null)
        {
            return;
        }

        Vector2 knockback = Vector2.right * (_facing * knockbackForce) + Vector2.up * knockbackUpForce;
        hitbox.Activate(gameObject, meleeDamage, knockback);
    }

    public void EndPrimaryHitboxWindow()
    {
        punchHitbox?.Deactivate();
        swordHitbox?.Deactivate();
        shieldHitbox?.Deactivate();
    }

    private void FireProjectile()
    {
        Vector2 spawn = muzzlePoint != null
            ? muzzlePoint.position
            : (Vector2)transform.TransformPoint(new Vector3(0.85f * _facing, 0.28f, 0f));
        Vector2 aimDirection = _target != null
            ? ((Vector2)_target.position - spawn).normalized
            : Vector2.right * _facing;

        if (aimDirection.sqrMagnitude <= 0.001f)
        {
            aimDirection = Vector2.right * _facing;
        }

        GameObject projectileObject = new GameObject($"{name}_Projectile");
        projectileObject.transform.position = spawn;
        ApplyProjectileLayer(projectileObject);
        DeadRevolverEnemyProjectile projectile = projectileObject.AddComponent<DeadRevolverEnemyProjectile>();
        projectile.Configure(
            aimDirection,
            projectileSpeed,
            projectileLifetime,
            projectileDamage,
            projectileKnockback,
            projectileRadius,
            projectileColor,
            gameObject);
    }

    private bool CanStartAttack(float absX, float absY)
    {
        if (_attackCooldownTimer > 0f || absY > verticalTolerance)
        {
            return false;
        }

        return IsGunner
            ? absX <= gunRange
            : absX <= meleeRange;
    }

    private bool ShouldSeparateFromTarget(float absX, float absY, float xDelta)
    {
        return absY <= verticalTolerance
            && absX > 0.01f
            && absX < bodySeparationDistance;
    }

    private float GetDesiredMoveDirection(float absX, float xDelta)
    {
        float directionToTarget = absX > 0.05f ? Mathf.Sign(xDelta) : _facing;

        if (IsGunner)
        {
            if (absX > gunRange * 0.92f)
            {
                return directionToTarget;
            }

            if (absX < stopDistance * 0.7f)
            {
                return -directionToTarget;
            }

            return 0f;
        }

        return absX > stopDistance ? directionToTarget : 0f;
    }

    private void ApplyHorizontalMovement(float direction)
    {
        if (body == null)
        {
            return;
        }

        float targetSpeed = direction * moveSpeed;
        Vector2 velocity = body.linearVelocity;
        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
        body.linearVelocity = velocity;

        SetMoveAnimation(Mathf.Abs(velocity.x) > 0.05f && !IsAttacking);
    }

    private void RefreshTarget()
    {
        if (targetOverride != null)
        {
            _target = targetOverride;
            _targetInteraction = _target.GetComponent<PlayerInteraction>();
            return;
        }

        SimplePlayerCombat player = FindFirstObjectByType<SimplePlayerCombat>();
        _target = player != null ? player.transform : null;
        _targetInteraction = _target != null ? _target.GetComponent<PlayerInteraction>() : null;
    }

    private void CacheReferences()
    {
        body ??= GetComponent<Rigidbody2D>();
        interaction ??= GetComponent<EnemyInteraction>();
        visualRoot ??= transform.Find("Visual");

        if (visualRoot != null)
        {
            visualAnimator ??= visualRoot.GetComponent<Animator>();
            muzzlePoint ??= visualRoot.Find("MuzzlePoint");
            punchHitbox ??= visualRoot.Find("PunchHitbox")?.GetComponent<DeadRevolverEnemyMeleeHitbox>();
            swordHitbox ??= visualRoot.Find("SwordHitbox")?.GetComponent<DeadRevolverEnemyMeleeHitbox>();
            shieldHitbox ??= visualRoot.Find("ShieldHitbox")?.GetComponent<DeadRevolverEnemyMeleeHitbox>();
        }
    }

    private bool HasGroundAhead(float direction)
    {
        if (groundLayer.value == 0)
        {
            return true;
        }

        Vector2 origin = (Vector2)transform.position + new Vector2(direction * ledgeProbeForward, ledgeProbeHeight);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, ledgeProbeDepth, groundLayer);
        return hit.collider != null;
    }

    private void ApplyFacingToVisual()
    {
        if (visualRoot == null)
        {
            return;
        }

        Vector3 scale = visualRoot.localScale;
        float sign = invertVisualFacing ? -_facing : _facing;
        scale.x = Mathf.Abs(scale.x) * sign;
        visualRoot.localScale = scale;
    }

    private void SetMoveAnimation(bool isMoving)
    {
        if (visualAnimator == null)
        {
            return;
        }

        visualAnimator.SetBool("Move", isMoving);
    }

    private void TriggerAnimation(string triggerName)
    {
        if (visualAnimator == null)
        {
            return;
        }

        visualAnimator.SetTrigger(triggerName);
    }

    private void HandleDamaged()
    {
        TriggerAnimation("Hit");
    }

    private void HandleDied()
    {
        ApplyHorizontalMovement(0f);
        EndPrimaryHitboxWindow();
        TriggerAnimation("Die");
    }

    private void HandleRespawned()
    {
        _attackTimer = 0f;
        _attackCooldownTimer = 0f;
        EndPrimaryHitboxWindow();
        SetMoveAnimation(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsGunner ? new Color(1f, 0.7f, 0.3f, 0.65f) : new Color(1f, 0.25f, 0.25f, 0.65f);

        if (!IsGunner)
        {
            DeadRevolverEnemyMeleeHitbox hitbox = GetPrimaryHitbox();
            BoxCollider2D box = hitbox != null ? hitbox.GetComponent<BoxCollider2D>() : null;
            if (box != null)
            {
                Gizmos.matrix = box.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.offset, box.size);
            }
        }
    }

    private DeadRevolverEnemyMeleeHitbox GetPrimaryHitbox()
    {
        return archetype switch
        {
            DeadRevolverArchetype.Swordsman => swordHitbox,
            DeadRevolverArchetype.Brawler => punchHitbox,
            DeadRevolverArchetype.ShieldBearer => shieldHitbox,
            _ => null
        };
    }

    private static void ApplyProjectileLayer(GameObject projectile)
    {
        int layer = LayerMask.NameToLayer(ProjectileLayerName);
        if (layer >= 0)
        {
            projectile.layer = layer;
        }
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
