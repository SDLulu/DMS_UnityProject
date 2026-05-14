using UnityEngine;

// 역할:
// - 플레이어가 특정 2D 트리거에 들어오면 연결된 SceneEventSequence를 실행합니다.
// - 튜토리얼 안내, 브로커 통신, 장소 이벤트처럼 위치 기반 진행에 사용합니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class SceneEventTrigger2D : MonoBehaviour
{
    [SerializeField] private SceneEventSequence sequence;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private string requiredTag = "Player";

    private bool _hasTriggered;
    private Collider2D _collider;

    private void Reset()
    {
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
        sequence = GetComponent<SceneEventSequence>();
    }

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;

        if (sequence == null)
        {
            sequence = GetComponent<SceneEventSequence>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && _hasTriggered)
        {
            return;
        }

        bool hasRequiredTag = string.IsNullOrWhiteSpace(requiredTag) || other.CompareTag(requiredTag);
        bool hasPlayerInteraction = other.GetComponentInParent<PlayerInteraction>() != null;
        if (!hasRequiredTag && !hasPlayerInteraction)
        {
            return;
        }

        if (sequence == null || sequence.IsPlaying)
        {
            return;
        }

        _hasTriggered = true;
        sequence.Play();
    }
}
