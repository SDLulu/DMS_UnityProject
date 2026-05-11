using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DeadRevolverStageEnemyBuilder
{
    private const string StageScenePath = "Assets/_Scenes/Yongwoo_Stage.unity";
    private const string PrefabRoot = "Assets/_WIP/yongwoo/Prefabs/Enemy/DeadRevolver";

    private sealed class VariantDefinition
    {
        public VariantDefinition(
            string id,
            DeadRevolverEnemyController.DeadRevolverArchetype archetype,
            float maxHealth,
            float moveSpeed,
            float stopDistance,
            float attackCooldown,
            float attackWindup,
            float attackRecovery,
            float meleeRange,
            Vector2 meleeSize,
            float meleeDamage,
            float knockbackForce,
            float knockbackUpForce,
            float gunRange,
            float projectileSpeed,
            float projectileDamage)
        {
            Id = id;
            Archetype = archetype;
            MaxHealth = maxHealth;
            MoveSpeed = moveSpeed;
            StopDistance = stopDistance;
            AttackCooldown = attackCooldown;
            AttackWindup = attackWindup;
            AttackRecovery = attackRecovery;
            MeleeRange = meleeRange;
            MeleeSize = meleeSize;
            MeleeDamage = meleeDamage;
            KnockbackForce = knockbackForce;
            KnockbackUpForce = knockbackUpForce;
            GunRange = gunRange;
            ProjectileSpeed = projectileSpeed;
            ProjectileDamage = projectileDamage;
        }

        public string Id { get; }
        public DeadRevolverEnemyController.DeadRevolverArchetype Archetype { get; }
        public float MaxHealth { get; }
        public float MoveSpeed { get; }
        public float StopDistance { get; }
        public float AttackCooldown { get; }
        public float AttackWindup { get; }
        public float AttackRecovery { get; }
        public float MeleeRange { get; }
        public Vector2 MeleeSize { get; }
        public float MeleeDamage { get; }
        public float KnockbackForce { get; }
        public float KnockbackUpForce { get; }
        public float GunRange { get; }
        public float ProjectileSpeed { get; }
        public float ProjectileDamage { get; }
    }

    private static readonly VariantDefinition[] Variants =
    {
        new VariantDefinition("Gunner", DeadRevolverEnemyController.DeadRevolverArchetype.Gunner, 3f, 2.4f, 3.4f, 1.1f, 0.18f, 0.2f, 0f, Vector2.zero, 0f, 5f, 1.4f, 5.4f, 10.5f, 1f),
        new VariantDefinition("Swordsman", DeadRevolverEnemyController.DeadRevolverArchetype.Swordsman, 5f, 3.8f, 0.9f, 0.72f, 0.12f, 0.16f, 1.1f, new Vector2(1.1f, 0.72f), 1f, 7f, 2.2f, 0f, 0f, 0f),
        new VariantDefinition("Brawler", DeadRevolverEnemyController.DeadRevolverArchetype.Brawler, 4f, 4.2f, 0.78f, 0.55f, 0.1f, 0.14f, 0.9f, new Vector2(0.9f, 0.68f), 1f, 6.5f, 1.8f, 0f, 0f, 0f),
        new VariantDefinition("ShieldBearer", DeadRevolverEnemyController.DeadRevolverArchetype.ShieldBearer, 7f, 2.8f, 0.92f, 0.95f, 0.16f, 0.22f, 1f, new Vector2(1.05f, 0.78f), 1f, 5.5f, 1.6f, 0f, 0f, 0f)
    };

    [MenuItem("Tools/Yongwoo/Build DeadRevolver Stage Enemies")]
    public static void Build()
    {
        DeadRevolverEnemyShowcaseBuilder.EnsureAssets();

        for (int i = 0; i < Variants.Length; i++)
        {
            UpgradePrefab(Variants[i]);
        }

        PlaceVariantsInStage();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("DeadRevolver stage enemies build complete.");
    }

    private static void UpgradePrefab(VariantDefinition definition)
    {
        string prefabPath = $"{PrefabRoot}/DeadRevolver_{definition.Id}.prefab";
        if (!File.Exists(prefabPath))
        {
            throw new FileNotFoundException($"DeadRevolver prefab missing: {prefabPath}");
        }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            throw new IOException($"Failed to load prefab contents at {prefabPath}");
        }

        try
        {
            root.name = $"DeadRevolver_{definition.Id}";
            root.tag = "Untagged";
            SetLayerRecursively(root, LayerMask.NameToLayer("Enemy"));

            Transform visual = root.transform.Find("Visual");
            if (visual == null)
            {
                throw new InvalidDataException($"Prefab {prefabPath} is missing Visual child.");
            }

            SpriteRenderer visualRenderer = visual.GetComponent<SpriteRenderer>();
            Animator visualAnimator = visual.GetComponent<Animator>();
            bool colliderAdded = root.GetComponent<BoxCollider2D>() == null;
            bool bodyAdded = root.GetComponent<Rigidbody2D>() == null;
            bool interactionAdded = root.GetComponent<BlindHuntressEnemyInteraction>() == null;
            bool controllerAdded = root.GetComponent<DeadRevolverEnemyController>() == null;

            BoxCollider2D collider = EnsureComponent<BoxCollider2D>(root);
            Rigidbody2D body = EnsureComponent<Rigidbody2D>(root);
            BlindHuntressEnemyInteraction interaction = EnsureComponent<BlindHuntressEnemyInteraction>(root);
            DeadRevolverEnemyController controller = EnsureComponent<DeadRevolverEnemyController>(root);

            if (bodyAdded)
            {
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = 1f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            if (colliderAdded)
            {
                collider.size = new Vector2(0.62f, 1.14f);
                collider.offset = new Vector2(0f, -0.55f);
            }

            Transform sensors = GetOrCreateChild(root.transform, "Sensors", out _);
            bool attackOriginCreated;
            Transform attackOrigin = GetOrCreateChild(sensors, "AttackOrigin", out attackOriginCreated);
            bool muzzlePointCreated;
            Transform muzzlePoint = GetOrCreateChild(sensors, "MuzzlePoint", out muzzlePointCreated);

            if (attackOriginCreated)
            {
                attackOrigin.localPosition = new Vector3(0.7f, 0.14f, 0f);
            }

            if (muzzlePointCreated)
            {
                muzzlePoint.localPosition = new Vector3(0.76f, 0.22f, 0f);
            }

            SerializedObject interactionSo = new SerializedObject(interaction);
            interactionSo.FindProperty("spriteRenderer").objectReferenceValue = visualRenderer;
            interactionSo.FindProperty("body").objectReferenceValue = body;
            if (interactionAdded)
            {
                interactionSo.FindProperty("maxHealth").floatValue = definition.MaxHealth;
                interactionSo.FindProperty("invulnerabilityDuration").floatValue = 0.08f;
                interactionSo.FindProperty("flashDuration").floatValue = 0.08f;
                interactionSo.FindProperty("respawnOnDeath").boolValue = false;
            }
            interactionSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("visualRoot").objectReferenceValue = visual;
            controllerSo.FindProperty("visualAnimator").objectReferenceValue = visualAnimator;
            controllerSo.FindProperty("attackOrigin").objectReferenceValue = attackOrigin;
            controllerSo.FindProperty("muzzlePoint").objectReferenceValue = muzzlePoint;
            controllerSo.FindProperty("interaction").objectReferenceValue = interaction;
            controllerSo.FindProperty("body").objectReferenceValue = body;
            if (controllerAdded)
            {
                controllerSo.FindProperty("archetype").enumValueIndex = (int)definition.Archetype;
                controllerSo.FindProperty("detectionRange").floatValue = 8f;
                controllerSo.FindProperty("verticalTolerance").floatValue = 1.9f;
                controllerSo.FindProperty("targetRefreshInterval").floatValue = 0.35f;
                controllerSo.FindProperty("moveSpeed").floatValue = definition.MoveSpeed;
                controllerSo.FindProperty("acceleration").floatValue = 28f;
                controllerSo.FindProperty("stopDistance").floatValue = definition.StopDistance;
                controllerSo.FindProperty("groundLayer").intValue = LayerMask.GetMask("Ground");
                controllerSo.FindProperty("ledgeProbeForward").floatValue = 0.52f;
                controllerSo.FindProperty("ledgeProbeHeight").floatValue = 0.15f;
                controllerSo.FindProperty("ledgeProbeDepth").floatValue = 1.4f;
                controllerSo.FindProperty("invertVisualFacing").boolValue = false;
                controllerSo.FindProperty("attackCooldown").floatValue = definition.AttackCooldown;
                controllerSo.FindProperty("attackWindup").floatValue = definition.AttackWindup;
                controllerSo.FindProperty("attackRecovery").floatValue = definition.AttackRecovery;
                controllerSo.FindProperty("meleeRange").floatValue = definition.MeleeRange;
                controllerSo.FindProperty("meleeHitboxSize").vector2Value = definition.MeleeSize;
                controllerSo.FindProperty("meleeDamage").floatValue = definition.MeleeDamage;
                controllerSo.FindProperty("knockbackForce").floatValue = definition.KnockbackForce;
                controllerSo.FindProperty("knockbackUpForce").floatValue = definition.KnockbackUpForce;
                controllerSo.FindProperty("gunRange").floatValue = definition.GunRange;
                controllerSo.FindProperty("projectileSpeed").floatValue = definition.ProjectileSpeed > 0f ? definition.ProjectileSpeed : 10f;
                controllerSo.FindProperty("projectileLifetime").floatValue = 1.45f;
                controllerSo.FindProperty("projectileDamage").floatValue = definition.ProjectileDamage > 0f ? definition.ProjectileDamage : 1f;
                controllerSo.FindProperty("projectileKnockback").floatValue = 5f;
                controllerSo.FindProperty("projectileRadius").floatValue = 0.08f;
                controllerSo.FindProperty("projectileColor").colorValue = new Color(1f, 0.72f, 0.28f, 1f);
            }
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void PlaceVariantsInStage()
    {
        Scene scene = EditorSceneManager.OpenScene(StageScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            throw new IOException($"Failed to open scene: {StageScenePath}");
        }

        GameObject group = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "DeadRevolverEnemies");
        bool groupCreated = false;
        if (group == null)
        {
            group = new GameObject("DeadRevolverEnemies");
            SceneManager.MoveGameObjectToScene(group, scene);
            groupCreated = true;
        }

        GameObject anchorObject = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "BlindHuntressEnemy")
            ?? scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Player");

        Vector3 anchorPosition = anchorObject != null ? anchorObject.transform.position : new Vector3(4f, -1f, 0f);
        float? anchorGroundY = SampleGroundHeight(anchorPosition.x, anchorPosition.y + 2.5f, 8f);
        float anchorOffset = anchorGroundY.HasValue ? anchorPosition.y - anchorGroundY.Value : 0f;
        float[] offsets = { -5.4f, -1.8f, 1.8f, 5.4f };

        for (int i = 0; i < Variants.Length; i++)
        {
            string prefabPath = $"{PrefabRoot}/DeadRevolver_{Variants[i].Id}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new FileNotFoundException($"Failed to load prefab at {prefabPath}");
            }

            Transform existingChild = group.transform.Find(Variants[i].Id);
            if (existingChild != null)
            {
                continue;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
            {
                throw new IOException($"Failed to instantiate prefab at {prefabPath}");
            }

            instance.name = Variants[i].Id;
            instance.transform.SetParent(group.transform);

            if (groupCreated)
            {
                float targetX = anchorPosition.x + offsets[i];
                float? sampledGround = SampleGroundHeight(targetX, anchorPosition.y + 2.5f, 8f);
                float targetY = sampledGround.HasValue ? sampledGround.Value + anchorOffset : anchorPosition.y;
                instance.transform.position = new Vector3(targetX, targetY, 0f);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static float? SampleGroundHeight(float x, float rayStartY, float distance)
    {
        int groundMask = LayerMask.GetMask("Ground");
        if (groundMask == 0)
        {
            return null;
        }

        RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, rayStartY), Vector2.down, distance, groundMask);
        return hit.collider != null ? hit.point.y : null;
    }

    private static Transform GetOrCreateChild(Transform parent, string name, out bool created)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            created = false;
            return child;
        }

        GameObject childObject = new GameObject(name);
        childObject.transform.SetParent(parent, false);
        created = true;
        return childObject.transform;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (layer < 0)
        {
            return;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = layer;
        }
    }
}
