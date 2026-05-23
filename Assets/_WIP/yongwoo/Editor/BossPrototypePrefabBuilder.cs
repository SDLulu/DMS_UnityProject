#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// HTML에서 확정한 보스 P1 통합형을 빠르게 Unity 프리팹으로 조립하는 편집기 도구입니다.
// 기존 수동 프리팹을 덮는 용도이므로, 실행 대상은 Boss_P1_Prototype에 한정합니다.

public static class BossPrototypePrefabBuilder
{
    private const string BossDir = "Assets/_WIP/yongwoo/Prefabs/Boss";
    private const string ProjectilePath = BossDir + "/BossProjectile.prefab";
    private const string BossPath = BossDir + "/Boss_P1_Prototype.prefab";
    private const string BossP1SpritePath = "Assets/_WIP/yongwoo/Art/Boss/Runtime_20260523/Sprites/boss_p1_idle.png";
    private const string BossP1IdleFramesPath = "Assets/_WIP/yongwoo/Art/Boss/Runtime_20260523/AnimationSheets/boss_p1_idle_minimal_idle_4f.png";
    private static readonly string[] BossSpritePaths =
    {
        BossP1SpritePath,
        "Assets/_WIP/yongwoo/Art/Boss/Runtime_20260523/Sprites/boss_clone_a_idle.png",
        "Assets/_WIP/yongwoo/Art/Boss/Runtime_20260523/Sprites/boss_clone_b_idle.png",
        "Assets/_WIP/yongwoo/Art/Boss/Runtime_20260523/Sprites/boss_clone_c_idle.png"
    };
    private static readonly string[] BossIdleFramePaths =
    {
        BossP1IdleFramesPath,
        "Assets/_WIP/yongwoo/Art/Boss/Runtime_20260523/AnimationSheets/boss_clone_a_idle_minimal_idle_4f.png",
        "Assets/_WIP/yongwoo/Art/Boss/Runtime_20260523/AnimationSheets/boss_clone_b_idle_minimal_idle_4f.png",
        "Assets/_WIP/yongwoo/Art/Boss/Runtime_20260523/AnimationSheets/boss_clone_c_idle_minimal_idle_4f.png"
    };

    [MenuItem("Tools/Yongwoo/Boss/Rebuild P1 Prototype Prefab")]
    public static void RebuildP1PrototypePrefab()
    {
        EnsureFolder("Assets/_WIP/yongwoo/Prefabs", "Boss");
        CleanupTemporarySceneObjects();
        ConfigureBossSpriteImporters();

        BossProjectile projectilePrefab = BuildProjectilePrefab();
        BuildBossPrefab(projectilePrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BossPrototypePrefabBuilder] Rebuilt {BossPath}");
    }

    private static BossProjectile BuildProjectilePrefab()
    {
        GameObject projectileGo = new GameObject("BossProjectile")
        {
            layer = LayerMask.NameToLayer("Enemy")
        };

        SpriteRenderer projectileRenderer = projectileGo.AddComponent<SpriteRenderer>();
        projectileRenderer.sprite = RuntimeSpriteUtility.CircleSprite;
        projectileRenderer.color = new Color(1f, 0.35f, 0.3f, 0.95f);
        projectileRenderer.sortingLayerName = "Effect";
        projectileRenderer.sortingOrder = 42;

        CircleCollider2D projectileCollider = projectileGo.AddComponent<CircleCollider2D>();
        projectileCollider.isTrigger = true;
        projectileCollider.radius = 0.03f;

        projectileGo.AddComponent<Rigidbody2D>();
        projectileGo.AddComponent<TrailRenderer>();
        BossProjectile projectile = projectileGo.AddComponent<BossProjectile>();
        projectileGo.transform.localScale = Vector3.one * 0.135f;
        SetField(projectile, "defaultRadius", 0.03f);

        PrefabUtility.SaveAsPrefabAsset(projectileGo, ProjectilePath);
        UnityEngine.Object.DestroyImmediate(projectileGo);
        return AssetDatabase.LoadAssetAtPath<BossProjectile>(ProjectilePath);
    }

