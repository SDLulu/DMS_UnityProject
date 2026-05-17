using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 역할:
// - Blind Huntress 테스트용 애니메이션, 프리팹, 씬 배치를 한 번에 생성합니다.
// - Unity CLI에서 실행해도 같은 결과가 나오도록 에디터 절차를 코드로 고정합니다.

public static class BlindHuntressPrototypeBuilder
{
    private const string BasePlayerPrefabPath = "Assets/_WIP/yongwoo/Prefabs/Prototype/Player.prefab";
    private const string PrefabPath = "Assets/_WIP/yongwoo/Prefabs/Prototype/BlindHuntressPrototype.prefab";
    private const string AnimationFolder = "Assets/_WIP/yongwoo/Animations/BlindHuntress";
    private const string ControllerPath = AnimationFolder + "/BlindHuntressPrototype.controller";
    private const string SpriteFolder = "Assets/_WIP/yongwoo/Art/SHADOW Series - The Free Assets/SHADOW Series - The Blind Huntress/Sprite Sheet";

    [MenuItem("Tools/Yongwoo/Build Blind Huntress Prototype")]
    public static void BuildFromMenu()
    {
        Build();
    }

    public static void Build()
    {
        EnsureFolder("Assets/_WIP/yongwoo/Animations");
        EnsureFolder(AnimationFolder);

        Dictionary<string, AnimationClip> clips = BuildAnimationClips();
        AnimatorController controller = BuildAnimatorController(clips);
        GameObject prefabRoot = BuildPrefab(controller);
        PlacePrefabInActiveScene(prefabRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Blind Huntress prototype build complete.");
    }

    private static Dictionary<string, AnimationClip> BuildAnimationClips()
    {
        Dictionary<string, (string fileName, float frameRate, bool loop)> definitions = new()
        {
            { "Idle", ("1 - Idle.png", 12f, true) },
            { "Run", ("2 - Run.png", 14f, true) },
            { "Attack", ("10 - attack 1.png", 14f, false) },
            { "Jump", ("3 - jump.png", 12f, false) },
            { "MidAir", ("4 - mid-air.png", 12f, true) },
            { "Fall", ("5 - fall.png", 12f, true) },
            { "Dash", ("6 - dash.png", 14f, false) },
            { "JumpUpAttack", ("7 - jump-up-attack.png", 14f, false) },
            { "JumpDownAttack", ("8 - jump-down-attack.png", 14f, false) },
            { "IdleUpAttack", ("9 - idle-up-attack.png", 14f, false) },
            { "Attack3", ("11 - attack 3.png", 14f, false) },
            { "DashAttack", ("12 - dash attack.png", 14f, false) },
            { "SpecialDash", ("13 - spetial dash.png", 14f, false) },
            { "Hit", ("14 - hit.png", 12f, false) },
            { "Death", ("15 - death.png", 12f, false) }
        };

        Dictionary<string, AnimationClip> clips = new();
        foreach ((string stateName, (string fileName, float frameRate, bool loop)) in definitions)
        {
            string sourcePath = $"{SpriteFolder}/{fileName}";
            string clipPath = $"{AnimationFolder}/{stateName}.anim";
            AnimationClip clip = CreateSpriteClip(clipPath, sourcePath, frameRate, loop);
            clips[stateName] = clip;
        }

        return clips;
    }

    private static AnimatorController BuildAnimatorController(IReadOnlyDictionary<string, AnimationClip> clips)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        stateMachine.states = new ChildAnimatorState[0];
        foreach ((string stateName, AnimationClip clip) in clips)
        {
            AnimatorState state = stateMachine.AddState(stateName);
            state.motion = clip;
            if (stateName == "Idle")
            {
                stateMachine.defaultState = state;
            }
        }

        return controller;
    }

