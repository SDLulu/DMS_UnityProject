using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 역할:
// - Blind Huntress 적의 실제 액션 실행, 히트박스, 쿨다운을 관리합니다.
// - Brain은 어떤 행동을 쓸지만 정하고, 실제 움직임과 판정은 이 스크립트가 맡습니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class BlindHuntressEnemyCombat : MonoBehaviour
{
    private enum EnemyAction
    {
        None,
        Attack,
        Dash,
        DashAttack,
        Jump,
        UpAttack
    }

    [Serializable]
    private struct ActionConfig
    {
        [Tooltip("이 행동에서 재생할 애니메이션 상태 이름입니다. Animator 상태 이름과 정확히 같아야 합니다.")]
        public string animationState;
        [Tooltip("행동 종료 후 다시 같은 행동을 쓰기 전까지 기다리는 시간입니다.")]
        public float cooldown;
        [Tooltip("행동 전체 길이입니다. 이 시간이 끝나면 다른 행동으로 넘어갈 수 있습니다.")]
        public float duration;
        [Tooltip("실제로 속도를 강제로 넣는 시간입니다. duration보다 짧게 두면 후반은 관성만 남깁니다.")]
        public float motionDuration;
        [Tooltip("히트박스가 켜지기 시작하는 시간입니다.")]
        public float activeStart;
        [Tooltip("히트박스가 꺼지는 시간입니다.")]
        public float activeEnd;
        [Tooltip("켜면 activeStart/activeEnd 대신 Animation Event로 판정 시작/종료를 제어합니다.")]
        public bool useAnimationEvents;
        [Tooltip("히트박스 크기입니다. 위치는 위 References의 히트박스 앵커 Transform에서 맞춥니다.")]
        public Vector2 hitboxSize;
        [Tooltip("행동 중 강제로 줄 좌우 속도입니다. 대시/점프 접근에 주로 씁니다.")]
        public float horizontalSpeed;
        [Tooltip("행동 시작 시 줄 Y 속도입니다. 점프 행동에 주로 씁니다.")]
        public float verticalImpulse;
        [Tooltip("행동 중 중력을 꺼야 하면 켭니다. 대시 계열에 사용합니다.")]
        public bool zeroGravity;
        [Tooltip("행동 중 X축 이동을 0으로 묶고 싶을 때 켭니다.")]
        public bool freezeHorizontal;
        [Tooltip("행동 종료 순간 좌우 속도를 0으로 끊고 싶을 때 켭니다.")]
        public bool clearHorizontalOnEnd;
        [Tooltip("행동이 끝난 뒤 애니메이션을 조금 더 유지하는 시간입니다.")]
        public float animationRecovery;
    }

    [Header("References")]
    [Tooltip("기본 공격 히트박스 앵커입니다. 오른쪽을 바라보는 기준으로 위치를 맞춥니다.")]
    [SerializeField] private Transform attackHitboxAnchor;
    [Tooltip("대시 공격 히트박스 앵커입니다. 오른쪽을 바라보는 기준으로 위치를 맞춥니다.")]
    [SerializeField] private Transform dashAttackHitboxAnchor;
    [Tooltip("위 공격 히트박스 앵커입니다. 보통 캐릭터 머리 위쪽에 둡니다.")]
    [SerializeField] private Transform upAttackHitboxAnchor;

    [Header("Hit")]
    [Tooltip("맞출 대상 레이어입니다. 적 프리팹에서는 보통 Player만 넣습니다.")]
    [SerializeField] private LayerMask hitLayers;
    [Tooltip("한 번 맞을 때 들어가는 데미지입니다.")]
    [SerializeField] private float damage = 1f;
    [Tooltip("맞은 대상을 좌우로 밀어내는 힘입니다.")]
    [SerializeField] private float knockbackForce = 6f;
    [Tooltip("맞은 대상을 위로 띄우는 힘입니다.")]
    [SerializeField] private float knockbackUpForce = 2f;

    [Header("Action - Attack")]
    [SerializeField] private ActionConfig attackAction = new ActionConfig
    {
        animationState = "Attack",
        cooldown = 0.7f,
        duration = 0.24f,
        motionDuration = 0.24f,
        activeStart = 0.02f,
        activeEnd = 0.14f,
        useAnimationEvents = true,
        hitboxSize = new Vector2(0.92f, 0.72f),
        horizontalSpeed = 0f,
        verticalImpulse = 0f,
        zeroGravity = false,
        freezeHorizontal = true,
        clearHorizontalOnEnd = true,
        animationRecovery = 0.03f
    };

    [Header("Action - Dash")]
    [SerializeField] private ActionConfig dashAction = new ActionConfig
    {
        animationState = "Dash",
        cooldown = 1.4f,
        duration = 0.18f,
        motionDuration = 0.18f,
        activeStart = 0f,
        activeEnd = 0f,
        useAnimationEvents = false,
        hitboxSize = Vector2.zero,
        horizontalSpeed = 12f,
        verticalImpulse = 0f,
        zeroGravity = true,
        freezeHorizontal = false,
        clearHorizontalOnEnd = true,
        animationRecovery = 0.08f
    };

    [Header("Action - Dash Attack")]
    [SerializeField] private ActionConfig dashAttackAction = new ActionConfig
    {
        animationState = "DashAttack",
        cooldown = 1.3f,
        duration = 0.3f,
        motionDuration = 0.26f,
        activeStart = 0.04f,
        activeEnd = 0.24f,
        useAnimationEvents = true,
        hitboxSize = new Vector2(1.12f, 0.68f),
        horizontalSpeed = 11.5f,
        verticalImpulse = 0f,
        zeroGravity = true,
        freezeHorizontal = false,
        clearHorizontalOnEnd = true,
        animationRecovery = 0.12f
    };

    [Header("Action - Jump")]
    [SerializeField] private ActionConfig jumpAction = new ActionConfig
    {
        animationState = "Jump",
        cooldown = 1.1f,
        duration = 0.28f,
        motionDuration = 0.18f,
        activeStart = 0f,
        activeEnd = 0f,
        useAnimationEvents = false,
        hitboxSize = Vector2.zero,
        horizontalSpeed = 4.6f,
        verticalImpulse = 9.8f,
        zeroGravity = false,
        freezeHorizontal = false,
        clearHorizontalOnEnd = false,
        animationRecovery = 0.05f
    };

    [Header("Action - Up Attack")]
    [SerializeField] private ActionConfig upAttackAction = new ActionConfig
    {
        animationState = "IdleUpAttack",
        cooldown = 0.9f,
        duration = 0.28f,
        motionDuration = 0.28f,
        activeStart = 0.04f,
        activeEnd = 0.16f,
        useAnimationEvents = true,
        hitboxSize = new Vector2(0.82f, 1.12f),
        horizontalSpeed = 0f,
        verticalImpulse = 0f,
        zeroGravity = false,
        freezeHorizontal = true,
        clearHorizontalOnEnd = true,
        animationRecovery = 0.04f
    };

    private readonly HashSet<MonoBehaviour> _alreadyHit = new();

    private Rigidbody2D _body;
    private float _defaultGravityScale;
    private EnemyAction _activeAction;
    private float _activeTimer;
    private float _activeElapsed;
    private float _facing = 1f;
    private string _animationOverrideState;
    private float _animationOverrideTimer;
    private bool _hitboxWindowOpen;
    private EnemyAction _hitboxWindowAction;

    private float _attackCooldownTimer;
    private float _dashCooldownTimer;
    private float _dashAttackCooldownTimer;
    private float _jumpCooldownTimer;
    private float _upAttackCooldownTimer;

    public bool IsBusy => _activeAction != EnemyAction.None;
    public bool HasAnimationOverride => !string.IsNullOrWhiteSpace(_animationOverrideState) && _animationOverrideTimer > 0f;
    public string CurrentAnimationStateName => _animationOverrideState;
    public bool CanUseAttack => CanStart(EnemyAction.Attack);
    public bool CanUseDash => CanStart(EnemyAction.Dash);
    public bool CanUseDashAttack => CanStart(EnemyAction.DashAttack);
    public bool CanUseJump => CanStart(EnemyAction.Jump);
    public bool CanUseUpAttack => CanStart(EnemyAction.UpAttack);

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _defaultGravityScale = _body != null ? _body.gravityScale : 1f;

        if (hitLayers.value == 0)
        {
            hitLayers = LayerMask.GetMask("Player");
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        TickCooldowns(dt);
        TickAnimationOverride(dt);

        if (IsBusy)
        {
            TickActiveAction(dt);
        }
    }

    public bool TryStartAttack(float facingDirection)
    {
        return TryStartAction(EnemyAction.Attack, facingDirection);
    }

    public bool TryStartDash(float facingDirection)
    {
        return TryStartAction(EnemyAction.Dash, facingDirection);
    }

    public bool TryStartDashAttack(float facingDirection)
    {
        return TryStartAction(EnemyAction.DashAttack, facingDirection);
    }

    public bool TryStartJump(float facingDirection)
    {
        return TryStartAction(EnemyAction.Jump, facingDirection);
    }

    public bool TryStartUpAttack(float facingDirection)
    {
        return TryStartAction(EnemyAction.UpAttack, facingDirection);
    }

    private bool TryStartAction(EnemyAction action, float facingDirection)
    {
        if (!CanStart(action))
        {
            return false;
        }

        ActionConfig config = GetConfig(action);
        _activeAction = action;
        _activeTimer = config.duration;
        _activeElapsed = 0f;
        _facing = Mathf.Approximately(facingDirection, 0f) ? 1f : Mathf.Sign(facingDirection);
        _animationOverrideState = config.animationState;
        _animationOverrideTimer = config.duration + config.animationRecovery;
        _hitboxWindowOpen = false;
        _hitboxWindowAction = EnemyAction.None;
        _alreadyHit.Clear();
        SetCooldown(action, config.cooldown);

        ApplyActionStartImpulse(config);
        return true;
    }

    private void TickCooldowns(float dt)
    {
        _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - dt);
        _dashCooldownTimer = Mathf.Max(0f, _dashCooldownTimer - dt);
        _dashAttackCooldownTimer = Mathf.Max(0f, _dashAttackCooldownTimer - dt);
        _jumpCooldownTimer = Mathf.Max(0f, _jumpCooldownTimer - dt);
        _upAttackCooldownTimer = Mathf.Max(0f, _upAttackCooldownTimer - dt);
    }

    private void TickAnimationOverride(float dt)
    {
        if (_animationOverrideTimer <= 0f)
        {
            return;
        }

        _animationOverrideTimer -= dt;
        if (_animationOverrideTimer <= 0f)
        {
            _animationOverrideState = null;
        }
    }

    private void TickActiveAction(float dt)
    {
        ActionConfig config = GetConfig(_activeAction);
        _activeTimer -= dt;
        _activeElapsed += dt;

        ApplyActionMotion(config);

        if (ShouldTickHitbox(_activeAction, config))
        {
            TickHitDetection(_activeAction, config);
        }

        if (_activeTimer > 0f)
        {
            return;
        }

        FinishAction(config);
    }

    private void FinishAction(ActionConfig config)
    {
        if (_body != null)
        {
            _body.gravityScale = _defaultGravityScale;

            if (config.clearHorizontalOnEnd)
            {
                Vector2 velocity = _body.linearVelocity;
                velocity.x = 0f;
                _body.linearVelocity = velocity;
            }
        }

        _activeAction = EnemyAction.None;
        _activeTimer = 0f;
        _activeElapsed = 0f;
        _hitboxWindowOpen = false;
        _hitboxWindowAction = EnemyAction.None;
    }

    public void AnimationEvent_BeginAttackHitbox()
    {
        SetHitboxWindow(EnemyAction.Attack, true);
    }

    public void AnimationEvent_EndAttackHitbox()
    {
        SetHitboxWindow(EnemyAction.Attack, false);
    }

    public void AnimationEvent_BeginDashAttackHitbox()
    {
        SetHitboxWindow(EnemyAction.DashAttack, true);
    }

    public void AnimationEvent_EndDashAttackHitbox()
    {
        SetHitboxWindow(EnemyAction.DashAttack, false);
    }

    public void AnimationEvent_BeginUpAttackHitbox()
    {
        SetHitboxWindow(EnemyAction.UpAttack, true);
    }

    public void AnimationEvent_EndUpAttackHitbox()
    {
        SetHitboxWindow(EnemyAction.UpAttack, false);
    }

    private void ApplyActionStartImpulse(ActionConfig config)
    {
        if (_body == null)
        {
            return;
        }

        _body.gravityScale = config.zeroGravity ? 0f : _defaultGravityScale;

        Vector2 velocity = _body.linearVelocity;
        if (config.freezeHorizontal)
        {
            velocity.x = 0f;
        }
        else if (!Mathf.Approximately(config.horizontalSpeed, 0f))
        {
            velocity.x = _facing * config.horizontalSpeed;
        }

        if (!Mathf.Approximately(config.verticalImpulse, 0f))
        {
            velocity.y = config.verticalImpulse;
        }

        _body.linearVelocity = velocity;
    }

    private void ApplyActionMotion(ActionConfig config)
    {
        if (_body == null)
        {
            return;
        }

        _body.gravityScale = config.zeroGravity ? 0f : _defaultGravityScale;

        Vector2 velocity = _body.linearVelocity;
        if (config.freezeHorizontal)
        {
            velocity.x = 0f;
        }
        else if (!Mathf.Approximately(config.horizontalSpeed, 0f) && _activeElapsed <= config.motionDuration)
        {
            velocity.x = _facing * config.horizontalSpeed;
        }

        _body.linearVelocity = velocity;
    }

    private void TickHitDetection(EnemyAction action, ActionConfig config)
    {
        Vector2 center = GetHitboxCenter(action);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, config.hitboxSize, 0f, hitLayers);
        Vector2 knockback = new Vector2(_facing * knockbackForce, knockbackUpForce);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            MonoBehaviour receiver = ResolveDamageReceiver(hit);
            if (receiver == null || _alreadyHit.Contains(receiver) || receiver is not IDamageReceiver damageReceiver)
            {
                continue;
            }

            _alreadyHit.Add(receiver);
            damageReceiver.ReceiveHit(damage, knockback, gameObject);
        }
    }

    private Vector2 GetHitboxCenter(EnemyAction action)
    {
        Transform anchor = GetHitboxAnchor(action);
        return GetMirroredAnchorPosition(anchor);
    }

    private Transform GetHitboxAnchor(EnemyAction action)
    {
        return action switch
        {
            EnemyAction.Attack => attackHitboxAnchor,
            EnemyAction.DashAttack => dashAttackHitboxAnchor != null ? dashAttackHitboxAnchor : attackHitboxAnchor,
            EnemyAction.UpAttack => upAttackHitboxAnchor != null ? upAttackHitboxAnchor : attackHitboxAnchor,
            _ => null
        };
    }

    private Vector2 GetMirroredAnchorPosition(Transform anchor)
    {
        if (anchor == null)
        {
            return transform.position;
        }

        Vector3 local = anchor.localPosition;
        local.x = Mathf.Abs(local.x) * (_facing >= 0f ? 1f : -1f);
        Transform parent = anchor.parent != null ? anchor.parent : transform;
        return parent.TransformPoint(local);
    }

    private bool CanStart(EnemyAction action)
    {
        return !IsBusy && GetCooldown(action) <= 0f;
    }

    private float GetCooldown(EnemyAction action)
    {
        return action switch
        {
            EnemyAction.Attack => _attackCooldownTimer,
            EnemyAction.Dash => _dashCooldownTimer,
            EnemyAction.DashAttack => _dashAttackCooldownTimer,
            EnemyAction.Jump => _jumpCooldownTimer,
            EnemyAction.UpAttack => _upAttackCooldownTimer,
            _ => 0f
        };
    }

    private void SetCooldown(EnemyAction action, float value)
    {
        switch (action)
        {
            case EnemyAction.Attack:
                _attackCooldownTimer = value;
                break;
            case EnemyAction.Dash:
                _dashCooldownTimer = value;
                break;
            case EnemyAction.DashAttack:
                _dashAttackCooldownTimer = value;
                break;
            case EnemyAction.Jump:
                _jumpCooldownTimer = value;
                break;
            case EnemyAction.UpAttack:
                _upAttackCooldownTimer = value;
                break;
        }
    }

    private ActionConfig GetConfig(EnemyAction action)
    {
        return action switch
        {
            EnemyAction.Attack => attackAction,
            EnemyAction.Dash => dashAction,
            EnemyAction.DashAttack => dashAttackAction,
            EnemyAction.Jump => jumpAction,
            EnemyAction.UpAttack => upAttackAction,
            _ => default
        };
    }

    private static bool HasHitWindow(ActionConfig config)
    {
        return config.activeEnd > config.activeStart && config.hitboxSize.sqrMagnitude > 0.0001f;
    }

    private bool ShouldTickHitbox(EnemyAction action, ActionConfig config)
    {
        if (config.hitboxSize.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        if (config.useAnimationEvents)
        {
            return _hitboxWindowOpen && _hitboxWindowAction == action;
        }

        return HasHitWindow(config) && _activeElapsed >= config.activeStart && _activeElapsed <= config.activeEnd;
    }

    private void SetHitboxWindow(EnemyAction action, bool isOpen)
    {
        if (_activeAction != action)
        {
            return;
        }

        _hitboxWindowOpen = isOpen;
        _hitboxWindowAction = isOpen ? action : EnemyAction.None;
    }

    private static MonoBehaviour ResolveDamageReceiver(Collider2D hit)
    {
        MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour is IDamageReceiver)
            {
                return behaviour;
            }
        }

        return null;
    }

    private void OnDrawGizmos()
    {
        if (!ShouldDrawGizmos())
        {
            return;
        }

        DrawPreviewHitbox(attackHitboxAnchor, attackAction.hitboxSize, new Color(1f, 0.35f, 0.25f, 0.9f));
        DrawPreviewHitbox(dashAttackHitboxAnchor, dashAttackAction.hitboxSize, new Color(1f, 0.75f, 0.2f, 0.9f));
        DrawPreviewHitbox(upAttackHitboxAnchor, upAttackAction.hitboxSize, new Color(0.45f, 0.95f, 1f, 0.9f));
    }

    private void DrawPreviewHitbox(Transform anchor, Vector2 size, Color color)
    {
        if (anchor == null || size.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Gizmos.color = color;
        Gizmos.DrawWireCube(anchor.position, size);
    }

    private bool ShouldDrawGizmos()
    {
#if UNITY_EDITOR
        Transform selected = Selection.activeTransform;
        return selected != null && (selected == transform || selected.IsChildOf(transform));
#else
        return false;
#endif
    }
}