    private static void BuildBossPrefab(BossProjectile projectilePrefab)
    {
        GameObject boss = new GameObject("Boss_P1_Prototype")
        {
            layer = LayerMask.NameToLayer("Enemy")
        };

        Rigidbody2D body = boss.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;

        CircleCollider2D collider = boss.AddComponent<CircleCollider2D>();
        collider.radius = 0.62f;
        collider.isTrigger = true;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(boss.transform, false);
        SpriteRenderer bossRenderer = visual.AddComponent<SpriteRenderer>();
        bossRenderer.sprite = LoadSpriteOrFallback(BossP1SpritePath, RuntimeSpriteUtility.WhiteSprite);
        bossRenderer.color = Color.white;
        bossRenderer.sortingLayerName = "Characters";
        bossRenderer.sortingOrder = 5;
        visual.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

        GameObject muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(boss.transform, false);
        muzzle.transform.localPosition = new Vector3(0.55f, 0f, 0f);

        BossInteraction interaction = boss.AddComponent<BossInteraction>();
        SetField(interaction, "maxHealth", 5);
        SetField(interaction, "spriteRenderer", bossRenderer);

        BossTeleporter teleporter = boss.AddComponent<BossTeleporter>();
        BossPatternRunner runner = boss.AddComponent<BossPatternRunner>();

        BossPatternStraightShot straight = boss.AddComponent<BossPatternStraightShot>();
        SetBaseTiming(straight, 0.55f, 0.12f, 0.45f);
        SetField(straight, "projectilePrefab", projectilePrefab);
        SetField(straight, "muzzle", muzzle.transform);
        SetField(straight, "projectileSpeed", 15f);
        SetField(straight, "projectileLifetime", 3f);
        SetField(straight, "damage", 1f);
        SetField(straight, "telegraphVisual", MakeTelegraph("Telegraph_Straight", boss.transform, new Color(1f, 0.1f, 0.12f, 0.45f), new Vector2(8f, 0.05f)));

        BossPatternVolley volley = boss.AddComponent<BossPatternVolley>();
        SetBaseTiming(volley, 0.65f, 0.08f, 0.5f);
        SetField(volley, "projectilePrefab", projectilePrefab);
        SetField(volley, "muzzle", muzzle.transform);
        SetField(volley, "shotCount", 4);
        SetField(volley, "interShotInterval", 0.13f);
        SetField(volley, "projectileSpeed", 13f);
        SetField(volley, "projectileLifetime", 3f);
        SetField(volley, "damage", 1f);
        SetField(volley, "telegraphVisual", MakeTelegraph("Telegraph_Volley", boss.transform, new Color(1f, 0.78f, 0.25f, 0.45f), new Vector2(1.5f, 0.16f)));

        BossPatternSpread spread = boss.AddComponent<BossPatternSpread>();
        SetBaseTiming(spread, 0.8f, 0.12f, 0.6f);
        SetField(spread, "projectilePrefab", projectilePrefab);
        SetField(spread, "muzzle", muzzle.transform);
        SetField(spread, "shotCount", 5);
        SetField(spread, "totalSpreadDegrees", 120f);
        SetField(spread, "projectileSpeed", 10.5f);
        SetField(spread, "projectileLifetime", 3f);
        SetField(spread, "damage", 1f);
        SetField(spread, "telegraphVisual", MakeTelegraph("Telegraph_Spread", boss.transform, new Color(1f, 0.35f, 0.1f, 0.35f), new Vector2(2f, 0.12f)));

        BossPatternDashSlash dash = boss.AddComponent<BossPatternDashSlash>();
        SetBaseTiming(dash, 0.6f, 0.12f, 0.55f);
        SetField(dash, "dashDuration", 0.32f);
        SetField(dash, "dashSpeed", 100f);
        SetField(dash, "hitRadius", 0.95f);
        SetField(dash, "damage", 1f);
        SetField(dash, "targetLayers", new LayerMask { value = LayerMask.GetMask("Player") });
        SetField(dash, "telegraphVisual", MakeTelegraph("Telegraph_DashSlash", boss.transform, new Color(1f, 0.1f, 0.12f, 0.55f), new Vector2(6f, 0.08f)));

        BossPatternDelayedBlast blast = boss.AddComponent<BossPatternDelayedBlast>();
        SetBaseTiming(blast, 0.65f, 0f, 0.6f);
        SetField(blast, "blastCount", 2);
        SetField(blast, "warningDuration", 1f);
        SetField(blast, "activeDuration", 0.28f);
        SetField(blast, "blastRadius", 1.4f);
        SetField(blast, "damage", 1f);
        SetField(blast, "sideOffset", 2.4f);
        SetField(blast, "telegraphVisual", MakeTelegraph("Telegraph_Blast", boss.transform, new Color(1f, 0.78f, 0.25f, 0.25f), new Vector2(1.4f, 1.4f)));

        BossPatternPredictShot predict = boss.AddComponent<BossPatternPredictShot>();
        SetBaseTiming(predict, 0.7f, 0.1f, 0.55f);
        SetField(predict, "projectilePrefab", projectilePrefab);
        SetField(predict, "muzzle", muzzle.transform);
        SetField(predict, "shotCount", 3);
        SetField(predict, "interShotInterval", 0.12f);
        SetField(predict, "projectileSpeed", 12f);
        SetField(predict, "projectileLifetime", 3f);
        SetField(predict, "damage", 1f);
        SetField(predict, "leadDistance", 2.2f);
        SetField(predict, "backDistance", 1.7f);
        SetField(predict, "telegraphVisual", MakeTelegraph("Telegraph_Predict", boss.transform, new Color(0.45f, 0.85f, 1f, 0.35f), new Vector2(2f, 0.1f)));

        BossPatternTeleportSlam slam = boss.AddComponent<BossPatternTeleportSlam>();
        SetBaseTiming(slam, 0.55f, 0.1f, 0.55f);
        SetField(slam, "hitRadius", 1.2f);
        SetField(slam, "activeDuration", 0.22f);
        SetField(slam, "damage", 1f);
        SetField(slam, "telegraphVisual", MakeTelegraph("Telegraph_Slam", boss.transform, new Color(1f, 0.1f, 0.12f, 0.28f), new Vector2(1.28f, 1.28f)));

        BossPatternLaserWall laser = boss.AddComponent<BossPatternLaserWall>();
        SetBaseTiming(laser, 0.75f, 0.1f, 0.6f);
        SetField(laser, "wallLength", 18f);
        SetField(laser, "wallWidth", 1.52f);
        SetField(laser, "warningDuration", 0.12f);
        SetField(laser, "activeDuration", 0.45f);
        SetField(laser, "damage", 1f);

        BossPatternSafeZoneCollapse safeZone = boss.AddComponent<BossPatternSafeZoneCollapse>();
        SetBaseTiming(safeZone, 0.8f, 0f, 0.7f);
        SetField(safeZone, "safeRadius", 2.2f);
        SetField(safeZone, "warningDuration", 0.8f);
        SetField(safeZone, "activeDuration", 1.1f);
        SetField(safeZone, "damage", 1f);

        BossPhaseController phaseController = boss.AddComponent<BossPhaseController>();
        ConfigurePhaseController(phaseController, interaction, teleporter, runner, bossRenderer);

        SetField(runner, "interaction", interaction);
        SetField(runner, "teleporter", teleporter);
        SetField(runner, "patternSlots", new MonoBehaviour[] { straight, volley, spread, dash, blast, predict });
        SetField(runner, "interPatternDelay", 0.2f);
        SetField(runner, "teleportBetweenPatterns", true);
        SetField(runner, "autoStart", true);
        SetField(runner, "startupDelay", 0.5f);

        PrefabUtility.SaveAsPrefabAsset(boss, BossPath);
        UnityEngine.Object.DestroyImmediate(boss);
    }

