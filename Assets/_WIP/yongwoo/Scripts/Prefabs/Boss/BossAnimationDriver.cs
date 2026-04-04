using UnityEngine;

// 역할:
// - BossController와 BossInteraction 상태를 읽어 보스 비주얼만 갱신합니다.
// - 전투 규칙을 바꾸지 않고 애니메이터 파라미터와 스프라이트 표현만 책임집니다.
//
// 구조 포인트:
// - 전투 판단은 BossController에 두고, 이 파일은 표현 계층으로만 유지합니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(BossController))]
[RequireComponent(typeof(BossInteraction))]
public class BossAnimationDriver : MonoBehaviour
{
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int TelegraphState = Animator.StringToHash("Base Layer.Telegraph");
    private static readonly int DashState = Animator.StringToHash("Base Layer.Dash");
    private static readonly int LeapState = Animator.StringToHash("Base Layer.Leap");
    private static readonly int ShootState = Animator.StringToHash("Base Layer.Shoot");
    private static readonly int HitState = Animator.StringToHash("Base Layer.Hit");

    [SerializeField] private Animator visualAnimator;
    [SerializeField] private float crossFadeDuration = 0.05f;
    [SerializeField] private float hitDuration = 0.18f;

    private Animator _animator;
    private BossController _controller;
    private BossInteraction _interaction;
    private int _currentState;
    private float _hitTimer;

    private void Awake()
    {
        CacheReferences();
        PlayState(IdleState, true);
    }

    private void OnEnable()
    {
        CacheReferences();
        if (_interaction != null)
        {
            _interaction.Damaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (_interaction != null)
        {
            _interaction.Damaged -= HandleDamaged;
        }
    }

    private void Update()
    {
        if (_animator == null || _controller == null)
        {
            CacheReferences();
        }

        if (_animator == null || _controller == null)
        {
            return;
        }

        if (_hitTimer > 0f)
        {
            _hitTimer -= Time.deltaTime;
            PlayState(HitState, false);
            return;
        }

        switch (_controller.AnimationState)
        {
            case BossAnimationState.Telegraph:
                PlayState(TelegraphState, false);
                break;
            case BossAnimationState.Dash:
                PlayState(DashState, false);
                break;
            case BossAnimationState.Leap:
                PlayState(LeapState, false);
                break;
            case BossAnimationState.Shoot:
                PlayState(ShootState, false);
                break;
            default:
                PlayState(IdleState, false);
                break;
        }
    }

    private void CacheReferences()
    {
        if (_controller == null)
        {
            _controller = GetComponent<BossController>();
        }

        if (_interaction == null)
        {
            _interaction = GetComponent<BossInteraction>();
        }

        if (_animator == null)
        {
            if (visualAnimator == null)
            {
                visualAnimator = GetComponent<Animator>();
            }

            if (visualAnimator == null)
            {
                visualAnimator = GetComponentInChildren<Animator>();
            }

            _animator = visualAnimator;
        }
    }

    private void HandleDamaged()
    {
        _hitTimer = hitDuration;
        PlayState(HitState, true);
    }

    private void PlayState(int stateHash, bool restart)
    {
        if (!restart && _currentState == stateHash)
        {
            return;
        }

        _currentState = stateHash;
        _animator.CrossFade(stateHash, crossFadeDuration, 0, restart ? 0f : float.NegativeInfinity);
    }
}
