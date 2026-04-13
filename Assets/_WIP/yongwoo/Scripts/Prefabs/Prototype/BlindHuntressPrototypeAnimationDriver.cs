using UnityEngine;

// 역할:
// - Blind Huntress 프로토타입의 이동/공격/피격 상태를 읽어 비주얼 애니메이션만 갱신합니다.
// - 전투 규칙은 건드리지 않고 표현 상태 전환만 맡습니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(SimplePlayerController))]
[RequireComponent(typeof(PlayerInteraction))]
[RequireComponent(typeof(BlindHuntressPrototypeCombat))]
public class BlindHuntressPrototypeAnimationDriver : MonoBehaviour
{
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int RunState = Animator.StringToHash("Base Layer.Run");
    private static readonly int JumpState = Animator.StringToHash("Base Layer.Jump");
    private static readonly int FallState = Animator.StringToHash("Base Layer.Fall");
    private static readonly int DashState = Animator.StringToHash("Base Layer.Dash");
    private static readonly int HitState = Animator.StringToHash("Base Layer.Hit");
    private static readonly int DeathState = Animator.StringToHash("Base Layer.Death");

    [SerializeField] private Transform visualRoot;
    [SerializeField] private RuntimeAnimatorController fallbackController;
    [SerializeField] private float runThreshold = 0.08f;
    [SerializeField] private float jumpThreshold = 0.2f;
    [SerializeField] private float fallThreshold = -0.15f;
    [SerializeField] private float crossFadeDuration = 0.04f;
    [SerializeField] private float hitDuration = 0.16f;

    private Animator _animator;
    private Rigidbody2D _body;
    private SimplePlayerController _controller;
    private PlayerInteraction _interaction;
    private BlindHuntressPrototypeCombat _combat;
    private float _hitTimer;
    private int _currentState;

    private void Awake()
    {
        CacheReferences();
        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            PlayState(IdleState, true);
        }
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
        if (_animator == null)
        {
            CacheReferences();
            if (_animator == null)
            {
                return;
            }
        }

        if (_interaction != null && _interaction.IsDead)
        {
            PlayState(DeathState, false);
            return;
        }

        if (_hitTimer > 0f)
        {
            _hitTimer -= Time.deltaTime;
            PlayState(HitState, false);
            return;
        }

        if (_combat != null && _combat.HasAnimationOverride)
        {
            PlayStateByName(_combat.CurrentAnimationStateName, false);
            return;
        }

        if (_controller != null && (_controller.IsDashing || _controller.IsRolling))
        {
            PlayState(DashState, false);
            return;
        }

        float horizontalSpeed = _body != null ? Mathf.Abs(_body.linearVelocity.x) : 0f;
        float verticalSpeed = _body != null ? _body.linearVelocity.y : 0f;
        bool isGrounded = _controller != null && _controller.IsGroundedNow;

        if (!isGrounded)
        {
            PlayState(verticalSpeed > jumpThreshold ? JumpState : FallState, false);
            return;
        }

        if (verticalSpeed < fallThreshold)
        {
            PlayState(FallState, false);
            return;
        }

        PlayState(horizontalSpeed > runThreshold ? RunState : IdleState, false);
    }

    private void CacheReferences()
    {
        _body ??= GetComponent<Rigidbody2D>();
        _controller ??= GetComponent<SimplePlayerController>();
        _interaction ??= GetComponent<PlayerInteraction>();
        _combat ??= GetComponent<BlindHuntressPrototypeCombat>();

        if (visualRoot == null && _controller != null)
        {
            visualRoot = _controller.VisualRoot;
        }

        if (_animator == null)
        {
            _animator = ResolveAnimator();
        }

        if (_animator != null && _animator.runtimeAnimatorController == null && fallbackController != null)
        {
            _animator.runtimeAnimatorController = fallbackController;
        }
    }

    private Animator ResolveAnimator()
    {
        if (visualRoot != null)
        {
            Animator visualAnimator = visualRoot.GetComponent<Animator>();
            if (visualAnimator != null)
            {
                return visualAnimator;
            }

            Animator[] visualAnimators = visualRoot.GetComponentsInChildren<Animator>(includeInactive: true);
            for (int i = 0; i < visualAnimators.Length; i++)
            {
                if (visualAnimators[i] != null)
                {
                    return visualAnimators[i];
                }
            }
        }

        Animator[] animators = GetComponentsInChildren<Animator>(includeInactive: true);
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null && animators[i].runtimeAnimatorController != null)
            {
                return animators[i];
            }
        }

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
            {
                return animators[i];
            }
        }

        return null;
    }

    private void HandleDamaged()
    {
        _hitTimer = hitDuration;
        PlayState(HitState, true);
    }

    private void PlayState(int stateHash, bool restart)
    {
        if (_animator == null)
        {
            return;
        }

        if (!restart && _currentState == stateHash)
        {
            return;
        }

        _currentState = stateHash;
        _animator.CrossFade(stateHash, crossFadeDuration, 0, restart ? 0f : float.NegativeInfinity);
    }

    private void PlayStateByName(string stateName, bool restart)
    {
        if (_animator == null || string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        int stateHash = Animator.StringToHash($"Base Layer.{stateName}");
        PlayState(stateHash, restart);
    }
}
