using UnityEngine;

// 역할:
// - 기존 Hp UI에 붙어 있는 Animator의 "게이지가 차는" 루프 클립을 슬로우 자원 게이지로 재활용합니다.
// - Animator의 클립 normalizedTime을 자원량(0~1)에 매핑해 정지 상태로 표시합니다.
// - HP 표시는 일격사 룰로 의미가 없어 사용하지 않습니다.

[DisallowMultipleComponent]
public class SlowGaugeUI : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("동기화할 PlayerSlowMotion. 비워두면 씬에서 검색합니다.")]
    [SerializeField] private PlayerSlowMotion source;

    [Header("Animator")]
    [Tooltip("게이지 채움 애니메이션이 들어 있는 Animator. 비워두면 이 오브젝트에서 찾습니다.")]
    [SerializeField] private Animator animator;
    [Tooltip("클립을 평가할 Animator 레이어 인덱스.")]
    [SerializeField, Min(0)] private int layerIndex = 0;

    [Header("Mapping")]
    [Tooltip("자원 0일 때의 클립 normalizedTime.")]
    [SerializeField, Range(0f, 1f)] private float emptyNormalizedTime = 0f;
    [Tooltip("자원이 가득 찼을 때의 클립 normalizedTime.")]
    [SerializeField, Range(0f, 1f)] private float fullNormalizedTime = 1f;

    private int _stateHash;
    private bool _hashCached;

    private void Reset()
    {
        source = Object.FindFirstObjectByType<PlayerSlowMotion>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (source == null)
        {
            source = Object.FindFirstObjectByType<PlayerSlowMotion>();
        }

        _hashCached = false;
        if (animator != null)
        {
            animator.speed = 0f;
        }
    }

    private void Start()
    {
        // 시작 첫 프레임에 Animator가 원래 normalizedTime 0으로 한 프레임 깜빡이는 걸 막는다.
        SyncToCharges();
    }

    private void LateUpdate()
    {
        SyncToCharges();
    }

    private void SyncToCharges()
    {
        if (animator == null || source == null)
        {
            return;
        }

        if (!_hashCached)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layerIndex);
            _stateHash = info.shortNameHash != 0 ? info.shortNameHash : info.fullPathHash;
            _hashCached = _stateHash != 0;
            if (!_hashCached)
            {
                return;
            }
        }

        float fill = Mathf.Clamp01(source.ChargeFillNormalized);
        float target = Mathf.Lerp(emptyNormalizedTime, fullNormalizedTime, fill);
        // Looping 클립에서 normalizedTime=1.0은 0으로 wrap된다. 1.0 직전까지로 clamp해 깜빡임 방지.
        target = Mathf.Clamp(target, 0f, 0.9999f);
        animator.Play(_stateHash, layerIndex, target);
        animator.Update(0f);
    }
}
