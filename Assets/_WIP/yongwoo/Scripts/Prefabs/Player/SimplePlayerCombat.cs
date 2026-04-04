using System;
using System.Collections.Generic;
using UnityEngine;

// 역할:
// - 플레이어 무기 전환, 공격 입력 소비, 근접/원거리 공격 실행을 담당합니다.
// - 시각 이펙트와 투사체 생성도 여기서 묶되, 피해 판정은 객체별 Interaction 창구로 넘깁니다.
//
// 구조 포인트:
// - 이동은 SimplePlayerController, 표현은 PlayerAnimationDriver와 분리된 전투 허브입니다.

[DisallowMultipleComponent]
public class SimplePlayerCombat : MonoBehaviour
{
    public event Action AttackPerformed;

    [Header("References")]
    [SerializeField] private Transform weaponOrigin;
    [SerializeField] private Transform muzzle;

    [Header("Targeting")]
    [SerializeField] private LayerMask hitLayers;

    [Header("Attack")]
    [SerializeField] private PlayerWeaponType defaultWeapon = PlayerWeaponType.Sword;
    [SerializeField] private float attackAnimationDuration = 0.18f;
    [SerializeField] private float aimFacingThreshold = 0.18f;

    [Header("Sword")]
    [SerializeField] private Vector2 swordOriginOffset = new Vector2(0.22f, -0.28f);
    [SerializeField] private float swordDamage = 1f;
    [SerializeField] private float swordCooldown = 0.28f;
    [SerializeField] private float swordRange = 1.1f;
    [SerializeField, Range(1f, 180f)] private float swordArcAngle = 90f;
    [SerializeField] private float swordKnockbackX = 6f;
    [SerializeField] private float swordKnockbackY = 2.5f;
    [SerializeField] private float swordVisualDuration = 0.12f;
    [SerializeField] private Color swordVisualColor = new Color(1f, 0.9f, 0.2f, 0.95f);
    [SerializeField] private bool showSwordAimPreview = true;
    [SerializeField] private Color swordPreviewColor = new Color(1f, 0.75f, 0.15f, 0.85f);

    [Header("Gun")]
    [SerializeField] private Vector2 gunMuzzleOffset = new Vector2(0.28f, -0.28f);
    [SerializeField] private float gunDamage = 1f;
    [SerializeField] private float gunCooldown = 0.18f;
    [SerializeField] private float gunProjectileSpeed = 15f;
    [SerializeField] private float gunProjectileLifetime = 1.2f;
    [SerializeField] private float gunProjectileRadius = 0.1f;
    [SerializeField] private float gunKnockbackX = 5f;
    [SerializeField] private float gunKnockbackY = 1.2f;
    [SerializeField] private float gunMuzzleVisualDuration = 0.06f;
    [SerializeField] private Color gunVisualColor = new Color(1f, 0.6f, 0.2f, 0.85f);

    private float _cooldownTimer;
    private PlayerWeaponType _currentWeapon;
    private Vector2 _currentAimDirection = Vector2.right;
    private SimplePlayerController _controller;
    private Camera _mainCamera;
    private GameObject _swordPreviewObject;
    private LineRenderer _swordPreviewLine;

    public PlayerWeaponType CurrentWeapon => _currentWeapon;
    public Vector2 CurrentAimDirection => _currentAimDirection;
    public float AttackCooldown => _currentWeapon == PlayerWeaponType.Gun ? gunCooldown : swordCooldown;
    public float AttackAnimationDuration => attackAnimationDuration;

    public PlayerAttackConfig CreateConfigSnapshot()
    {
        return new PlayerAttackConfig
        {
            defaultWeapon = defaultWeapon,
            attackAnimationDuration = attackAnimationDuration,
            aimFacingThreshold = aimFacingThreshold,
            swordOriginOffset = swordOriginOffset,
            swordDamage = swordDamage,
            swordCooldown = swordCooldown,
            swordRange = swordRange,
            swordArcAngle = swordArcAngle,
            swordKnockbackX = swordKnockbackX,
            swordKnockbackY = swordKnockbackY,
            swordVisualDuration = swordVisualDuration,
            swordVisualColor = new SerializableColor(
                swordVisualColor.r,
                swordVisualColor.g,
                swordVisualColor.b,
                swordVisualColor.a),
            gunMuzzleOffset = gunMuzzleOffset,
            gunDamage = gunDamage,
            gunCooldown = gunCooldown,
            gunProjectileSpeed = gunProjectileSpeed,
            gunProjectileLifetime = gunProjectileLifetime,
            gunProjectileRadius = gunProjectileRadius,
            gunKnockbackX = gunKnockbackX,
            gunKnockbackY = gunKnockbackY,
            gunMuzzleVisualDuration = gunMuzzleVisualDuration,
            gunVisualColor = new SerializableColor(
                gunVisualColor.r,
                gunVisualColor.g,
                gunVisualColor.b,
                gunVisualColor.a)
        };
    }

