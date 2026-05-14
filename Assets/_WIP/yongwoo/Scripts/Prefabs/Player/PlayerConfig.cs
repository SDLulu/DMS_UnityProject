using UnityEngine;

// 역할:
// - 플레이어 이동, 공격, 체력, 콜라이더, 카메라 수치를 한 묶음 설정으로 보관합니다.
// - 런타임 적용 전 기본값과 안전 범위를 보정하는 로더를 함께 제공합니다.
//
// 구조 포인트:
// - PlayerRuntimeConfig와 개별 플레이어 컴포넌트 사이의 공통 데이터 계약입니다.

[System.Serializable]
public class PlayerConfig
{
    public PlayerMovementConfig movement = new PlayerMovementConfig();
    public PlayerAttackConfig attack = new PlayerAttackConfig();
    public PlayerHealthConfig health = new PlayerHealthConfig();
    public PlayerColliderConfig collider = new PlayerColliderConfig();
    public PlayerCameraConfig camera = new PlayerCameraConfig();
}

[System.Serializable]
// 이동, 점프, 대시, 구르기처럼 이동 계층에만 필요한 수치 묶음입니다.
public class PlayerMovementConfig
{
    public int configVersion = 2;
    public float groundMoveSpeed = 6.25f;
    public float airMoveSpeed = 6f;
    public float groundAcceleration = 72f;
    public float groundDeceleration = 84f;
    public float airAcceleration = 54f;
    public float airDeceleration = 42f;
    public float turnaroundAccelerationMultiplier = 1.65f;
    public float jumpForce = 8.6f;
    public int extraAirJumps = 1;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.12f;
    public float fallGravityMultiplier = 2.35f;
    public float jumpCutGravityMultiplier = 2.9f;
    public float apexGravityMultiplier = 0.92f;
    public float apexMoveSpeedMultiplier = 1.08f;
    public float apexThreshold = 1.2f;
    public float maxFallSpeed = 18f;
    public float groundedStickForce = 1.5f;
    public float gravityScale = 3f;
    public float groundCheckRadius = 0.18f;
    public float dashSpeed = 12f;
    public float dashMaxDistance = 3.5f;
    public float dashDuration = 0.14f;
    public float dashCooldown = 2f;
    public float rollSpeed = 8.5f;
    public float rollDuration = 0.36f;
}

[System.Serializable]
// 현재 플레이어 전투 계층이 지원하는 무기 종류입니다.
public enum PlayerWeaponType
{
    Sword,
    Gun
}

[System.Serializable]
// 근접/원거리 공격과 무기 시각화에 필요한 수치 묶음입니다.
public class PlayerAttackConfig
{
    public PlayerWeaponType defaultWeapon = PlayerWeaponType.Sword;
    public float attackAnimationDuration = 0.18f;
    public float aimFacingThreshold = 0.18f;

    public Vector2 swordOriginOffset = new Vector2(0.22f, -0.28f);
    public float swordDamage = 1f;
    public float swordCooldown = 0.28f;
    public float swordRange = 1.1f;
    public float swordArcAngle = 90f;
    public float swordKnockbackX = 6f;
    public float swordKnockbackY = 2.5f;
    public float swordVisualDuration = 0.12f;
    public SerializableColor swordVisualColor = new SerializableColor(1f, 0.9f, 0.2f, 0.95f);

    public Vector2 gunMuzzleOffset = new Vector2(0.28f, -0.28f);
    public float gunDamage = 1f;
    public float gunCooldown = 0.18f;
    public float gunProjectileSpeed = 15f;
    public float gunProjectileLifetime = 1.2f;
    public float gunProjectileRadius = 0.1f;
    public float gunKnockbackX = 5f;
    public float gunKnockbackY = 1.2f;
    public float gunMuzzleVisualDuration = 0.06f;
    public SerializableColor gunVisualColor = new SerializableColor(1f, 0.6f, 0.2f, 0.85f);
}