    private static void ConfigurePhaseController(
        BossPhaseController phaseController,
        BossInteraction interaction,
        BossTeleporter teleporter,
        BossPatternRunner runner,
        SpriteRenderer visualRenderer)
    {
        SetField(phaseController, "interaction", interaction);
        SetField(phaseController, "teleporter", teleporter);
        SetField(phaseController, "runner", runner);
        SetField(phaseController, "visualRenderer", visualRenderer);
        SetField(phaseController, "p1Sprite", LoadSpriteOrFallback(BossSpritePaths[0], RuntimeSpriteUtility.WhiteSprite));
        SetField(phaseController, "cloneASprite", LoadSpriteOrFallback(BossSpritePaths[1], RuntimeSpriteUtility.WhiteSprite));
        SetField(phaseController, "cloneBSprite", LoadSpriteOrFallback(BossSpritePaths[2], RuntimeSpriteUtility.WhiteSprite));
        SetField(phaseController, "cloneCSprite", LoadSpriteOrFallback(BossSpritePaths[3], RuntimeSpriteUtility.WhiteSprite));
        SetField(phaseController, "p1IdleFrames", LoadSpriteFrames(BossIdleFramePaths[0]));
        SetField(phaseController, "cloneAIdleFrames", LoadSpriteFrames(BossIdleFramePaths[1]));
        SetField(phaseController, "cloneBIdleFrames", LoadSpriteFrames(BossIdleFramePaths[2]));
        SetField(phaseController, "cloneCIdleFrames", LoadSpriteFrames(BossIdleFramePaths[3]));
    }

