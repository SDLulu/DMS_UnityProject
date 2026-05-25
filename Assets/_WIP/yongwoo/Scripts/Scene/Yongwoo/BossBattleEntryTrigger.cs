using System.Collections;
using UnityEngine;

// 역할:
// - 플레이어가 보스 아레나 입구 트리거에 들어오면 BossBattleArena.EnterBattle()을 호출합니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class BossBattleEntryTrigger : MonoBehaviour
{
    [SerializeField] private BossBattleArena arena;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private string requiredTag = "Player";
    [SerializeField] private SceneEventSequence beforeBattleSequence;
    [SerializeField] private bool waitForBeforeBattleSequence = true;

    [Header("Respawn")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private bool updateRespawnOnEnter = true;

    private bool _hasTriggered;
    private bool _isEntering;
    private Collider2D _collider;

    private void Reset()
    {
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
        arena = GetComponentInParent<BossBattleArena>();
        if (arena == null)
        {
            arena = FindFirstObjectByType<BossBattleArena>();
        }

        EnsureTutorialMarker();
    }

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
        arena ??= GetComponentInParent<BossBattleArena>();
        arena ??= FindFirstObjectByType<BossBattleArena>();
        respawnPoint ??= FindDeepChild(transform.root, "스폰_보스방");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && _hasTriggered)
        {
            return;
        }

        if (_isEntering)
        {
            return;
        }

        if (arena == null)
        {
            Debug.LogWarning("[BossBattleEntry] arena 참조 없음", this);
            return;
        }

        if (arena.IsActive)
        {
            return;
        }

        bool hasRequiredTag = string.IsNullOrWhiteSpace(requiredTag) || other.CompareTag(requiredTag);
        PlayerInteraction player = other.GetComponentInParent<PlayerInteraction>();
        if (!hasRequiredTag && player == null)
        {
            return;
        }

        ApplyRespawnPoint(player);
        _hasTriggered = true;
        beforeBattleSequence ??= BossStoryRuntimeSequenceFactory.EnsureEntrySequence(transform);
        StartCoroutine(EnterBattleRoutine());
    }

    private IEnumerator EnterBattleRoutine()
    {
        _isEntering = true;

        arena.EnterIntroView();

        if (beforeBattleSequence != null)
        {
            beforeBattleSequence.Play();
            if (waitForBeforeBattleSequence)
            {
                while (beforeBattleSequence.IsPlaying)
                {
                    yield return null;
                }
            }
        }

        arena.EnterBattle();
        _isEntering = false;
    }

    private void ApplyRespawnPoint(PlayerInteraction player)
    {
        if (!updateRespawnOnEnter || player == null || respawnPoint == null)
        {
            return;
        }

        player.SetSpawnPosition(respawnPoint.position);
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
    }

    private void EnsureTutorialMarker()
    {
        TutorialMarker marker = GetComponent<TutorialMarker>();
        if (marker == null)
        {
            marker = gameObject.AddComponent<TutorialMarker>();
        }

        marker.Configure(TutorialMarker.MarkerType.Trigger, 0.45f);
    }
}
