using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SceneSpawnManager : MonoBehaviour
{
    [Header("Fallback")]
    [SerializeField] private Transform defaultSpawnPoint;

    [Header("Options")]
    [SerializeField] private bool clearPlayerVelocity = true;
    [SerializeField] private bool snapCameraAfterSpawn = true;

    private IEnumerator Start()
    {
        yield return null;
        SpawnPlayer();
    }

    public void SpawnPlayer()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameObject player = FindPlayer();
        if (player == null)
        {
            Debug.LogWarning("SceneSpawnManager could not find a Player object.", this);
            return;
        }

        Transform spawnPoint = ResolveSpawnPoint();
        if (spawnPoint == null)
        {
            return;
        }

        MovePlayer(player, spawnPoint.position);

        if (snapCameraAfterSpawn)
        {
            SimpleCameraFollow cameraFollow = FindFirstObjectByType<SimpleCameraFollow>();
            cameraFollow?.SetTarget(player.transform);
            cameraFollow?.SnapToTarget();
        }
    }

    private Transform ResolveSpawnPoint()
    {
        GameState state = GameManager.Instance.State;
        string spawnPointName = state.HasPendingSpawnPoint
            ? GameManager.Instance.ConsumeNextSpawnPointName()
            : string.Empty;

        if (!string.IsNullOrWhiteSpace(spawnPointName))
        {
            GameObject spawnObject = GameObject.Find(spawnPointName);
            if (spawnObject != null)
            {
                return spawnObject.transform;
            }

            Debug.LogWarning($"Spawn point '{spawnPointName}' was not found in this scene.", this);
        }

        return defaultSpawnPoint;
    }

    private GameObject FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player;
        }

        PlayerInteraction interaction = FindFirstObjectByType<PlayerInteraction>();
        return interaction != null ? interaction.gameObject : null;
    }

    private void MovePlayer(GameObject player, Vector3 position)
    {
        PlayerInteraction interaction = player.GetComponentInParent<PlayerInteraction>();
        if (interaction != null)
        {
            interaction.MoveToPosition(position, clearPlayerVelocity);
            interaction.SetSpawnPosition(position);
            return;
        }

        Rigidbody2D body = player.GetComponentInParent<Rigidbody2D>();
        if (body != null)
        {
            body.position = position;
            if (clearPlayerVelocity)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        player.transform.position = position;
    }
}
