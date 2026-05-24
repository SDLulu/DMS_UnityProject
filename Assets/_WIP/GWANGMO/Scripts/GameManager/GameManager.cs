using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameState state = new GameState();

    public GameState State => state;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        state.SetCurrentScene(SceneManager.GetActiveScene().name);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    public void LoadSceneFromPortal(string targetSceneName, string targetSpawnPointName, GameObject player)
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("Target scene name is empty.", this);
            return;
        }

        CapturePlayerState(player);
        state.SetNextSpawnPoint(targetSceneName, targetSpawnPointName);
        SceneManager.LoadScene(targetSceneName);
    }

    public void LoadSceneFromPortal(string targetSceneName, string targetSpawnPointName)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        LoadSceneFromPortal(targetSceneName, targetSpawnPointName, player);
    }

    public void CapturePlayerState(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        float currentHealth = 1f;
        float maxHealth = 1f;
        float slowCharge = 0f;

        PlayerInteraction interaction = player.GetComponentInParent<PlayerInteraction>();
        if (interaction != null)
        {
            currentHealth = interaction.CurrentHealth;
            maxHealth = interaction.MaxHealth;
        }

        PlayerSlowMotion slowMotion = player.GetComponentInParent<PlayerSlowMotion>();
        if (slowMotion != null)
        {
            slowCharge = slowMotion.CurrentChargesRaw;
        }

        state.SavePlayerSnapshot(currentHealth, maxHealth, slowCharge, player.transform.position);
    }

    public string ConsumeNextSpawnPointName()
    {
        string spawnPointName = state.NextSpawnPointName;
        state.ClearPendingSpawnPoint();
        return spawnPointName;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        state.SetCurrentScene(scene.name);
    }
}