[System.Serializable]
// 공통 체력 컴포넌트에 주입할 플레이어 전용 체력/피격 설정입니다.
public class PlayerHealthConfig
{
    public float maxHealth = 5f;
    public float invulnerabilityDuration = 0.08f;
    public float flashDuration = 0.08f;
    public float respawnDelay = 0.75f;
    public SerializableColor normalColor = new SerializableColor(1f, 1f, 1f, 1f);
    public SerializableColor damageFlashColor = new SerializableColor(1f, 1f, 1f, 1f);
    public SerializableColor deadTint = new SerializableColor(0.2f, 0.2f, 0.25f, 1f);
}

[System.Serializable]
// 플레이어 본체 충돌체 크기와 오프셋을 저장하는 설정 묶음입니다.
public class PlayerColliderConfig
{
    public float width = 1f;
    public float height = 1f;
    public float offsetX = 0f;
    public float offsetY = 0f;
    public bool isTrigger = false;
}

[System.Serializable]
// 기본 플레이 카메라 추적 계층에 주입할 오프셋과 속도 설정입니다.
public class PlayerCameraConfig
{
    public SerializableVector3 offset = new SerializableVector3(0f, 1f, -10f);
    public float followSpeed = 7.5f;
    public float horizontalLookAhead = 1.45f;
    public float lookAheadSmoothing = 8f;
    public float verticalFollowSpeed = 5.5f;
}

[System.Serializable]
// 설정 파일 안에서 Vector3를 직렬화하기 위한 경량 래퍼입니다.
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[System.Serializable]
// 설정 파일 안에서 Unity Color를 직렬화하기 위한 경량 래퍼입니다.
public struct SerializableColor
{
    public float r;
    public float g;
    public float b;
    public float a;

    public SerializableColor(float r, float g, float b, float a = 1f)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public Color ToColor()
    {
        return new Color(r, g, b, a);
    }
}

// PlayerConfig의 기본값 생성과 범위 보정을 담당하는 로더입니다.
public static class PlayerConfigLoader
{
    public static PlayerConfig DeepClone(PlayerConfig source)
    {
        if (source == null)
        {
            return CreateDefault();
        }

        return Sanitize(JsonUtility.FromJson<PlayerConfig>(JsonUtility.ToJson(source)));
    }