    private static GameObject MakeTelegraph(string name, Transform parent, Color color, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteUtility.WhiteSprite;
        renderer.color = color;
        renderer.sortingLayerName = "Effect";
        renderer.sortingOrder = 38;
        go.transform.localScale = size;
        go.SetActive(false);
        return go;
    }

    private static void SetBaseTiming(BossPatternBase pattern, float telegraph, float prefire, float recovery)
    {
        if (pattern == null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        SetField(pattern, "telegraphDuration", telegraph);
        SetField(pattern, "prefireDelay", prefire);
        SetField(pattern, "recoveryDelay", recovery);
    }

    private static void SetField(object target, string name, object value)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target), $"Cannot set {name} on a null target.");
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        FieldInfo field = null;
        Type type = target.GetType();
        while (type != null && field == null)
        {
            field = type.GetField(name, flags);
            type = type.BaseType;
        }

        if (field == null)
        {
            throw new MissingFieldException(target.GetType().Name, name);
        }

        field.SetValue(target, value);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string full = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(full))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static Sprite LoadSpriteOrFallback(string path, Sprite fallback)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return sprite != null ? sprite : fallback;
    }

    private static Sprite[] LoadSpriteFrames(string path)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        System.Collections.Generic.List<Sprite> sprites = new();
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
            {
                sprites.Add(sprite);
            }
        }

        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites.ToArray();
    }

    private static void ConfigureBossSpriteImporters()
    {
        for (int i = 0; i < BossSpritePaths.Length; i++)
        {
            ConfigureSpriteImporter(BossSpritePaths[i]);
        }
    }

    private static void ConfigureSpriteImporter(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 320f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void CleanupTemporarySceneObjects()
    {
        string[] names = { "Boss_P1_Prototype", "BossProjectile" };
        for (int i = 0; i < names.Length; i++)
        {
            GameObject[] objects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int j = 0; j < objects.Length; j++)
            {
                GameObject obj = objects[j];
                if (obj != null && obj.scene.IsValid() && obj.name == names[i])
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }
        }
    }
}
#endif
