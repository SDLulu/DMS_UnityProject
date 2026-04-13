using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 역할:
// - Blind Huntress 프로토타입 프리팹을 기반으로 적 전용 프리팹을 만듭니다.
// - 현재 씬에도 같은 적 인스턴스를 바로 배치합니다.

public static class BlindHuntressEnemyBuilder
{
    private const string EnemyObjectName = "BlindHuntressEnemy";
    private const string SourcePrefabPath = "Assets/_WIP/yongwoo/Prefabs/Prototype/BlindHuntressPrototype.prefab";
    private const string EnemyPrefabFolder = "Assets/_WIP/yongwoo/Prefabs/Enemy";
    private const string EnemyPrefabPath = EnemyPrefabFolder + "/BlindHuntressEnemy.prefab";

    [MenuItem("Tools/Yongwoo/Build Blind Huntress Enemy")]
    public static void BuildFromMenu()
    {
        Build();
    }

    public static void Build()
    {
        EnsureFolder(EnemyPrefabFolder);
        BlindHuntressEnemyAnimationEventSetup.Apply();
        Selection.activeObject = null;

        GameObject prefab = BuildPrefab();
        PlacePrefabInActiveScene(prefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Blind Huntress enemy build complete.");
    }

    private static GameObject BuildPrefab()
    {
        if (File.Exists(EnemyPrefabPath))
        {
            GameObject existingRoot = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
            if (existingRoot == null)
            {
                throw new System.InvalidOperationException($"Failed to load existing Blind Huntress enemy prefab at {EnemyPrefabPath}");
            }

            ConfigureEnemyRoot(existingRoot, preserveExistingTuning: true);
            PrefabUtility.SaveAsPrefabAsset(existingRoot, EnemyPrefabPath);
            PrefabUtility.UnloadPrefabContents(existingRoot);
            return AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
        }

        GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
        if (sourcePrefab == null)
        {
            throw new FileNotFoundException($"Blind Huntress prototype prefab not found at {SourcePrefabPath}");
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
        if (instance == null)
        {
            throw new System.InvalidOperationException("Failed to instantiate Blind Huntress prototype prefab.");
        }

        if (PrefabUtility.IsPartOfAnyPrefab(instance))
        {
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        ConfigureEnemyRoot(instance, preserveExistingTuning: false);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, EnemyPrefabPath);
        Object.DestroyImmediate(instance);
        return AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
    }

    private static void PlacePrefabInActiveScene(GameObject prefab)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || string.IsNullOrWhiteSpace(scene.path))
        {
            throw new System.InvalidOperationException("Open and save the target scene in Unity before building the Blind Huntress enemy.");
        }

        GameObject existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == EnemyObjectName);
        if (existing != null)
        {
            GameObject existingSource = PrefabUtility.GetCorrespondingObjectFromSource(existing);
            if (existingSource == prefab)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                return;
            }

            Debug.LogWarning($"Scene already has a root object named {EnemyObjectName}, but it is not the expected prefab instance. Builder skipped scene placement to avoid overwriting manual scene work.");
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (instance == null)
        {
            throw new System.InvalidOperationException("Failed to instantiate Blind Huntress enemy into scene.");
        }

