using UnityEngine;

// 역할:
// - 씬 하단 낙사 트리거가 기존 PlayerInteraction 사망/부활 흐름만 재사용하게 합니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class DeathZone : MonoBehaviour
{
    [Header("Respawn")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private bool updateRespawnPointBeforeDeath = true;

    private void Reset()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void OnValidate()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerInteraction player = other.GetComponentInParent<PlayerInteraction>();
        if (player == null)
        {
            return;
        }

        ApplyRespawnPoint(player);
        player.OnDie();
    }

    private void ApplyRespawnPoint(PlayerInteraction player)
    {
        if (!updateRespawnPointBeforeDeath || respawnPoint == null)
        {
            return;
        }

        player.SetSpawnPosition(respawnPoint.position);
    }
}
