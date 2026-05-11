using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// 역할:
// - Blind Huntress 프로토타입의 근접 공격 입력과 판정을 관리합니다.
// - 손/무기 프리팹 없이도 공격 감각을 먼저 검증할 수 있게 만듭니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(SimplePlayerController))]
public class BlindHuntressPrototypeCombat : MonoBehaviour
{
    private static readonly FieldInfo InvertVisualFacingField = typeof(SimplePlayerController)
        .GetField("invertVisualFacing", BindingFlags.Instance | BindingFlags.NonPublic);

    private enum SkillType
    {
        None,
        Attack,
        Attack3,
        DashAttack,
        SpecialDash,
        IdleUpAttack,
        JumpUpAttack,
        JumpDownAttack
    }

    private readonly struct SkillConfig
    {
        public SkillConfig(
            string animationState,
            float duration,
            float activeStart,
            float activeEnd,
            Vector2 hitboxOffset,
            Vector2 hitboxSize,
            float dashSpeed,
            float impulseY,
            bool zeroGravity,
            bool freezeX,
            float animationRecovery,
            bool holdZeroGravityAfterMotion,
            float endPositionOffset)
        {
            AnimationState = animationState;
            Duration = duration;
            ActiveStart = activeStart;
            ActiveEnd = activeEnd;
            HitboxOffset = hitboxOffset;
            HitboxSize = hitboxSize;
            DashSpeed = dashSpeed;
            ImpulseY = impulseY;
            ZeroGravity = zeroGravity;
            FreezeX = freezeX;
            AnimationRecovery = animationRecovery;
            HoldZeroGravityAfterMotion = holdZeroGravityAfterMotion;
            EndPositionOffset = endPositionOffset;
        }

        public string AnimationState { get; }
        public float Duration { get; }
        public float ActiveStart { get; }
        public float ActiveEnd { get; }
        public Vector2 HitboxOffset { get; }
        public Vector2 HitboxSize { get; }
        public float DashSpeed { get; }
        public float ImpulseY { get; }
        public bool ZeroGravity { get; }
        public bool FreezeX { get; }
        public float AnimationRecovery { get; }
        public bool HoldZeroGravityAfterMotion { get; }
        public float EndPositionOffset { get; }
    }

    [Header("Timing")]
    [SerializeField] private float attackCooldown = 0.32f;
    [SerializeField] private float attackActiveDuration = 0.14f;
    [SerializeField] private float attackAnimationDuration = 0.24f;

