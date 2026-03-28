using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrototypeBossBootstrap
{
    private const string TargetSceneName = "Yongwoo";
    private const string PlayerAnimatorControllerPath = "RobotMaid/Animations/Player/RobotMaidPlayer";
    private const string BattleHudObjectName = "PrototypeBattleHud";
    private const string DebugDirectorObjectName = "PrototypeBossDebugDirector";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || scene.name != TargetSceneName)
        {
            return;
        }

        SimplePlayerController playerController = Object.FindFirstObjectByType<SimplePlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("PrototypeBossBootstrap could not find SimplePlayerController in Yongwoo scene.");
            return;
        }

        PrototypeHealth playerHealth = SetupPlayer(playerController.gameObject);
        if (playerHealth == null)
        {
            return;
        }

        PrototypeBossDebugDirector director = SetupDebugDirector(playerController.transform);
        SetupBattleHud(playerHealth, director);
    }

    private static PrototypeHealth SetupPlayer(GameObject playerObject)
    {
        PrototypeHealth playerHealth = playerObject.GetComponent<PrototypeHealth>();

        SimplePlayerCombat combat = playerObject.GetComponent<SimplePlayerCombat>();
        BoxCollider2D bodyCollider = playerObject.GetComponent<BoxCollider2D>();
        PrototypePlayerRuntimeConfig runtimeConfig = playerObject.GetComponent<PrototypePlayerRuntimeConfig>();
        Transform visualTransform = playerObject.transform.Find("RobotMaidVisual");

        if (playerHealth == null || combat == null || bodyCollider == null || runtimeConfig == null || visualTransform == null)
        {
            Debug.LogWarning("Player prefab is missing required prototype components. Fix the prefab instead of relying on runtime fallback.");
            return null;
        }

        ConfigurePlayerAnimation(playerObject, visualTransform.gameObject);
        SetupPlayerRuntimeConfig(runtimeConfig, playerObject, combat, playerHealth, bodyCollider);
        return playerHealth;
    }

    private static void ConfigurePlayerAnimation(GameObject playerObject, GameObject visualObject)
    {
        Animator animator = visualObject.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Player visual is missing Animator. Fix the prefab instead of relying on runtime fallback.");
            return;
        }

        RuntimeAnimatorController playerController = Resources.Load<RuntimeAnimatorController>(PlayerAnimatorControllerPath);
        if (playerController != null)
        {
            animator.runtimeAnimatorController = playerController;
            animator.enabled = true;
        }
        else
        {
            animator.enabled = false;
        }
    }

    private static void SetupPlayerRuntimeConfig(PrototypePlayerRuntimeConfig runtimeConfig, GameObject playerObject, SimplePlayerCombat combat, PrototypeHealth playerHealth, BoxCollider2D bodyCollider)
    {
        SimplePlayerController controller = playerObject.GetComponent<SimplePlayerController>();
        SimpleCameraFollow cameraFollow = Object.FindFirstObjectByType<SimpleCameraFollow>();
        runtimeConfig.Initialize(controller, combat, playerHealth, bodyCollider, cameraFollow);
    }

    private static PrototypeBossDebugDirector SetupDebugDirector(Transform playerTransform)
    {
        GameObject directorObject = GameObject.Find(DebugDirectorObjectName);
        if (directorObject == null)
        {
            directorObject = new GameObject(DebugDirectorObjectName);
        }

        PrototypeBossDebugDirector director = directorObject.GetComponent<PrototypeBossDebugDirector>();
        if (director == null)
        {
            director = directorObject.AddComponent<PrototypeBossDebugDirector>();
        }

        director.Initialize(playerTransform);
        return director;
    }

    private static void SetupBattleHud(PrototypeHealth playerHealth, PrototypeBossDebugDirector director)
    {
        GameObject hudObject = GameObject.Find(BattleHudObjectName);
        if (hudObject == null)
        {
            hudObject = new GameObject(BattleHudObjectName, typeof(RectTransform));
        }

        PrototypeBattleHud hud = hudObject.GetComponent<PrototypeBattleHud>();
        if (hud == null)
        {
            hud = hudObject.AddComponent<PrototypeBattleHud>();
        }

        hud.Initialize(playerHealth, director != null ? director.CurrentBossHealth : null);
        hud.BindDirector(director);
        if (director != null)
        {
            director.SetHud(hud);
        }
    }
}