        instance.transform.position = new Vector3(4f, 1f, 0f);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureEnemyRoot(GameObject root, bool preserveExistingTuning)
    {
        root.name = EnemyObjectName;
        root.tag = "Untagged";
        SetLayerRecursively(root, LayerMask.NameToLayer("Enemy"));

        RemoveIfPresent<BlindHuntressPrototypeAnimationDriver>(root);
        RemoveIfPresent<BlindHuntressPrototypeCombat>(root);
        RemoveIfPresent<PlayerInteraction>(root);
        RemoveIfPresent<SimplePlayerController>(root);

        Transform visual = root.transform.Find("Visual");
        if (visual == null)
        {
            throw new System.InvalidOperationException("Blind Huntress enemy prefab is missing Visual child.");
        }

        SpriteRenderer visualRenderer = visual.GetComponent<SpriteRenderer>();
        if (visualRenderer != null)
        {
            visualRenderer.sortingLayerName = "Enemy";
            visualRenderer.sortingOrder = 0;
        }

        Animator visualAnimator = visual.GetComponent<Animator>();
        RuntimeAnimatorController controller = visualAnimator != null ? visualAnimator.runtimeAnimatorController : null;
        BlindHuntressEnemyAnimationEventRelay eventRelay = EnsureComponent<BlindHuntressEnemyAnimationEventRelay>(visual.gameObject);

        Transform sensors = root.transform.Find("Sensors");
        if (sensors == null)
        {
            GameObject sensorsObject = new GameObject("Sensors");
            sensorsObject.transform.SetParent(root.transform, false);
            sensors = sensorsObject.transform;
        }

        RemoveChildIfPresent(sensors, "AttackOrigin");
        RemoveChildIfPresent(sensors, "UpAttackOrigin");

        Transform groundCheck = GetOrCreateChild(sensors, "GroundCheck", Vector3.zero, preserveExistingTuning);
        Transform attackHitbox = GetOrCreateChild(sensors, "AttackHitbox", new Vector3(0.64f, 0.77f, 0f), preserveExistingTuning);
        Transform dashAttackHitbox = GetOrCreateChild(sensors, "DashAttackHitbox", new Vector3(0.72f, 0.74f, 0f), preserveExistingTuning);
        Transform upAttackHitbox = GetOrCreateChild(sensors, "UpAttackHitbox", new Vector3(0f, 1.66f, 0f), preserveExistingTuning);
        Transform wallCheck = GetOrCreateChild(sensors, "WallCheck", new Vector3(0.32f, 0.56f, 0f), preserveExistingTuning);
        Transform ledgeCheck = GetOrCreateChild(sensors, "LedgeCheck", new Vector3(0.4f, 0.04f, 0f), preserveExistingTuning);

        BlindHuntressEnemyCombat combat = EnsureComponent<BlindHuntressEnemyCombat>(root);
        BlindHuntressEnemyBrain brain = EnsureComponent<BlindHuntressEnemyBrain>(root);
        BlindHuntressEnemyAnimationDriver animationDriver = EnsureComponent<BlindHuntressEnemyAnimationDriver>(root);
        BlindHuntressEnemyEditPreview editPreview = EnsureComponent<BlindHuntressEnemyEditPreview>(root);
        EnsureComponent<BlindHuntressEnemyInteraction>(root);

        SerializedObject combatSo = new SerializedObject(combat);
        combatSo.FindProperty("attackHitboxAnchor").objectReferenceValue = attackHitbox;
        combatSo.FindProperty("dashAttackHitboxAnchor").objectReferenceValue = dashAttackHitbox;
        combatSo.FindProperty("upAttackHitboxAnchor").objectReferenceValue = upAttackHitbox;
        SerializedProperty hitLayersProp = combatSo.FindProperty("hitLayers");
        if (hitLayersProp.intValue == 0)
        {
            hitLayersProp.intValue = LayerMask.GetMask("Player");
        }

        combatSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject brainSo = new SerializedObject(brain);
        brainSo.FindProperty("visualRoot").objectReferenceValue = visual;
        brainSo.FindProperty("groundCheck").objectReferenceValue = groundCheck;
        brainSo.FindProperty("wallCheck").objectReferenceValue = wallCheck;
        brainSo.FindProperty("ledgeCheck").objectReferenceValue = ledgeCheck;
        SerializedProperty groundLayerProp = brainSo.FindProperty("groundLayer");
        if (groundLayerProp.intValue == 0)
        {
            groundLayerProp.intValue = LayerMask.GetMask("Ground");
        }

        brainSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject animationSo = new SerializedObject(animationDriver);
        animationSo.FindProperty("visualRoot").objectReferenceValue = visual;
        animationSo.FindProperty("fallbackController").objectReferenceValue = controller;
        animationSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject relaySo = new SerializedObject(eventRelay);
        relaySo.FindProperty("combat").objectReferenceValue = combat;
        relaySo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject previewSo = new SerializedObject(editPreview);
        previewSo.FindProperty("visualRoot").objectReferenceValue = visual;
        previewSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform GetOrCreateChild(Transform parent, string childName, Vector3 localPosition, bool preserveExistingTuning)
    {
        Transform child = parent.Find(childName);
        bool created = false;
        if (child == null)
        {
            GameObject childObject = new GameObject(childName);
            childObject.transform.SetParent(parent, false);
            child = childObject.transform;
            created = true;
        }

        if (created || !preserveExistingTuning)
        {
            child.localPosition = localPosition;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
        }

        child.gameObject.layer = parent.gameObject.layer;
        return child;
    }

    private static void RemoveChildIfPresent(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (layer < 0)
        {
            return;
        }

        root.layer = layer;
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            children[i].gameObject.layer = layer;
        }
    }

    private static void EnsureFolder(string assetPath)
    {
        string parent = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
        if (string.IsNullOrWhiteSpace(parent) || AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, Path.GetFileName(assetPath));
    }

    private static T EnsureComponent<T>(GameObject root) where T : Component
    {
        T component = root.GetComponent<T>();
        if (component == null)
        {
            component = root.AddComponent<T>();
        }

        return component;
    }

    private static void RemoveIfPresent<T>(GameObject root) where T : Component
    {
        T component = root.GetComponent<T>();
        if (component != null)
        {
            Object.DestroyImmediate(component);
        }
    }
}
