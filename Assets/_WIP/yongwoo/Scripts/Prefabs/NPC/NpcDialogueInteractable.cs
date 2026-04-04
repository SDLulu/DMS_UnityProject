using UnityEngine;
using UnityEngine.Events;

// 역할:
// - NPC 쪽에서 상호작용 반경과 입력을 감지해 DialogueManager를 호출하는 진입점입니다.
// - NPC 프리팹에 붙는 개별 오브젝트 컴포넌트입니다.

[DisallowMultipleComponent]
public class NpcDialogueInteractable : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueSequence dialogueSequence;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private Transform interactionOrigin;
    [SerializeField] private float interactionRadius = 1.75f;

    [Header("Events")]
    [SerializeField] private UnityEvent onDialogueStarted;
    [SerializeField] private UnityEvent onDialogueCompleted;

    private SimplePlayerController _playerController;

    public bool IsPlayerInRange => TryGetPlayerPosition(out Vector3 playerPosition)
        && Vector3.Distance(GetInteractionOriginPosition(), playerPosition) <= interactionRadius;

    private void Awake()
    {
        AutoWire();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        AutoWire();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        AutoWire();
        if (dialogueSequence == null || dialogueManager == null || dialogueManager.IsPlaying || !IsPlayerInRange)
        {
            return;
        }

        if (!GameInput.Instance.InteractPressed)
        {
            return;
        }

        dialogueManager.TryPlay(dialogueSequence, new DialoguePlaybackContext
        {
            onStarted = () => onDialogueStarted?.Invoke(),
            onCompleted = () => onDialogueCompleted?.Invoke()
        });
    }

    private void AutoWire()
    {
        dialogueManager ??= UnityEngine.Object.FindFirstObjectByType<DialogueManager>();
        _playerController ??= UnityEngine.Object.FindFirstObjectByType<SimplePlayerController>();
    }

    private bool TryGetPlayerPosition(out Vector3 playerPosition)
    {
        playerPosition = Vector3.zero;
        if (_playerController == null)
        {
            return false;
        }

        playerPosition = _playerController.transform.position;
        return true;
    }

    private Vector3 GetInteractionOriginPosition()
    {
        return interactionOrigin != null ? interactionOrigin.position : transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.85f);
        Gizmos.DrawWireSphere(GetInteractionOriginPosition(), Mathf.Max(0.05f, interactionRadius));
    }
}