    private static GameObject BuildPrefab(RuntimeAnimatorController controller)
    {
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePlayerPrefabPath);
        if (basePrefab == null)
        {
            throw new FileNotFoundException($"Base player prefab not found at {BasePlayerPrefabPath}");
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
        if (instance == null)
        {
            throw new System.InvalidOperationException("Failed to instantiate base player prefab.");
        }

        instance.name = "Blind Huntress Prototype";
        instance.transform.localScale = Vector3.one;

        RemoveIfPresent<SimplePlayerCombat>(instance);
        RemoveIfPresent<PlayerAnimationDriver>(instance);
        RemoveIfPresent<PlayerRuntimeConfig>(instance);
        RemoveIfPresent<PlayerSlowMotion>(instance);
        RemoveIfPresent<Animator>(instance);

        DestroyChildIfPresent(instance.transform, "Hand");
        DestroyChildIfPresent(instance.transform, "WeaponOrigin");
        DestroyChildIfPresent(instance.transform, "Muzzle");

        SimplePlayerController movement = instance.GetComponent<SimplePlayerController>();
        SerializedObject movementSo = new SerializedObject(movement);
        movementSo.FindProperty("groundMoveSpeed").floatValue = 7.4f;
        movementSo.FindProperty("airMoveSpeed").floatValue = 6.8f;
        movementSo.FindProperty("jumpForce").floatValue = 9.5f;
        movementSo.FindProperty("dashSpeed").floatValue = 14.5f;
        movementSo.FindProperty("dashDuration").floatValue = 0.16f;
        movementSo.FindProperty("dashCooldown").floatValue = 0.38f;
        movementSo.FindProperty("rollSpeed").floatValue = 10f;
        movementSo.FindProperty("rollDuration").floatValue = 0.2f;
        movementSo.FindProperty("invertVisualFacing").boolValue = false;
        movementSo.ApplyModifiedPropertiesWithoutUndo();

        PlayerInteraction interaction = instance.GetComponent<PlayerInteraction>();
        SerializedObject interactionSo = new SerializedObject(interaction);
        interactionSo.FindProperty("maxHealth").floatValue = 6f;
        interactionSo.FindProperty("respawnDelay").floatValue = 0.65f;
        interactionSo.ApplyModifiedPropertiesWithoutUndo();

        Transform visual = instance.transform.Find("Visual");
        if (visual == null)
        {
            throw new System.InvalidOperationException("Base player prefab is missing Visual child.");
        }

        visual.localPosition = new Vector3(0f, -0.72f, 0f);
        visual.localScale = new Vector3(3f, 3f, 1f);

        SpriteRenderer visualRenderer = visual.GetComponent<SpriteRenderer>();
        visualRenderer.sprite = LoadFirstSprite($"{SpriteFolder}/1 - Idle.png");
        visualRenderer.sortingLayerName = "Player";
        visualRenderer.sortingOrder = 0;

        Animator visualAnimator = visual.GetComponent<Animator>();
        visualAnimator.runtimeAnimatorController = controller;

        Transform sensors = instance.transform.Find("Sensors");
        if (sensors == null)
        {
            GameObject sensorsObject = new GameObject("Sensors");
            sensorsObject.transform.SetParent(instance.transform, false);
            sensors = sensorsObject.transform;
        }

        sensors.localPosition = new Vector3(0f, -1.05f, 0f);
        Transform attackOrigin = sensors.Find("AttackOrigin");
        if (attackOrigin == null)
        {
            GameObject attackOriginObject = new GameObject("AttackOrigin");
            attackOriginObject.transform.SetParent(sensors, false);
            attackOrigin = attackOriginObject.transform;
        }
        attackOrigin.localPosition = new Vector3(0f, 0.82f, 0f);

        Rigidbody2D body = instance.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        BoxCollider2D bodyCollider = instance.GetComponent<BoxCollider2D>();
        bodyCollider.offset = new Vector2(0f, -0.82f);
        bodyCollider.size = new Vector2(0.34f, 0.58f);

        BlindHuntressPrototypeCombat combat = instance.GetComponent<BlindHuntressPrototypeCombat>();
        if (combat == null)
        {
            combat = instance.AddComponent<BlindHuntressPrototypeCombat>();
        }

        SerializedObject combatSo = new SerializedObject(combat);
        combatSo.FindProperty("attackOrigin").objectReferenceValue = attackOrigin;
        combatSo.FindProperty("attackOffset").vector2Value = new Vector2(0.64f, -0.05f);
        combatSo.FindProperty("attackSize").vector2Value = new Vector2(0.92f, 0.72f);
        combatSo.FindProperty("attackCooldown").floatValue = 0.34f;
        combatSo.FindProperty("attackActiveDuration").floatValue = 0.14f;
        combatSo.FindProperty("attackAnimationDuration").floatValue = 0.24f;
        combatSo.ApplyModifiedPropertiesWithoutUndo();

        BlindHuntressPrototypeAnimationDriver animationDriver = instance.GetComponent<BlindHuntressPrototypeAnimationDriver>();
        if (animationDriver == null)
        {
            animationDriver = instance.AddComponent<BlindHuntressPrototypeAnimationDriver>();
        }

        SerializedObject animationSo = new SerializedObject(animationDriver);
        animationSo.FindProperty("visualRoot").objectReferenceValue = visual;
        animationSo.FindProperty("fallbackController").objectReferenceValue = controller;
        animationSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        Object.DestroyImmediate(instance);
        return prefab;
    }

    private static void PlacePrefabInActiveScene(GameObject prefab)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || string.IsNullOrWhiteSpace(scene.path))
        {
            throw new System.InvalidOperationException("Open and save the target scene in Unity before building the Blind Huntress prototype.");
        }

        GameObject existing = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Blind Huntress Prototype");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (instance == null)
        {
            throw new System.InvalidOperationException("Failed to instantiate Blind Huntress prefab into scene.");
        }

        instance.transform.position = new Vector3(0f, 1f, 0f);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static AnimationClip CreateSpriteClip(string clipPath, string sourceTexturePath, float frameRate, bool loop)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
        {
            AssetDatabase.DeleteAsset(clipPath);
        }

        Sprite[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(sourceTexturePath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name)
            .ToArray();

        if (sprites.Length == 0)
        {
            throw new FileNotFoundException($"No sprites found at {sourceTexturePath}");
        }

        AnimationClip clip = new AnimationClip
        {
            frameRate = frameRate
        };

        EditorCurveBinding spriteBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            frames[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, frames);
        SetLoopTime(clip, loop);

        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static void SetLoopTime(AnimationClip clip, bool loop)
    {
        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
        settings.FindPropertyRelative("m_LoopTime").boolValue = loop;
        serializedClip.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Sprite LoadFirstSprite(string texturePath)
    {
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(texturePath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name)
            .FirstOrDefault();
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

    private static void DestroyChildIfPresent(Transform root, string childName)
    {
        Transform child = root.Find(childName);
        if (child != null)
        {
            Object.DestroyImmediate(child.gameObject);
        }
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