    public void ApplyConfig(PlayerAttackConfig config)
    {
        config = PlayerConfigLoader.Sanitize(new PlayerConfig { attack = config }).attack;
        defaultWeapon = config.defaultWeapon;
        attackAnimationDuration = config.attackAnimationDuration;
        aimFacingThreshold = config.aimFacingThreshold;
        swordOriginOffset = config.swordOriginOffset;
        swordDamage = config.swordDamage;
        swordCooldown = config.swordCooldown;
        swordRange = config.swordRange;
        swordArcAngle = config.swordArcAngle;
        swordKnockbackX = config.swordKnockbackX;
        swordKnockbackY = config.swordKnockbackY;
        swordVisualDuration = config.swordVisualDuration;
        swordVisualColor = config.swordVisualColor.ToColor();
        gunMuzzleOffset = config.gunMuzzleOffset;
        gunDamage = config.gunDamage;
        gunCooldown = config.gunCooldown;
        gunProjectileSpeed = config.gunProjectileSpeed;
        gunProjectileLifetime = config.gunProjectileLifetime;
        gunProjectileRadius = config.gunProjectileRadius;
        gunKnockbackX = config.gunKnockbackX;
        gunKnockbackY = config.gunKnockbackY;
        gunMuzzleVisualDuration = config.gunMuzzleVisualDuration;
        gunVisualColor = config.gunVisualColor.ToColor();
        _currentWeapon = defaultWeapon;
    }

    private void Awake()
    {
        if (hitLayers.value == 0)
        {
            hitLayers = LayerMask.GetMask("Enemy");
        }

        _controller = GetComponent<SimplePlayerController>();
        _mainCamera = Camera.main;
        _currentWeapon = defaultWeapon;

        weaponOrigin ??= transform.Find("WeaponOrigin");
        muzzle ??= transform.Find("Muzzle");
        EnsureSwordPreview();
    }

    private void Update()
    {
        // 전투 틱에서는 쿨다운, 조준, 무기 선택 프리뷰를 먼저 갱신합니다.
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
        }

        UpdateAimDirection();
        UpdateWeaponSelection();
        UpdateSwordPreview();

        if (_controller != null && _controller.IsActionLocked)
        {
            return;
        }

