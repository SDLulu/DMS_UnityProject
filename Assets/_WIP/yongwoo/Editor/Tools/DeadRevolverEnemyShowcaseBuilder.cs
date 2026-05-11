using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DeadRevolverEnemyShowcaseBuilder
{
    private const string SourceAnimationRoot = "Assets/ThirdParty/DeadRevolver/PixelPrototypePlayerSprites/Art/Animations";
    private const string WorkingRoot = "Assets/_WIP/yongwoo/Art/Enemy/DeadRevolver";
    private const string WorkingAnimationRoot = WorkingRoot + "/Animations";
    private const string WorkingControllerRoot = WorkingRoot + "/Controllers";
    private const string PrefabRoot = "Assets/_WIP/yongwoo/Prefabs/Enemy/DeadRevolver";
    private const string ScenePath = "Assets/_Scenes/Dev_yongwoo_DeadRevolverEnemies.unity";

    private sealed class EnemyDefinition
    {
        public EnemyDefinition(string id, string label, string idleClip, string moveClip, string attackClip, string hitClip, string deathClip)
        {
            Id = id;
            Label = label;
            IdleClip = idleClip;
            MoveClip = moveClip;
            AttackClip = attackClip;
            HitClip = hitClip;
            DeathClip = deathClip;
        }

        public string Id { get; }
        public string Label { get; }
        public string IdleClip { get; }
        public string MoveClip { get; }
        public string AttackClip { get; }
        public string HitClip { get; }
        public string DeathClip { get; }
    }

    private static readonly EnemyDefinition[] Definitions =
    {
        new EnemyDefinition("Gunner", "Gun", "GunAim", "GunRun", "GunFire", "HitDamage", "Death"),
        new EnemyDefinition("Swordsman", "Sword", "SwordIdle", "SwordRun", "SwordAttack", "HitDamage", "Death"),
        new EnemyDefinition("Brawler", "Fist", "Idle", "Run", "PunchA", "HitDamage", "Death"),
        new EnemyDefinition("ShieldBearer", "Shield", "GunAim", "GunWalk", "GunFire", "HitDamage", "Death")
    };

    [MenuItem("Tools/Yongwoo/Build DeadRevolver Enemy Showcase")]
    public static void Build()
    {
        EnsureAssets();
        Dictionary<string, GameObject> prefabs = LoadPrefabs();
        BuildScene(prefabs);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("DeadRevolver enemy showcase build complete.");
    }

    public static void EnsureAssets()
    {
        EnsureFolder(WorkingRoot);
        EnsureFolder(WorkingAnimationRoot);
        EnsureFolder(WorkingControllerRoot);
        EnsureFolder(PrefabRoot);

        CopyRequiredAnimations();
        Dictionary<string, RuntimeAnimatorController> controllers = BuildControllers();
        BuildPrefabs(controllers);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static Dictionary<string, GameObject> LoadPrefabs()
    {
        Dictionary<string, GameObject> prefabs = new();

        for (int i = 0; i < Definitions.Length; i++)
        {
            string prefabPath = $"{PrefabRoot}/DeadRevolver_{Definitions[i].Id}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                prefabs[Definitions[i].Id] = prefab;
            }
        }

        return prefabs;
    }

    private static void CopyRequiredAnimations()
    {
        HashSet<string> clipNames = new();

        for (int i = 0; i < Definitions.Length; i++)
        {
            clipNames.Add(Definitions[i].IdleClip);
            clipNames.Add(Definitions[i].MoveClip);
            clipNames.Add(Definitions[i].AttackClip);
            clipNames.Add(Definitions[i].HitClip);
            clipNames.Add(Definitions[i].DeathClip);
        }

        foreach (string clipName in clipNames)
        {
            string sourcePath = $"{SourceAnimationRoot}/{clipName}.anim";
            string destinationPath = $"{WorkingAnimationRoot}/{clipName}.anim";

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(sourcePath) == null)
            {
                throw new FileNotFoundException($"DeadRevolver source clip missing: {sourcePath}");
            }

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath) != null)
            {
                continue;
            }

            if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
            {
                throw new IOException($"Failed to copy animation clip from {sourcePath} to {destinationPath}");
            }
        }
    }

    private static Dictionary<string, RuntimeAnimatorController> BuildControllers()
    {
        Dictionary<string, RuntimeAnimatorController> controllers = new();

        for (int i = 0; i < Definitions.Length; i++)
        {
            EnemyDefinition definition = Definitions[i];
            string controllerPath = $"{WorkingControllerRoot}/Enemy_{definition.Id}.controller";

            AnimatorController existingController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (existingController != null)
            {
                controllers.Add(definition.Id, existingController);
                continue;
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Move", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            stateMachine.anyStatePosition = new Vector3(-240f, 90f, 0f);

            AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(0f, 0f, 0f));
            AnimatorState moveState = stateMachine.AddState("Move", new Vector3(240f, 0f, 0f));
            AnimatorState attackState = stateMachine.AddState("Attack", new Vector3(0f, 120f, 0f));
            AnimatorState hitState = stateMachine.AddState("Hit", new Vector3(240f, 120f, 0f));
            AnimatorState deathState = stateMachine.AddState("Death", new Vector3(120f, 240f, 0f));

            idleState.motion = LoadWorkingClip(definition.IdleClip);
            moveState.motion = LoadWorkingClip(definition.MoveClip);
            attackState.motion = LoadWorkingClip(definition.AttackClip);
            hitState.motion = LoadWorkingClip(definition.HitClip);
            deathState.motion = LoadWorkingClip(definition.DeathClip);

            stateMachine.defaultState = idleState;

            AddMoveTransition(idleState, moveState, true);
            AddMoveTransition(moveState, idleState, false);
            AddActionTransition(idleState, attackState, "Attack");
            AddActionTransition(moveState, attackState, "Attack");
            AddAnyStateTransition(stateMachine, hitState, "Hit");
            AddAnyStateTransition(stateMachine, deathState, "Die");
            AddReturnTransition(attackState, idleState);
            AddReturnTransition(hitState, idleState);

            controllers.Add(definition.Id, controller);
        }

        return controllers;
    }

    private static Dictionary<string, GameObject> BuildPrefabs(Dictionary<string, RuntimeAnimatorController> controllers)
    {
        Dictionary<string, GameObject> prefabs = new();

        for (int i = 0; i < Definitions.Length; i++)
        {
            EnemyDefinition definition = Definitions[i];
            string prefabPath = $"{PrefabRoot}/DeadRevolver_{definition.Id}.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                prefabs.Add(definition.Id, existingPrefab);
                continue;
            }

            GameObject root = new GameObject($"DeadRevolver_{definition.Id}");
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Enemy";
            renderer.sprite = GetFirstSprite(LoadWorkingClip(definition.IdleClip));

            Animator animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controllers[definition.Id];

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            prefabs.Add(definition.Id, prefab);
        }

        return prefabs;
    }

    private static void BuildScene(Dictionary<string, GameObject> prefabs)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Camera camera = CreateCamera();
        camera.transform.position = new Vector3(0f, 0.75f, -10f);
        camera.orthographic = true;
        camera.orthographicSize = 3.5f;
        camera.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
        camera.clearFlags = CameraClearFlags.SolidColor;

        CreateGround();

        float[] xPositions = { -6f, -2f, 2f, 6f };

        for (int i = 0; i < Definitions.Length; i++)
        {
            EnemyDefinition definition = Definitions[i];
            GameObject instance = PrefabUtility.InstantiatePrefab(prefabs[definition.Id], scene) as GameObject;
            if (instance == null)
            {
                throw new IOException($"Failed to instantiate prefab for {definition.Id}");
            }

            instance.name = definition.Id;
            instance.transform.position = new Vector3(xPositions[i], -0.35f, 0f);
            CreateLabel(instance.transform.position + new Vector3(0f, -1.4f, 0f), $"{definition.Label} Enemy");
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        return camera;
    }

    private static void CreateGround()
    {
        GameObject ground = new GameObject("GroundLine");
        LineRenderer line = ground.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, new Vector3(-9f, -1.05f, 0f));
        line.SetPosition(1, new Vector3(9f, -1.05f, 0f));
        line.startWidth = 0.08f;
        line.endWidth = 0.08f;
        line.useWorldSpace = true;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(0.42f, 0.47f, 0.55f);
        line.endColor = line.startColor;
        line.sortingLayerName = "Default";
    }

    private static void CreateLabel(Vector3 position, string text)
    {
        GameObject label = new GameObject(text);
        label.transform.position = position;

        TextMesh textMesh = label.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.characterSize = 0.14f;
        textMesh.fontSize = 48;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = new Color(0.89f, 0.91f, 0.95f);
    }

    private static void AddMoveTransition(AnimatorState from, AnimatorState to, bool shouldMove)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.AddCondition(shouldMove ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "Move");
    }

    private static void AddActionTransition(AnimatorState from, AnimatorState to, string triggerName)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.03f;
        transition.AddCondition(AnimatorConditionMode.If, 0f, triggerName);
    }

    private static void AddAnyStateTransition(AnimatorStateMachine stateMachine, AnimatorState to, string triggerName)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.03f;
        transition.AddCondition(AnimatorConditionMode.If, 0f, triggerName);
    }

    private static void AddReturnTransition(AnimatorState from, AnimatorState to)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = 0.98f;
        transition.duration = 0.05f;
    }

    private static AnimationClip LoadWorkingClip(string clipName)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{WorkingAnimationRoot}/{clipName}.anim");
        if (clip == null)
        {
            throw new FileNotFoundException($"Working clip missing: {clipName}");
        }

        return clip;
    }

    private static Sprite GetFirstSprite(AnimationClip clip)
    {
        EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        for (int i = 0; i < bindings.Length; i++)
        {
            ObjectReferenceKeyframe[] frames = AnimationUtility.GetObjectReferenceCurve(clip, bindings[i]);
            if (frames.Length > 0 && frames[0].value is Sprite sprite)
            {
                return sprite;
            }
        }

        return null;
    }

    private static void EnsureFolder(string assetPath)
    {
        string normalizedPath = assetPath.Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(normalizedPath))
        {
            return;
        }

        string parentPath = Path.GetDirectoryName(normalizedPath)?.Replace("\\", "/");
        string folderName = Path.GetFileName(normalizedPath);

        if (string.IsNullOrWhiteSpace(parentPath) || string.IsNullOrWhiteSpace(folderName))
        {
            return;
        }

        EnsureFolder(parentPath);
        AssetDatabase.CreateFolder(parentPath, folderName);
    }
}
