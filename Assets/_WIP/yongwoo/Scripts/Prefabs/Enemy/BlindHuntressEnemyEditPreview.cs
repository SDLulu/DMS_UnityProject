using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 역할:
// - 편집 모드에서 Blind Huntress 적의 특정 애니메이션 프레임을 고정해 보여줍니다.
// - 센서/히트박스를 맞출 때 Animator 상태가 풀리지 않도록 돕는 에디터 전용 프리뷰 보조입니다.

[ExecuteAlways]
[DisallowMultipleComponent]
public class BlindHuntressEnemyEditPreview : MonoBehaviour
{
    private enum PreviewState
    {
        Idle,
        Run,
        Jump,
        Fall,
        Dash,
        Attack,
        DashAttack,
        UpAttack,
        Hit,
        Death
    }

    [Header("Edit Preview")]
    [Tooltip("켜면 플레이 모드가 아닐 때 선택한 애니메이션 프레임을 계속 고정해서 보여줍니다.")]
    [SerializeField] private bool enablePreview;
    [Tooltip("애니메이션을 재생할 비주얼 루트입니다. 보통 Visual 자식을 넣습니다.")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("센서를 맞출 때 기준으로 볼 상태입니다.")]
    [SerializeField] private PreviewState previewState = PreviewState.DashAttack;
    [Tooltip("애니메이션의 어느 시점을 볼지 0~1 사이로 고정합니다. 0은 시작, 1은 끝입니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float normalizedTime = 0.2f;

    private Animator _animator;
    private int _lastStateHash;
    private float _lastNormalizedTime = -1f;
    private bool _lastEnablePreview;

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            return;
        }

        CacheReferences();
        ApplyPreview(force: true);
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        CacheReferences();
        ApplyPreview(force: true);
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            return;
        }

        CacheReferences();
        ApplyPreview(force: false);
    }

    private void CacheReferences()
    {
        if (visualRoot == null)
        {
            visualRoot = transform.Find("Visual");
        }

        if (_animator == null && visualRoot != null)
        {
            _animator = visualRoot.GetComponent<Animator>();
        }
    }

    private void ApplyPreview(bool force)
    {
#if UNITY_EDITOR
        if (_animator == null || _animator.runtimeAnimatorController == null)
        {
            return;
        }

        int stateHash = Animator.StringToHash("Base Layer." + GetStateName(previewState));
        float clampedTime = Mathf.Clamp01(normalizedTime);

        if (!enablePreview)
        {
            _lastEnablePreview = false;
            return;
        }

        if (!force &&
            _lastEnablePreview &&
            _lastStateHash == stateHash &&
            Mathf.Approximately(_lastNormalizedTime, clampedTime))
        {
            return;
        }

        _animator.Play(stateHash, 0, clampedTime);
        _animator.Update(0f);

        _lastEnablePreview = true;
        _lastStateHash = stateHash;
        _lastNormalizedTime = clampedTime;

        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
#endif
    }

    private static string GetStateName(PreviewState state)
    {
        return state switch
        {
            PreviewState.Idle => "Idle",
            PreviewState.Run => "Run",
            PreviewState.Jump => "Jump",
            PreviewState.Fall => "Fall",
            PreviewState.Dash => "Dash",
            PreviewState.Attack => "Attack",
            PreviewState.DashAttack => "DashAttack",
            PreviewState.UpAttack => "IdleUpAttack",
            PreviewState.Hit => "Hit",
            PreviewState.Death => "Death",
            _ => "Idle"
        };
    }
}