    public static PlayerConfig Sanitize(PlayerConfig config)
    {
        config ??= new PlayerConfig();
        config.movement ??= new PlayerMovementConfig();
        config.attack ??= new PlayerAttackConfig();
        config.health ??= new PlayerHealthConfig();
        config.collider ??= new PlayerColliderConfig();
        config.camera ??= new PlayerCameraConfig();

        if (config.movement.configVersion < 1)
        {
            config.movement.extraAirJumps = 1;
            config.movement.configVersion = 1;
        }

        if (config.movement.configVersion < 2)
        {
            config.movement.dashMaxDistance = 3.5f;
            config.movement.configVersion = 2;
        }

        config.movement.groundMoveSpeed = Mathf.Max(0f, config.movement.groundMoveSpeed);
        config.movement.airMoveSpeed = Mathf.Max(0f, config.movement.airMoveSpeed);
        config.movement.groundAcceleration = Mathf.Max(0f, config.movement.groundAcceleration);
        config.movement.groundDeceleration = Mathf.Max(0f, config.movement.groundDeceleration);
        config.movement.airAcceleration = Mathf.Max(0f, config.movement.airAcceleration);
        config.movement.airDeceleration = Mathf.Max(0f, config.movement.airDeceleration);
        config.movement.turnaroundAccelerationMultiplier = Mathf.Max(0f, config.movement.turnaroundAccelerationMultiplier);
        config.movement.jumpForce = Mathf.Max(0f, config.movement.jumpForce);
        config.movement.extraAirJumps = Mathf.Max(0, config.movement.extraAirJumps);
        config.movement.coyoteTime = Mathf.Max(0f, config.movement.coyoteTime);
        config.movement.jumpBufferTime = Mathf.Max(0f, config.movement.jumpBufferTime);
        config.movement.fallGravityMultiplier = Mathf.Max(0.1f, config.movement.fallGravityMultiplier);
        config.movement.jumpCutGravityMultiplier = Mathf.Max(0.1f, config.movement.jumpCutGravityMultiplier);
        config.movement.apexGravityMultiplier = Mathf.Max(0.1f, config.movement.apexGravityMultiplier);
        config.movement.apexMoveSpeedMultiplier = Mathf.Max(0f, config.movement.apexMoveSpeedMultiplier);
        config.movement.apexThreshold = Mathf.Max(0.01f, config.movement.apexThreshold);
        config.movement.maxFallSpeed = Mathf.Max(0.5f, config.movement.maxFallSpeed);
        config.movement.groundedStickForce = Mathf.Max(0f, config.movement.groundedStickForce);
        config.movement.gravityScale = Mathf.Max(0.1f, config.movement.gravityScale);
        config.movement.groundCheckRadius = Mathf.Max(0.01f, config.movement.groundCheckRadius);
        config.movement.dashSpeed = Mathf.Max(0.1f, config.movement.dashSpeed);
        config.movement.dashMaxDistance = Mathf.Max(0.1f, config.movement.dashMaxDistance);
        config.movement.dashDuration = Mathf.Max(0.01f, config.movement.dashDuration);
        config.movement.dashCooldown = Mathf.Max(0f, config.movement.dashCooldown);
        config.movement.rollSpeed = Mathf.Max(0.1f, config.movement.rollSpeed);
        config.movement.rollDuration = Mathf.Max(0.01f, config.movement.rollDuration);

        config.attack.attackAnimationDuration = Mathf.Max(0.01f, config.attack.attackAnimationDuration);
        config.attack.aimFacingThreshold = Mathf.Max(0.01f, config.attack.aimFacingThreshold);
        config.attack.swordRange = Mathf.Max(0.05f, config.attack.swordRange);
        config.attack.swordArcAngle = Mathf.Clamp(config.attack.swordArcAngle, 1f, 180f);
        config.attack.swordDamage = Mathf.Max(0f, config.attack.swordDamage);
        config.attack.swordCooldown = Mathf.Max(0.01f, config.attack.swordCooldown);
        config.attack.swordVisualDuration = Mathf.Max(0.01f, config.attack.swordVisualDuration);
        config.attack.gunDamage = Mathf.Max(0f, config.attack.gunDamage);
        config.attack.gunCooldown = Mathf.Max(0.01f, config.attack.gunCooldown);
        config.attack.gunProjectileSpeed = Mathf.Max(0.1f, config.attack.gunProjectileSpeed);
        config.attack.gunProjectileLifetime = Mathf.Max(0.05f, config.attack.gunProjectileLifetime);
        config.attack.gunProjectileRadius = Mathf.Max(0.02f, config.attack.gunProjectileRadius);
        config.attack.gunMuzzleVisualDuration = Mathf.Max(0.01f, config.attack.gunMuzzleVisualDuration);

        config.health.maxHealth = Mathf.Max(1f, config.health.maxHealth);
        config.health.invulnerabilityDuration = Mathf.Max(0f, config.health.invulnerabilityDuration);
        config.health.flashDuration = Mathf.Max(0f, config.health.flashDuration);
        config.health.respawnDelay = Mathf.Max(0.05f, config.health.respawnDelay);

        config.collider.width = Mathf.Max(0.05f, config.collider.width);
        config.collider.height = Mathf.Max(0.05f, config.collider.height);

        config.camera.followSpeed = Mathf.Max(0f, config.camera.followSpeed);
        config.camera.horizontalLookAhead = Mathf.Max(0f, config.camera.horizontalLookAhead);
        config.camera.lookAheadSmoothing = Mathf.Max(0f, config.camera.lookAheadSmoothing);
        config.camera.verticalFollowSpeed = Mathf.Max(0f, config.camera.verticalFollowSpeed);

        return config;
    }

    public static PlayerConfig CreateDefault()
    {
        return new PlayerConfig();
    }
}
