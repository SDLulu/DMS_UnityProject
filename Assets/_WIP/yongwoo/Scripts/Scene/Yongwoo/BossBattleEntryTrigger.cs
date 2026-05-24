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
        bool hasPlayerInteraction = other.GetComponentInParent<PlayerInteraction>() != null;
        if (!hasRequiredTag && !hasPlayerInteraction)
        {
            return;
        }

        _hasTriggered = true;
        beforeBattleSequence ??= BossStoryRuntimeSequenceFactory.EnsureEntrySequence(transform);
        StartCoroutine(EnterBattleRoutine());
    }

    private IEnumerator EnterBattleRoutine()
    {
        _isEntering = true;

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
