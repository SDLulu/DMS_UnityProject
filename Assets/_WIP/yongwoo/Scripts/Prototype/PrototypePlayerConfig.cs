using UnityEngine;

[System.Serializable]
public class PrototypePlayerConfig
{
    public PlayerMovementConfig movement = new PlayerMovementConfig();
    public PlayerAttackConfig attack = new PlayerAttackConfig();
    public PlayerHealthConfig health = new PlayerHealthConfig();
    public PlayerColliderConfig collider = new PlayerColliderConfig();
    public PlayerCameraConfig camera = new PlayerCameraConfig();
}

[System.Serializable]
public class PlayerMovementConfig
{
    public float groundMoveSpeed = 6.25f;
    public float airMoveSpeed = 6f;
    public float groundAcceleration = 72f;
    public float groundDeceleration = 84f;
    public float airAcceleration = 54f;
    public float airDeceleration = 42f;
    public float turnaroundAccelerationMultiplier = 1.65f;
    public float jumpForce = 8.6f;
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
    public float dashDuration = 0.14f;
    public float dashCooldown = 2f;
    public float rollSpeed = 8.5f;
    public float rollDuration = 0.36f;
}

[System.Serializable]
public class PlayerAttackConfig
{
    public float attackDamage = 1f;
    public float attackCooldown = 0.28f;
    public float attackAnimationDuration = 0.18f;
    public Vector2 attackSize = new Vector2(1.2f, 0.9f);
    public Vector2 attackOffset = new Vector2(0.95f, 0f);
    public float attackKnockbackX = 6f;
    public float attackKnockbackY = 2.5f;
    public bool showAttackVisual = true;
    public float attackVisualDuration = 0.12f;
    public SerializableColor attackVisualColor = new SerializableColor(1f, 0.9f, 0.2f, 0.28f);
}

[System.Serializable]
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
public class PlayerColliderConfig
{
    public float width = 1f;
    public float height = 1f;
    public float offsetX = 0f;
    public float offsetY = 0f;
    public bool isTrigger = false;
}

[System.Serializable]
public class PlayerCameraConfig
{
    public SerializableVector3 offset = new SerializableVector3(0f, 1f, -10f);
    public float followSpeed = 7.5f;
    public float horizontalLookAhead = 1.45f;
    public float lookAheadSmoothing = 8f;
    public float verticalFollowSpeed = 5.5f;
}

[System.Serializable]
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

public static class PrototypePlayerConfigLoader
{
    public static PrototypePlayerConfig DeepClone(PrototypePlayerConfig source)
    {
        if (source == null)
        {
            return CreateDefault();
        }

        return Sanitize(JsonUtility.FromJson<PrototypePlayerConfig>(JsonUtility.ToJson(source)));
    }

    public static PrototypePlayerConfig Sanitize(PrototypePlayerConfig config)
    {
        config ??= new PrototypePlayerConfig();
        config.movement ??= new PlayerMovementConfig();
        config.attack ??= new PlayerAttackConfig();
        config.health ??= new PlayerHealthConfig();
        config.collider ??= new PlayerColliderConfig();
        config.camera ??= new PlayerCameraConfig();

        config.movement.groundMoveSpeed = Mathf.Max(0f, config.movement.groundMoveSpeed);
        config.movement.airMoveSpeed = Mathf.Max(0f, config.movement.airMoveSpeed);
        config.movement.groundAcceleration = Mathf.Max(0f, config.movement.groundAcceleration);
        config.movement.groundDeceleration = Mathf.Max(0f, config.movement.groundDeceleration);
        config.movement.airAcceleration = Mathf.Max(0f, config.movement.airAcceleration);
        config.movement.airDeceleration = Mathf.Max(0f, config.movement.airDeceleration);
        config.movement.turnaroundAccelerationMultiplier = Mathf.Max(0f, config.movement.turnaroundAccelerationMultiplier);
        config.movement.jumpForce = Mathf.Max(0f, config.movement.jumpForce);
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
        config.movement.dashDuration = Mathf.Max(0.01f, config.movement.dashDuration);
        config.movement.dashCooldown = Mathf.Max(0f, config.movement.dashCooldown);
        config.movement.rollSpeed = Mathf.Max(0.1f, config.movement.rollSpeed);
        config.movement.rollDuration = Mathf.Max(0.01f, config.movement.rollDuration);

        config.attack.attackDamage = Mathf.Max(0f, config.attack.attackDamage);
        config.attack.attackCooldown = Mathf.Max(0.01f, config.attack.attackCooldown);
        config.attack.attackAnimationDuration = Mathf.Max(0.01f, config.attack.attackAnimationDuration);
        config.attack.attackSize.x = Mathf.Max(0.05f, config.attack.attackSize.x);
        config.attack.attackSize.y = Mathf.Max(0.05f, config.attack.attackSize.y);
        config.attack.attackVisualDuration = Mathf.Max(0.01f, config.attack.attackVisualDuration);

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

    public static PrototypePlayerConfig CreateDefault()
    {
        return new PrototypePlayerConfig();
    }
}
