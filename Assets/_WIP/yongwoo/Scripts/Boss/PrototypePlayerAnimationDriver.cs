using UnityEngine;

[DisallowMultipleComponent]
public class PrototypePlayerAnimationDriver : MonoBehaviour
{
    private const float GroundedAnimationConfirmDuration = 0.06f;

    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int RunState = Animator.StringToHash("Base Layer.Run");
    private static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");
    private static readonly int JumpState = Animator.StringToHash("Base Layer.Jump");
    private static readonly int FallState = Animator.StringToHash("Base Layer.Fall");

    [SerializeField] private Transform visualRoot;
    [SerializeField] private float runThreshold = 0.05f;
    [SerializeField] private float jumpThreshold = 0.2f;
    [SerializeField] private float fallThreshold = -0.15f;
    [SerializeField] private float defaultAttackDuration = 0.28f;
    [SerializeField] private float crossFadeDuration = 0.04f;
    [SerializeField] private float attackVisualYOffset = 0f;
    [SerializeField] private float visualAnchorLerpSpeed = 16f;

    private Animator _animator;
    private Rigidbody2D _body;
    private SimplePlayerCombat _combat;
    private SimplePlayerController _controller;
    private Transform _visualTransform;
    private Vector3 _baseVisualLocalPosition;
    private float _attackTimer;
    private float _groundedConfirmTimer;
    private int _currentState;

    private void Awake()
    {
        CacheReferences();
        PlayState(IdleState, true);
    }

    private void OnEnable()
    {
        CacheReferences();
        if (_combat != null)
        {
            _combat.AttackPerformed += HandleAttackPerformed;
        }
    }

    private void OnDisable()
    {
        if (_combat != null)
        {
            _combat.AttackPerformed -= HandleAttackPerformed;
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

        UpdateVisualAnchor();

        if (_attackTimer > 0f)
        {
            _attackTimer -= Time.deltaTime;
            if (_currentState != AttackState)
            {
                PlayState(AttackState, true);
            }
            return;
        }

        float horizontalSpeed = _body != null ? Mathf.Abs(_body.linearVelocity.x) : 0f;
        float verticalSpeed = _body != null ? _body.linearVelocity.y : 0f;
        bool isGrounded = _controller != null && _controller.IsGroundedNow;
        _groundedConfirmTimer = isGrounded
            ? _groundedConfirmTimer + Time.deltaTime
            : 0f;

        int desiredState;
        bool groundedForAnimation = isGrounded && _groundedConfirmTimer >= GroundedAnimationConfirmDuration;
        if (!groundedForAnimation)
        {
            if (verticalSpeed > jumpThreshold)
            {
                desiredState = JumpState;
            }
            else if (verticalSpeed < fallThreshold)
            {
                desiredState = FallState;
            }
            else
            {
                desiredState = _currentState == FallState ? FallState : JumpState;
            }
        }
        else
        {
            desiredState = horizontalSpeed > runThreshold ? RunState : IdleState;
        }

        PlayState(desiredState, false);
    }

    private void HandleAttackPerformed()
    {
        _attackTimer = _combat != null ? Mathf.Max(defaultAttackDuration, _combat.AttackAnimationDuration) : defaultAttackDuration;
        PlayState(AttackState, true);
    }

    private void CacheReferences()
    {
        if (_body == null)
        {
            _body = GetComponent<Rigidbody2D>();
        }

        if (_combat == null)
        {
            _combat = GetComponent<SimplePlayerCombat>();
        }

        if (_controller == null)
        {
            _controller = GetComponent<SimplePlayerController>();
        }

        if (_animator == null)
        {
            if (visualRoot == null && _controller != null)
            {
                visualRoot = _controller.VisualRoot;
            }

            if (visualRoot == null)
            {
                visualRoot = transform.Find("Visual") ?? transform.Find("RobotMaidVisual");
            }

            if (visualRoot != null)
            {
                _visualTransform = visualRoot;
                _baseVisualLocalPosition = visualRoot.localPosition;
                _animator = visualRoot.GetComponent<Animator>();
            }
        }
    }

    private void UpdateVisualAnchor()
    {
        if (_visualTransform == null)
        {
            return;
        }

        Vector3 targetPosition = _baseVisualLocalPosition;
        if (_attackTimer > 0f)
        {
            targetPosition.y += attackVisualYOffset;
        }

        _visualTransform.localPosition = Vector3.Lerp(
            _visualTransform.localPosition,
            targetPosition,
            Time.deltaTime * visualAnchorLerpSpeed);
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
}
