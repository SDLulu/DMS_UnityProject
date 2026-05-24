using System;
using UnityEngine;

[Serializable]
public class GameState
{
    [Header("Scene Transition")]
    [SerializeField] private string currentSceneName;
    [SerializeField] private string nextSceneName;
    [SerializeField] private string nextSpawnPointName;
    [SerializeField] private bool hasPendingSpawnPoint;

    [Header("Player Snapshot")]
    [SerializeField] private float playerCurrentHealth = 1f;
    [SerializeField] private float playerMaxHealth = 1f;
    [SerializeField] private float playerSlowCharge;
    [SerializeField] private Vector3 lastPlayerPosition;

    public string CurrentSceneName => currentSceneName;
    public string NextSceneName => nextSceneName;
    public string NextSpawnPointName => nextSpawnPointName;
    public bool HasPendingSpawnPoint => hasPendingSpawnPoint;
    public float PlayerCurrentHealth => playerCurrentHealth;
    public float PlayerMaxHealth => playerMaxHealth;
    public float PlayerSlowCharge => playerSlowCharge;
    public Vector3 LastPlayerPosition => lastPlayerPosition;

    public void SetCurrentScene(string sceneName)
    {
        currentSceneName = sceneName;
    }

    public void SetNextSpawnPoint(string sceneName, string spawnPointName)
    {
        nextSceneName = sceneName;
        nextSpawnPointName = spawnPointName;
        hasPendingSpawnPoint = !string.IsNullOrWhiteSpace(spawnPointName);
    }

    public void ClearPendingSpawnPoint()
    {
        nextSceneName = string.Empty;
        nextSpawnPointName = string.Empty;
        hasPendingSpawnPoint = false;
    }

    public void SavePlayerSnapshot(
        float currentHealth,
        float maxHealth,
        float slowCharge,
        Vector3 position)
    {
        playerCurrentHealth = Mathf.Max(0f, currentHealth);
        playerMaxHealth = Mathf.Max(1f, maxHealth);
        playerSlowCharge = Mathf.Max(0f, slowCharge);
        lastPlayerPosition = position;
    }
}
