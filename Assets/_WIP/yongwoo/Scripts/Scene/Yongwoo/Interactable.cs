using UnityEngine;

// 역할:
// - 플레이어가 접근하면 프롬프트를 표시하고, 상호작용 키를 누르면 시퀀스를 실행합니다.
// - HOME 코어, 단말기, 문, 브로커 등 E키 상호작용 오브젝트에 사용합니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class Interactable : MonoBehaviour
{
    [Header("Prompt")]
    [SerializeField] private string promptText = "E : 상호작용";

    [Header("Action")]
    [SerializeField] private SceneEventSequence onInteractSequence;
    // 기본값은 반복 가능. 일회성(HOME코어, 칩장치 등)만 인스펙터 또는 빌더에서 true로 설정.
    [SerializeField] private bool interactOnce = false;

    [Header("References")]
    [SerializeField] private SystemLogPanel promptPanel;

    [Header("Optional Visual Animation")]
    [SerializeField] private Animator visualAnimator;
    [SerializeField] private string talkingParameter = "Talking";

    private bool _playerInside;
    private bool _used;
    private bool _isTalking;
    private Collider2D _collider;

    private void Reset()
    {
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
    }

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;

        if (promptPanel == null)
        {
            promptPanel = Object.FindFirstObjectByType<SystemLogPanel>();
        }

        if (visualAnimator == null)
        {
            visualAnimator = GetComponentInChildren<Animator>(includeInactive: true);
        }
    }

    private void Update()
    {
        UpdateTalkingState();

        if (!_playerInside || (interactOnce && _used))
        {
            return;
        }

        if (onInteractSequence != null && onInteractSequence.IsPlaying)
        {
            return;
        }

        if (GameInput.Instance.InteractPressed)
        {
            _used = true;
            promptPanel?.Hide();
            YongwooAudioManager.Play(YongwooSfxId.UiConfirm, 0.56f, 0.03f);

            if (onInteractSequence != null)
            {
                onInteractSequence.Play();
                SetTalking(true);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        if (interactOnce && _used)
        {
            return;
        }

        _playerInside = true;
        promptPanel?.Show(promptText);
        YongwooAudioManager.Play(YongwooSfxId.UiPromptIn, 0.42f, 0.03f);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        _playerInside = false;
        promptPanel?.Hide();
    }

    private void OnDisable()
    {
        SetTalking(false);
    }

    private void UpdateTalkingState()
    {
        if (onInteractSequence == null)
        {
            return;
        }

        SetTalking(onInteractSequence.IsPlaying);
    }

    private void SetTalking(bool talking)
    {
        if (_isTalking == talking)
        {
            return;
        }

        _isTalking = talking;

        if (visualAnimator != null && !string.IsNullOrWhiteSpace(talkingParameter))
        {
            visualAnimator.SetBool(talkingParameter, talking);
        }
    }

    private static bool IsPlayer(Collider2D other)
    {
        return other.CompareTag("Player") || other.GetComponentInParent<PlayerInteraction>() != null;
    }
}
