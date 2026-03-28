using System;
using UnityEngine;

[Serializable]
public class PrototypeBossConfig
{
    public BossCoreConfig core = new BossCoreConfig();
    public BossPhaseConfig[] phases = Array.Empty<BossPhaseConfig>();
}

[Serializable]
public class BossCoreConfig
{
    public string bossName = "Neon Executioner";
    public float maxHealth = 14f;
    public float contactDamage = 1f;
    public float contactKnockback = 6f;
    public float contactInterval = 0.4f;
    public float idleMoveSpeed = 2.5f;
    public float arenaLeft = -7f;
    public float arenaRight = 7f;
    public float groundY = -0.35f;
    public float preferredDistance = 4.5f;
    public float attackDecisionInterval = 0.2f;
    public float bodyColliderWidth = 0.3f;
    public float bodyColliderHeight = 0.46f;
    public float bodyColliderOffsetX = 0f;
    public float bodyColliderOffsetY = 0f;
    public SerializableColor normalColor = new SerializableColor(0.33f, 0.78f, 1f, 1f);
    public SerializableColor telegraphColor = new SerializableColor(1f, 0.42f, 0.25f, 1f);
    public SerializableColor deadColor = new SerializableColor(0.22f, 0.22f, 0.28f, 1f);
}

[Serializable]
public class BossPhaseConfig
{
    public string name = "Opening";
    public float healthThreshold = 1f;
    public float moveSpeedMultiplier = 1f;
    public float cooldownMultiplier = 1f;
    public SerializableColor phaseColor = new SerializableColor(0.33f, 0.78f, 1f, 1f);
    public BossPatternConfig[] patterns = Array.Empty<BossPatternConfig>();
}

[Serializable]
public class BossPatternConfig
{
    public string name = "Dash Slash";
    public string type = "DashStrike";
    public bool enabled = true;
    public float selectionWeight = 1f;
    public float minDistance = 0f;
    public float maxDistance = 99f;
    public float cooldown = 2f;
    public float telegraphDuration = 0.4f;
    public float executeDuration = 0.3f;
    public float recoveryDuration = 0.5f;
    public float damage = 1f;
    public float knockback = 6f;
    public float dashSpeed = 9f;
    public float dashHitWidth = 1.8f;
    public float dashHitHeight = 1.5f;
    public float leapHeight = 3f;
    public float landingRadius = 1.5f;
    public float landingOffset = 0f;
    public int projectileCount = 5;
    public float projectileSpreadAngle = 55f;
    public float projectileSpeed = 6f;
    public float projectileLifetime = 3f;
    public float projectileRadius = 0.32f;
    public float projectileSpawnX = 0.8f;
    public float projectileSpawnY = 0.8f;
    public int volleyBursts = 1;
    public float volleySpacing = 0.15f;
}

[Serializable]
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

public static class PrototypeBossConfigLoader
{
    public static PrototypeBossConfig DeepClone(PrototypeBossConfig source)
    {
        if (source == null)
        {
            return CreateDefault();
        }

        return Sanitize(JsonUtility.FromJson<PrototypeBossConfig>(JsonUtility.ToJson(source)));
    }

    public static PrototypeBossConfig Sanitize(PrototypeBossConfig config)
    {
        if (config == null)
        {
            return CreateDefault();
        }

        config.core ??= new BossCoreConfig();
        config.phases ??= Array.Empty<BossPhaseConfig>();
        if (config.phases.Length == 0)
        {
            return CreateDefault();
        }

        for (int i = 0; i < config.phases.Length; i++)
        {
            config.phases[i] ??= new BossPhaseConfig();
            config.phases[i].patterns ??= Array.Empty<BossPatternConfig>();

            for (int j = 0; j < config.phases[i].patterns.Length; j++)
            {
                config.phases[i].patterns[j] ??= new BossPatternConfig();
            }
        }

        return config;
    }

    public static PrototypeBossConfig CreateDefault()
    {
        return new PrototypeBossConfig
        {
            core = new BossCoreConfig(),
            phases = new[]
            {
                new BossPhaseConfig
                {
                    name = "Opening",
                    healthThreshold = 1f,
                    moveSpeedMultiplier = 1f,
                    cooldownMultiplier = 1f,
                    phaseColor = new SerializableColor(0.33f, 0.78f, 1f, 1f),
                    patterns = new[]
                    {
                        new BossPatternConfig
                        {
                            name = "Dash Slash",
                            type = "DashStrike"
                        },
                        new BossPatternConfig
                        {
                            name = "Leap Slam",
                            type = "LeapSlam",
                            cooldown = 4.5f,
                            telegraphDuration = 0.6f,
                            executeDuration = 0.75f,
                            recoveryDuration = 0.65f,
                            damage = 1.5f,
                            knockback = 8f,
                            minDistance = 2f,
                            leapHeight = 3.25f,
                            landingRadius = 1.7f
                        },
                        new BossPatternConfig
                        {
                            name = "Burst Fan",
                            type = "ProjectileFan",
                            cooldown = 3.5f,
                            telegraphDuration = 0.7f,
                            executeDuration = 0.45f,
                            recoveryDuration = 0.4f,
                            minDistance = 3f,
                            damage = 0.75f,
                            knockback = 4f,
                            projectileCount = 5,
                            projectileSpreadAngle = 55f,
                            projectileSpeed = 6f,
                            projectileLifetime = 3f
                        }
                    }
                },
                new BossPhaseConfig
                {
                    name = "Desperation",
                    healthThreshold = 0.5f,
                    moveSpeedMultiplier = 1.2f,
                    cooldownMultiplier = 0.82f,
                    phaseColor = new SerializableColor(1f, 0.35f, 0.35f, 1f),
                    patterns = new[]
                    {
                        new BossPatternConfig
                        {
                            name = "Cross Dash",
                            type = "DashStrike",
                            cooldown = 1.6f,
                            telegraphDuration = 0.3f,
                            executeDuration = 0.32f,
                            recoveryDuration = 0.35f,
                            damage = 1.25f,
                            knockback = 7f,
                            dashSpeed = 12f
                        },
                        new BossPatternConfig
                        {
                            name = "Meteor Leap",
                            type = "LeapSlam",
                            cooldown = 3.8f,
                            telegraphDuration = 0.45f,
                            executeDuration = 0.6f,
                            recoveryDuration = 0.45f,
                            damage = 1.75f,
                            knockback = 9f,
                            minDistance = 1.5f,
                            leapHeight = 3.6f,
                            landingRadius = 2f
                        },
                        new BossPatternConfig
                        {
                            name = "Scarlet Fan",
                            type = "ProjectileFan",
                            cooldown = 2.8f,
                            telegraphDuration = 0.45f,
                            executeDuration = 0.65f,
                            recoveryDuration = 0.3f,
                            minDistance = 2.5f,
                            projectileCount = 7,
                            projectileSpreadAngle = 70f,
                            projectileSpeed = 7.5f,
                            projectileLifetime = 3.2f,
                            volleyBursts = 2,
                            volleySpacing = 0.18f
                        }
                    }
                }
            }
        };
    }
}
