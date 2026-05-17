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
    [SerializeField] private Transform hitboxRoot;
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

        if (IsGunner)
        {
            return absX <= gunRange;
        }

        // 근접: 히트박스 영역 안에 플레이어가 있는지를 직접 본다.
        // 거리 기반 판정과 폴리곤/박스의 실제 영역이 어긋날 일이 없어진다.
        DeadRevolverEnemyMeleeHitbox hitbox = GetPrimaryHitbox();
        return hitbox != null && hitbox.HasPlayerInRange;
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
        hitboxRoot ??= transform.Find("Hitboxes");

        if (visualRoot != null)
        {
            visualAnimator ??= visualRoot.GetComponent<Animator>();
            visualAnimator ??= visualRoot.GetComponentInChildren<Animator>(true);
            muzzlePoint ??= visualRoot.Find("MuzzlePoint");
            punchHitbox ??= visualRoot.Find("PunchHitbox")?.GetComponent<DeadRevolverEnemyMeleeHitbox>();
            swordHitbox ??= visualRoot.Find("SwordHitbox")?.GetComponent<DeadRevolverEnemyMeleeHitbox>();
            shieldHitbox ??= visualRoot.Find("ShieldHitbox")?.GetComponent<DeadRevolverEnemyMeleeHitbox>();
        }

        if (hitboxRoot != null)
        {
            punchHitbox ??= hitboxRoot.Find("PunchHitbox")?.GetComponent<DeadRevolverEnemyMeleeHitbox>();
            swordHitbox ??= hitboxRoot.Find("SwordHitbox")?.GetComponent<DeadRevolverEnemyMeleeHitbox>();
            shieldHitbox ??= hitboxRoot.Find("ShieldHitbox")?.GetComponent<DeadRevolverEnemyMeleeHitbox>();
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
        float sign = invertVisualFacing ? -_facing : _facing;

        if (visualRoot != null)
        {
            Vector3 visualScale = visualRoot.localScale;
            visualScale.x = Mathf.Abs(visualScale.x) * sign;
            visualRoot.localScale = visualScale;
        }

        if (hitboxRoot != null)
        {
            Vector3 hitboxScale = hitboxRoot.localScale;
            hitboxScale.x = Mathf.Abs(hitboxScale.x) * sign;
            hitboxRoot.localScale = hitboxScale;
        }
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
        // 감지 범위 (탐지 시야)
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (IsGunner)
        {
            // 사거리
            Gizmos.color = new Color(1f, 0.7f, 0.3f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, gunRange);
        }
        else
        {
            // 근접 히트박스 (실제 모양 그대로)
            Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.9f);
            DeadRevolverEnemyMeleeHitbox hitbox = GetPrimaryHitbox();
            if (hitbox != null)
            {
                DrawCollider2DGizmo(hitbox.GetComponent<Collider2D>());
            }
        }
    }

    private static void DrawCollider2DGizmo(Collider2D col)
    {
        if (col == null)
        {
            return;
        }

        Transform t = col.transform;

        switch (col)
        {
            case BoxCollider2D box:
                Gizmos.matrix = t.localToWorldMatrix;
                Gizmos.DrawWireCube(box.offset, box.size);
                Gizmos.matrix = Matrix4x4.identity;
                break;

            case CircleCollider2D circle:
            {
                Vector3 center = t.TransformPoint(circle.offset);
                float radius = circle.radius * Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y));
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.DrawWireSphere(center, radius);
                break;
            }

            case PolygonCollider2D poly:
                Gizmos.matrix = Matrix4x4.identity;
                for (int pathIndex = 0; pathIndex < poly.pathCount; pathIndex++)
                {
                    Vector2[] points = poly.GetPath(pathIndex);
                    for (int i = 0; i < points.Length; i++)
                    {
                        Vector3 a = t.TransformPoint(points[i] + poly.offset);
                        Vector3 b = t.TransformPoint(points[(i + 1) % points.Length] + poly.offset);
                        Gizmos.DrawLine(a, b);
                    }
                }
                break;
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