    [Header("Hitbox")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private Vector2 attackOffset = new Vector2(0.62f, -0.18f);
    [SerializeField] private Vector2 attackSize = new Vector2(0.9f, 0.65f);
    [SerializeField] private LayerMask hitLayers;

    [Header("Damage")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackUpForce = 2f;

    private readonly HashSet<MonoBehaviour> _alreadyHit = new();
    private SimplePlayerController _controller;
    private Rigidbody2D _body;
    private float _cooldownTimer;
    private float _skillTimer;
    private float _skillElapsed;
    private float _defaultGravityScale;
    private SkillType _activeSkill;
    private string _animationOverrideState;
    private float _animationOverrideTimer;
    private bool _holdZeroGravityAfterMotion;

    public bool IsAttacking => _skillTimer > 0f;
    public bool IsSkillActive => _activeSkill != SkillType.None;
    public bool HasAnimationOverride => !string.IsNullOrWhiteSpace(_animationOverrideState) && _animationOverrideTimer > 0f;
    public string CurrentAnimationStateName => HasAnimationOverride ? _animationOverrideState : GetSkillConfig(_activeSkill).AnimationState;

    private void Awake()
    {
        _controller = GetComponent<SimplePlayerController>();
        _body = GetComponent<Rigidbody2D>();
        _defaultGravityScale = _body != null ? _body.gravityScale : 1f;
        ForceCorrectFacingDirection();

        if (_body != null)
        {
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        if (hitLayers.value == 0)
        {
            hitLayers = LayerMask.GetMask("Enemy");
        }
    }

    private void ForceCorrectFacingDirection()
    {
        if (_controller == null || InvertVisualFacingField == null)
        {
            return;
        }

        InvertVisualFacingField.SetValue(_controller, false);
        _controller.SetExternalFacing(_controller.FacingDirection, true);
        _controller.SetExternalFacing(_controller.FacingDirection, false);
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
        }

        if (_animationOverrideTimer > 0f)
        {
            _animationOverrideTimer -= Time.deltaTime;
            if (_animationOverrideTimer <= 0f)
            {
                _animationOverrideState = null;
                _holdZeroGravityAfterMotion = false;
            }
        }

        if (IsSkillActive)
        {
            TickActiveSkill(Time.deltaTime);
            return;
        }

        if (_cooldownTimer > 0f)
        {
            return;
        }

        if (TryStartMappedSkill())
        {
            return;
        }

        if (GameInput.Instance.AttackPressed)
        {
            StartSkill(SkillType.Attack);
        }
    }

    private bool TryStartMappedSkill()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) return StartSkill(SkillType.Attack3);
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) return StartSkill(SkillType.DashAttack);
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) return StartSkill(SkillType.SpecialDash);
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) return StartSkill(SkillType.IdleUpAttack);
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) return StartSkill(SkillType.JumpUpAttack);
        if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) return StartSkill(SkillType.JumpDownAttack);
        if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) return StartSkill(SkillType.Attack);
        return false;
    }

    private bool StartSkill(SkillType skill)
    {
        SkillConfig config = GetSkillConfig(skill);
        if (string.IsNullOrWhiteSpace(config.AnimationState))
        {
            return false;
        }

        _activeSkill = skill;
        _cooldownTimer = attackCooldown;
        _skillTimer = config.Duration;
        _skillElapsed = 0f;
        _animationOverrideState = config.AnimationState;
        _animationOverrideTimer = config.Duration + config.AnimationRecovery;
        _holdZeroGravityAfterMotion = config.HoldZeroGravityAfterMotion;
        _alreadyHit.Clear();
        if (_controller != null)
        {
            _controller.enabled = false;
        }

        ApplySkillStartImpulse(config);
        return true;
    }

    private void TickActiveSkill(float deltaTime)
    {
        SkillConfig config = GetSkillConfig(_activeSkill);
        _skillTimer -= deltaTime;
        _skillElapsed += deltaTime;

        ApplySkillMotion(config);

        if (_skillElapsed >= config.ActiveStart && _skillElapsed <= config.ActiveEnd)
        {
            TickHitDetection(config);
        }

        if (_skillTimer > 0f)
        {
            return;
        }

        ApplySkillEndCorrection(config);

        if (_body != null)
        {
            _body.gravityScale = _holdZeroGravityAfterMotion ? 0f : _defaultGravityScale;
        }

        if (_controller != null)
        {
            _controller.enabled = true;
        }

        _activeSkill = SkillType.None;
        _skillTimer = 0f;
        _skillElapsed = 0f;
    }

    private void TickHitDetection(SkillConfig config)
    {
        Vector2 center = GetAttackCenter(config.HitboxOffset);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, config.HitboxSize, 0f, hitLayers);
        Vector2 knockback = new Vector2(GetFacingDirection() * knockbackForce, knockbackUpForce);

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
            if (damageReceiver.ReceiveHit(damage, knockback, gameObject))
            {
                CombatHitFeedback.PlayLightHit();
            }
        }
    }

    private void ApplySkillStartImpulse(SkillConfig config)
    {
        if (_body == null)
        {
            return;
        }

        _body.gravityScale = config.ZeroGravity ? 0f : _defaultGravityScale;

        Vector2 velocity = _body.linearVelocity;
        velocity.x = config.DashSpeed != 0f ? GetFacingDirection() * config.DashSpeed : velocity.x;
        if (config.FreezeX)
        {
            velocity.x = 0f;
        }

        if (!Mathf.Approximately(config.ImpulseY, 0f))
        {
            velocity.y = config.ImpulseY;
        }

        _body.linearVelocity = velocity;
    }

    private void ApplySkillEndCorrection(SkillConfig config)
    {
        if (_body == null || Mathf.Approximately(config.EndPositionOffset, 0f))
        {
            return;
        }

        _body.position += Vector2.right * (GetFacingDirection() * config.EndPositionOffset);
    }

    private void ApplySkillMotion(SkillConfig config)
    {
        if (_body == null)
        {
            return;
        }

        _body.gravityScale = config.ZeroGravity ? 0f : _defaultGravityScale;

        Vector2 velocity = _body.linearVelocity;
        if (config.FreezeX)
        {
            velocity.x = 0f;
        }
        else if (!Mathf.Approximately(config.DashSpeed, 0f))
        {
            velocity.x = GetFacingDirection() * config.DashSpeed;
        }

        _body.linearVelocity = velocity;
    }

    private Vector2 GetAttackCenter(Vector2 activeOffset)
    {
        Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
        return origin + new Vector3(activeOffset.x * GetFacingDirection(), activeOffset.y, 0f);
    }

    private float GetFacingDirection()
    {
        return _controller != null && _controller.FacingDirection < 0f ? -1f : 1f;
    }

    private SkillConfig GetSkillConfig(SkillType skill)
    {
        return skill switch
        {
            SkillType.Attack => new SkillConfig(
                "Attack",
                attackAnimationDuration,
                0.02f,
                attackActiveDuration,
                attackOffset,
                attackSize,
                0f,
                0f,
                false,
                true,
                0.02f,
                false,
                0f),
            SkillType.Attack3 => new SkillConfig(
                "Attack3",
                0.34f,
                0.05f,
                0.18f,
                new Vector2(0.78f, -0.04f),
                new Vector2(1.15f, 0.78f),
                0f,
                0f,
                false,
                true,
                0.03f,
                false,
                0f),
            SkillType.DashAttack => new SkillConfig(
                "DashAttack",
                0.3f,
                0.04f,
                0.24f,
                new Vector2(0.72f, -0.08f),
                new Vector2(1.12f, 0.68f),
                11.5f,
                0f,
                true,
                false,
                0.12f,
                false,
                0.22f),
            SkillType.SpecialDash => new SkillConfig(
                "SpecialDash",
                0.42f,
                0.04f,
                0.34f,
                new Vector2(0.9f, -0.08f),
                new Vector2(1.35f, 0.72f),
                15.5f,
                0f,
                true,
                false,
                0.16f,
                false,
                0.3f),
            SkillType.IdleUpAttack => new SkillConfig(
                "IdleUpAttack",
                0.28f,
                0.04f,
                0.16f,
                new Vector2(0f, 0.58f),
                new Vector2(0.82f, 1.12f),
                0f,
                0f,
                false,
                true,
                0.04f,
                false,
                0f),
            SkillType.JumpUpAttack => new SkillConfig(
                "JumpUpAttack",
                0.46f,
                0.02f,
                0.24f,
                new Vector2(0.18f, 0.62f),
                new Vector2(0.9f, 1.18f),
                1.5f,
                10.5f,
                true,
                false,
                0.16f,
                true,
                0f),
            SkillType.JumpDownAttack => new SkillConfig(
                "JumpDownAttack",
                0.5f,
                0.06f,
                0.34f,
                new Vector2(0f, -0.88f),
                new Vector2(0.86f, 1.24f),
                0f,
                -8.5f,
                false,
                true,
                0.12f,
                false,
                0f),
            _ => default
        };
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.35f, 0.25f, 0.9f);
        SkillConfig config = Application.isPlaying ? GetSkillConfig(_activeSkill == SkillType.None ? SkillType.Attack : _activeSkill) : GetSkillConfig(SkillType.Attack);
        Vector2 center = Application.isPlaying ? GetAttackCenter(config.HitboxOffset) : (Vector2)transform.position + config.HitboxOffset;
        Gizmos.DrawWireCube(center, config.HitboxSize);
    }
}
