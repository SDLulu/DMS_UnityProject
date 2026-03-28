using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PrototypeBossController))]
[RequireComponent(typeof(PrototypeHealth))]
public class PrototypeBossAnimationDriver : MonoBehaviour
{
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int TelegraphState = Animator.StringToHash("Base Layer.Telegraph");
    private static readonly int DashState = Animator.StringToHash("Base Layer.Dash");
    private static readonly int LeapState = Animator.StringToHash("Base Layer.Leap");
    private static readonly int ShootState = Animator.StringToHash("Base Layer.Shoot");
    private static readonly int HitState = Animator.StringToHash("Base Layer.Hit");

    [SerializeField] private float crossFadeDuration = 0.05f;
    [SerializeField] private float hitDuration = 0.18f;

    private Animator _animator;
    private PrototypeBossController _controller;
    private PrototypeHealth _health;
    private int _currentState;
    private float _hitTimer;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<PrototypeBossController>();
        _health = GetComponent<PrototypeHealth>();
        PlayState(IdleState, true);
    }

    private void OnEnable()
    {
        if (_health != null)
        {
            _health.Damaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.Damaged -= HandleDamaged;
        }
    }

    private void Update()
    {
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
            case PrototypeBossAnimationState.Telegraph:
                PlayState(TelegraphState, false);
                break;
            case PrototypeBossAnimationState.Dash:
                PlayState(DashState, false);
                break;
            case PrototypeBossAnimationState.Leap:
                PlayState(LeapState, false);
                break;
            case PrototypeBossAnimationState.Shoot:
                PlayState(ShootState, false);
                break;
            default:
                PlayState(IdleState, false);
                break;
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