        // 실제 공격 발동은 쿨다운이 끝난 프레임에만 허용합니다.
        if (_cooldownTimer <= 0f && ReadAttackPressed())
        {
            PerformAttack();
        }
    }

    private void OnDisable()
    {
        _controller?.SetExternalFacing(_controller.FacingDirection, false);
        if (_swordPreviewObject != null)
        {
            _swordPreviewObject.SetActive(false);
        }
    }

    private void PerformAttack()
    {
        // 무기별 분기 전에 공통 처리인 쿨다운과 이벤트를 먼저 확정합니다.
        _cooldownTimer = AttackCooldown;
        AttackPerformed?.Invoke();

        switch (_currentWeapon)
        {
            case PlayerWeaponType.Gun:
                PerformGunAttack();
                break;
            default:
                PerformSwordAttack();
                break;
        }
    }

    private void PerformSwordAttack()
    {
        // 근접 공격은 범위 후보를 모은 뒤 거리/각도 조건을 통과한 대상만 맞힙니다.
        Vector2 origin = GetWeaponOriginPosition();
        SpawnSwordSlashVisual(origin, _currentAimDirection);

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, swordRange, hitLayers);
        HashSet<Component> damaged = new HashSet<Component>();
        float halfArc = swordArcAngle * 0.5f;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            Component target = ResolveDamageTarget(hit);
            if (target == null || damaged.Contains(target))
            {
                continue;
            }

            Vector2 targetPoint = hit.bounds.ClosestPoint(origin);
            Vector2 toTarget = targetPoint - origin;
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                toTarget = (Vector2)target.transform.position - origin;
            }

            if (toTarget.sqrMagnitude > swordRange * swordRange)
            {
                continue;
            }

            float angle = Vector2.Angle(_currentAimDirection, toTarget.normalized);
            if (angle > halfArc)
            {
                continue;
            }

            // 한 번의 휘두름 안에서는 같은 타깃을 한 번만 맞게 합니다.
            damaged.Add(target);
            ApplyHit(target, swordDamage, _currentAimDirection * swordKnockbackX + Vector2.up * swordKnockbackY);
        }
    }

    private void PerformGunAttack()
    {
        // 원거리 공격은 즉시 판정 대신 투사체 한 발을 생성해 책임을 넘깁니다.
        Vector2 spawnPosition = GetMuzzlePosition();
        SpawnGunMuzzleVisual(spawnPosition, _currentAimDirection);

        GameObject projectileObject = new GameObject("PlayerProjectile");
        projectileObject.transform.position = spawnPosition;

        SimplePlayerProjectile projectile = projectileObject.AddComponent<SimplePlayerProjectile>();
        projectile.Configure(
            _currentAimDirection,
            gunProjectileSpeed,
            gunProjectileLifetime,
            gunDamage,
            _currentAimDirection * gunKnockbackX + Vector2.up * gunKnockbackY,
            gunProjectileRadius,
            gunVisualColor,
            gameObject);
    }

    private void UpdateWeaponSelection()
    {
        if (!ReadWeaponSwapPressed())
        {
            return;
        }

        _currentWeapon = _currentWeapon == PlayerWeaponType.Sword
            ? PlayerWeaponType.Gun
            : PlayerWeaponType.Sword;
    }

    private void UpdateAimDirection()
    {
        // 포인터 입력이 없을 때를 대비해 현재 바라보는 방향을 fallback으로 유지합니다.
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        Vector2 fallbackDirection = _controller != null && _controller.FacingDirection < 0f
            ? Vector2.left
            : Vector2.right;

        if (TryGetAimWorldPosition(out Vector2 worldPosition))
        {
            Vector2 aim = worldPosition - (Vector2)transform.position;
            if (aim.sqrMagnitude > 0.0001f)
            {
                _currentAimDirection = aim.normalized;
            }
            else
            {
                _currentAimDirection = fallbackDirection;
            }
        }
        else
        {
            _currentAimDirection = fallbackDirection;
        }

        if (_controller == null)
        {
            return;
        }

        // 가로 조준값이 충분할 때만 비주얼 방향을 외부 강제 값으로 덮어씁니다.
        if (Mathf.Abs(_currentAimDirection.x) >= aimFacingThreshold)
        {
            _controller.SetExternalFacing(_currentAimDirection.x, true);
        }
        else
        {
            _controller.SetExternalFacing(_controller.FacingDirection, false);
        }
    }

    private bool TryGetAimWorldPosition(out Vector2 worldPosition)
    {
        // 포인터 좌표를 월드 좌표로 바꿀 수 없으면 조준은 실패로 두고 fallback 방향을 씁니다.
        worldPosition = Vector2.zero;
        if (_mainCamera == null)
        {
            return false;
        }

        if (!GameInput.Instance.TryGetPointerScreenPosition(out Vector2 screenPosition))
        {
            return false;
        }

        Vector3 world = _mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -_mainCamera.transform.position.z));
        worldPosition = world;
        return true;
    }

    private bool ReadAttackPressed()
    {
        return GameInput.Instance.AttackPressed;
    }

    private bool ReadWeaponSwapPressed()
    {
        return GameInput.Instance.PreviousWeaponPressed || GameInput.Instance.NextWeaponPressed;
    }

    private Vector2 GetWeaponOriginPosition()
    {
        return ResolveMirroredLocalPoint(weaponOrigin, swordOriginOffset);
    }

    private Vector2 GetMuzzlePosition()
    {
        return ResolveMirroredLocalPoint(muzzle, gunMuzzleOffset);
    }

    private Vector2 ResolveMirroredLocalPoint(Transform reference, Vector2 fallbackOffset)
    {
        Vector2 localOffset = reference != null
            ? (Vector2)reference.localPosition
            : fallbackOffset;

        float facingSign = _controller != null && _controller.FacingDirection < 0f ? -1f : 1f;
        if (Mathf.Abs(_currentAimDirection.x) >= aimFacingThreshold)
        {
            facingSign = Mathf.Sign(_currentAimDirection.x);
        }

        localOffset.x = Mathf.Abs(localOffset.x) * facingSign;
        return transform.TransformPoint(localOffset);
    }

    private void SpawnSwordSlashVisual(Vector2 origin, Vector2 direction)
    {
        SpawnSwordSectorVisual(origin, direction);

        Vector2 position = origin + direction * (swordRange * 0.68f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector3 scale = new Vector3(Mathf.Max(0.9f, swordRange * 2.1f), Mathf.Max(0.16f, swordRange * 0.28f), 1f);
        Color color = swordVisualColor;
        color.a = 1f;
        SpawnTransientVisual("SwordSlash", RuntimeSpriteUtility.WhiteSprite, position, angle, scale, color, Mathf.Max(0.2f, swordVisualDuration), 46);
    }

    private void SpawnGunMuzzleVisual(Vector2 position, Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector3 scale = new Vector3(0.55f, 0.24f, 1f);
        Color color = gunVisualColor;
        color.a = Mathf.Max(0.95f, color.a);
        SpawnTransientVisual("GunMuzzleFlash", RuntimeSpriteUtility.WhiteSprite, position, angle, scale, color, Mathf.Max(0.1f, gunMuzzleVisualDuration), 48);
        SpawnGunTrailVisual(position, direction);
    }

    private void SpawnGunTrailVisual(Vector2 position, Vector2 direction)
    {
        Vector2 trailEnd = position + direction * 1.2f;
        Vector2 center = (position + trailEnd) * 0.5f;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float length = Vector2.Distance(position, trailEnd);
        Color color = gunVisualColor;
        color.a = 0.8f;
        SpawnTransientVisual(
            "GunTrail",
            RuntimeSpriteUtility.WhiteSprite,
            center,
            angle,
            new Vector3(length, 0.08f, 1f),
            color,
            0.08f,
            44);
    }

    private Component ResolveDamageTarget(Collider2D hit)
    {
        if (hit == null)
        {
            return null;
        }

        BossInteraction boss = hit.GetComponentInParent<BossInteraction>();
        if (boss != null)
        {
            return boss;
        }

        return hit.GetComponentInParent<EnemyInteraction>();
    }

    private void ApplyHit(Component target, float damage, Vector2 knockback)
    {
        switch (target)
        {
            case BossInteraction boss:
                boss.ReceiveHit(damage, knockback, gameObject);
                break;
            case EnemyInteraction enemy:
                enemy.ReceiveHit(damage, knockback, gameObject);
                break;
        }
    }

    private void SpawnSwordSectorVisual(Vector2 origin, Vector2 direction)
    {
        GameObject visualObject = new GameObject("SwordArc");
        visualObject.transform.position = origin;
        visualObject.transform.rotation = Quaternion.identity;
        Color color = swordVisualColor;
        color.a = 0.95f;
        LineRenderer line = visualObject.AddComponent<LineRenderer>();
        ConfigureSwordArcLine(line, color, 0.16f, 52);
        line.positionCount = BuildSectorLinePoints(origin, direction, swordRange, swordArcAngle, 20, true, line);
        Destroy(visualObject, Mathf.Max(0.14f, swordVisualDuration));
    }

    private void EnsureSwordPreview()
    {
        // 조준 미리보기는 필요할 때만 지연 생성해 에디터와 플레이 양쪽에서 재사용합니다.
        if (_swordPreviewObject != null)
        {
            return;
        }

        _swordPreviewObject = new GameObject("SwordAimPreview");
        _swordPreviewObject.transform.SetParent(transform, false);
        _swordPreviewLine = _swordPreviewObject.AddComponent<LineRenderer>();
        ConfigureSwordArcLine(_swordPreviewLine, swordPreviewColor, 0.09f, 60);
        _swordPreviewObject.SetActive(false);
    }

    private void UpdateSwordPreview()
    {
        // 미리보기는 검 사용 중일 때만 켜고, 현재 조준 방향을 부채꼴로 다시 그립니다.
        if (!showSwordAimPreview)
        {
            if (_swordPreviewObject != null)
            {
                _swordPreviewObject.SetActive(false);
            }
            return;
        }

        EnsureSwordPreview();
        bool shouldShow = _currentWeapon == PlayerWeaponType.Sword;
        _swordPreviewObject.SetActive(shouldShow);
        if (!shouldShow)
        {
            return;
        }

        Vector2 origin = GetWeaponOriginPosition();
        _swordPreviewLine.startColor = swordPreviewColor;
        _swordPreviewLine.endColor = swordPreviewColor;
        _swordPreviewLine.positionCount = BuildSectorLinePoints(origin, _currentAimDirection, swordRange, swordArcAngle, 24, true, _swordPreviewLine);
    }

    private void ConfigureSwordArcLine(LineRenderer line, Color color, float width, int sortingOrder)
    {
        line.useWorldSpace = true;
        line.loop = false;
        line.textureMode = LineTextureMode.Stretch;
        line.alignment = LineAlignment.View;
        line.widthMultiplier = width;
        line.numCapVertices = 6;
        line.numCornerVertices = 4;
        line.startColor = color;
        line.endColor = color;
        line.sortingLayerName = "Effect";
        line.sortingOrder = sortingOrder;

        if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
        {
            line.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
        }
    }

    private int BuildSectorLinePoints(
        Vector2 origin,
        Vector2 direction,
        float radius,
        float arcAngle,
        int segments,
        bool closeToCenter,
        LineRenderer line)
    {
        // 중심점에서 시작하는 부채꼴 선분 배열을 만들어 라인 렌더러에 그대로 넘깁니다.
        Vector2 normalizedDirection = direction.sqrMagnitude <= 0.0001f ? Vector2.right : direction.normalized;
        float centerAngle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg;
        float startAngle = centerAngle - arcAngle * 0.5f;
        int pointCount = closeToCenter ? segments + 3 : segments + 1;
        Vector3[] positions = new Vector3[pointCount];
        int index = 0;

        if (closeToCenter)
        {
            positions[index++] = origin;
        }

        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + arcAngle * (i / (float)segments);
            float radians = angle * Mathf.Deg2Rad;
            positions[index++] = origin + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
        }

        if (closeToCenter)
        {
            positions[index] = origin;
        }

        line.positionCount = positions.Length;
        line.SetPositions(positions);
        return positions.Length;
    }

    private void SpawnTransientVisual(string name, Sprite sprite, Vector2 position, float rotationZ, Vector3 scale, Color color, float duration, int sortingOrder = 40)
    {
        GameObject visualObject = new GameObject(name);
        visualObject.transform.position = position;
        visualObject.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
        visualObject.transform.localScale = scale;

        SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerName = "Effect";
        renderer.sortingOrder = sortingOrder;
        if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
        {
            renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
        }

        Destroy(visualObject, Mathf.Max(0.01f, duration));
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 origin = Application.isPlaying ? GetWeaponOriginPosition() : (Vector2)transform.TransformPoint(swordOriginOffset);
        Vector2 direction = Application.isPlaying
            ? _currentAimDirection
            : Vector2.right;

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = Vector2.right;
        }

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.85f);
        Gizmos.DrawWireSphere(origin, swordRange);

        Quaternion leftRotation = Quaternion.Euler(0f, 0f, swordArcAngle * 0.5f);
        Quaternion rightRotation = Quaternion.Euler(0f, 0f, -swordArcAngle * 0.5f);
        Vector2 leftEdge = leftRotation * direction * swordRange;
        Vector2 rightEdge = rightRotation * direction * swordRange;
        Gizmos.DrawLine(origin, origin + direction * swordRange);
        Gizmos.DrawLine(origin, origin + leftEdge);
        Gizmos.DrawLine(origin, origin + rightEdge);

        Vector2 muzzlePosition = Application.isPlaying ? GetMuzzlePosition() : (Vector2)transform.TransformPoint(gunMuzzleOffset);
        Gizmos.color = new Color(1f, 0.45f, 0.15f, 0.85f);
        Gizmos.DrawLine(muzzlePosition, muzzlePosition + direction * 1.1f);
        Gizmos.DrawWireSphere(muzzlePosition, gunProjectileRadius);
    }
}
