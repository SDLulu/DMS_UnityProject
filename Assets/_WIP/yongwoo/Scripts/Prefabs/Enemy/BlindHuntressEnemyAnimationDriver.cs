using UnityEngine;

// 역할:
// - Blind Huntress 적의 이동/공격/피격 상태를 읽고 애니메이션만 갱신합니다.
// - Brain/Combat의 결과를 시각 상태로 번역하는 전용 표현 계층입니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(BlindHuntressEnemyBrain))]
[RequireComponent(typeof(BlindHuntressEnemyCombat))]
[RequireComponent(typeof(BlindHuntressEnemyInteraction))]
public class BlindHuntressEnemyAnimationDriver : MonoBehaviour
{
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int RunState = Animator.StringToHash("Base Layer.Run");
    private static readonly int JumpState = Animator.StringToHash("Base Layer.Jump");
    private static readonly int FallState = Animator.StringToHash("Base Layer.Fall");
    private static readonly int HitState = Animator.StringToHash("Base Layer.Hit");
    private static readonly int DeathState = Animator.StringToHash("Base Layer.Death");

    [Tooltip("애니메이션을 재생할 비주얼 루트입니다. 보통 Visual 자식 오브젝트를 넣습니다.")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("씬 인스턴스에서 Animator Controller가 빠졌을 때 다시 꽂아줄 기본 컨트롤러입니다.")]
    [SerializeField] private RuntimeAnimatorController fallbackController;
    [Tooltip("이 값보다 빨리 움직이면 Run으로 전환합니다.")]
    [SerializeField] private float runThreshold = 0.08f;
    [Tooltip("공중에서 이 값보다 위로 빨리 올라가면 Jump로 봅니다.")]
    [SerializeField] private float jumpThreshold = 0.2f;
    [Tooltip("공중에서 이 값보다 아래로 빨리 내려가면 Fall로 봅니다.")]
    [SerializeField] private float fallThreshold = -0.15f;
    [Tooltip("상태 전환 크로스페이드 시간입니다.")]
    [SerializeField] private float crossFadeDuration = 0.04f;
    [Tooltip("피격 애니메이션을 최소 유지할 시간입니다.")]
    [SerializeField] private float hitDuration = 0.16f;

    private Animator _animator;
    private Rigidbody2D _body;
    private BlindHuntressEnemyBrain _brain;
    private BlindHuntressEnemyCombat _combat;
    private BlindHuntressEnemyInteraction _interaction;
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

        float horizontalSpeed = _body != null ? Mathf.Abs(_body.linearVelocity.x) : 0f;
        float verticalSpeed = _body != null ? _body.linearVelocity.y : 0f;
        bool isGrounded = _brain != null && _brain.IsGroundedNow;

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
        _brain ??= GetComponent<BlindHuntressEnemyBrain>();
        _combat ??= GetComponent<BlindHuntressEnemyCombat>();
        _interaction ??= GetComponent<BlindHuntressEnemyInteraction>();

        if (visualRoot == null)
        {
            visualRoot = transform.Find("Visual");
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
            Animator directAnimator = visualRoot.GetComponent<Animator>();
            if (directAnimator != null)
            {
                return directAnimator;
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
