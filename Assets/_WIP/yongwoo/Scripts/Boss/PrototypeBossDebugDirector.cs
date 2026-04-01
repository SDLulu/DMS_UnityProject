using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class PrototypeBossDebugDirector : MonoBehaviour
{
    private const string BossPrefabAssetPath = "Assets/_WIP/yongwoo/Prefabs/Prototype/Boss.prefab";
    private const string BossAnimatorControllerPath = "RobotMaid/Animations/Boss/RobotMaidBoss";

    private Transform _playerTransform;
    private PrototypeBattleHud _hud;
    private PrototypeHealth _currentBossHealth;
    private GameObject _currentBossObject;
    private Vector3 _spawnPosition;

    public PrototypeHealth CurrentBossHealth => _currentBossHealth;

    public void Initialize(Transform playerTransform)
    {
        _playerTransform = playerTransform;

        if (_playerTransform != null)
        {
            PrototypeBossController bossPrefabController = LoadBossPrefabController();
            PrototypeBossConfig config = bossPrefabController != null
                ? bossPrefabController.RuntimeConfig
                : PrototypeBossConfigLoader.CreateDefault();
            _spawnPosition = CalculateSpawnPosition(config);
        }
    }

    public void SetHud(PrototypeBattleHud hud)
    {
        _hud = hud;
        _hud?.SetBossHealth(_currentBossHealth);
    }

    public void SpawnOrResetBoss()
    {
        if (_currentBossObject != null)
        {
            Destroy(_currentBossObject);
            _currentBossObject = null;
            _currentBossHealth = null;
            _hud?.SetBossHealth(null);
        }

        SpawnBoss();
    }

    private void SpawnBoss()
    {
        if (_playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                return;
            }

            _playerTransform = playerObject.transform;
        }

        GameObject bossPrefab = LoadBossPrefab();
        if (bossPrefab == null)
        {
            Debug.LogWarning($"Could not load boss prefab at {BossPrefabAssetPath}. Assign or restore the prefab first.");
            return;
        }

        PrototypeBossController prefabController = bossPrefab.GetComponent<PrototypeBossController>();
        PrototypeBossConfig config = prefabController != null
            ? prefabController.RuntimeConfig
            : PrototypeBossConfigLoader.CreateDefault();

        GameObject root = GameObject.Find("YongwooPrototype");
        if (_spawnPosition == Vector3.zero)
        {
            _spawnPosition = CalculateSpawnPosition(config);
        }

        GameObject bossObject = Instantiate(bossPrefab, _spawnPosition, Quaternion.identity);
        bossObject.name = config.core.bossName;
        if (root != null)
        {
            bossObject.transform.SetParent(root.transform);
        }

        SpriteRenderer renderer = bossObject.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = config.core.normalColor.ToColor();
        }

        Animator animator = bossObject.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController == null)
        {
            RuntimeAnimatorController bossController = Resources.Load<RuntimeAnimatorController>(BossAnimatorControllerPath);
            if (bossController != null)
            {
                animator.runtimeAnimatorController = bossController;
            }
        }

        PrototypeHealth health = bossObject.GetComponent<PrototypeHealth>();
        PrototypeBossController controller = bossObject.GetComponent<PrototypeBossController>();
        if (controller == null || health == null)
        {
            Debug.LogWarning("Boss prefab is missing PrototypeBossController or PrototypeHealth.");
            Destroy(bossObject);
            return;
        }

        controller.Initialize(_playerTransform);

        _currentBossObject = bossObject;
        _currentBossHealth = health;
        _hud?.SetBossHealth(_currentBossHealth);
    }

    private static PrototypeBossController LoadBossPrefabController()
    {
        GameObject bossPrefab = LoadBossPrefab();
        return bossPrefab != null ? bossPrefab.GetComponent<PrototypeBossController>() : null;
    }

    private static GameObject LoadBossPrefab()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabAssetPath);
#else
        return null;
#endif
    }

    private static Vector3 CalculateSpawnPosition(PrototypeBossConfig config)
    {
        float spawnX = Mathf.Clamp(config.core.arenaRight - 1.5f, config.core.arenaLeft, config.core.arenaRight);
        return new Vector3(spawnX, config.core.groundY, 0f);
    }
}
