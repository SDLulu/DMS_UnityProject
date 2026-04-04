using System.Collections.Generic;
using UnityEngine;

// 역할:
// - 보스 전투의 상태 기계, 타깃 추적, 패턴 선택과 실행을 관리합니다.
// - 피격/사망은 BossInteraction으로 외부와 연결하고, 씬 흐름은 BossEncounterDirector에 맡깁니다.
//
// 구조 포인트:
// - 보스 단일 프리팹 안에서 끝나는 전투 규칙의 중심 허브입니다.

public enum BossAnimationState
{
    Idle,
    Telegraph,
    Dash,
    Leap,
    Shoot,
    Hit
}

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(BossInteraction))]
public class BossController : MonoBehaviour
{
    [SerializeField] private BossConfig _config = new BossConfig();
    [SerializeField] private string fallbackTargetTag = "Player";
    [SerializeField] private float reacquireInterval = 0.35f;
    [SerializeField] private bool showHitboxVisuals = true;
    [SerializeField] private bool invertSpriteFacing = false;
    [SerializeField] private Color dashHitboxColor = new Color(1f, 0.25f, 0.25f, 0.22f);
    [SerializeField] private Color leapHitboxColor = new Color(1f, 0.65f, 0.2f, 0.2f);
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private Transform debugRoot;

    private enum BossState
    {
        Roam,
        Telegraph,
        Execute,
        Recover,
        Defeated
    }

    private enum BossPatternType
    {
        DashStrike,
        LeapSlam,
        ProjectileFan
    }

    private BossInteraction _interaction;
    private SpriteRenderer _spriteRenderer;
    private BoxCollider2D _boxCollider;
    private Transform _target;
    private float[][] _cooldowns;
    private BossState _state = BossState.Roam;
    private BossPhaseConfig _currentPhase;
    private int _currentPhaseIndex;
    private BossPatternConfig _activePattern;
    private BossPatternType _activePatternType;
    private float _stateTimer;
    private float _decisionTimer;
    private float _touchTimer;
    private float _facing = 1f;
    private bool _initialized;

    private readonly HashSet<PlayerInteraction> _damagedTargets = new HashSet<PlayerInteraction>();
    private Vector3 _leapStartPosition;
    private Vector3 _leapEndPosition;
    private bool _leapDamageApplied;
    private int _burstsRemaining;
    private float _burstTimer;
    private float _dashDirection = 1f;
    private BossAnimationState _animationState = BossAnimationState.Idle;
    private GameObject _dashHitboxVisual;
    private SpriteRenderer _dashHitboxRenderer;
    private GameObject _leapHitboxVisual;
    private SpriteRenderer _leapHitboxRenderer;
    private bool _combatActive = true;
    private float _reacquireTimer;

    public BossAnimationState AnimationState => _animationState;
    public BossConfig RuntimeConfig => _config;
    public Transform VisualRoot => visualRoot;
    public SpriteRenderer VisualRenderer => visualRenderer;
    public Transform ProjectileSpawnPoint => projectileSpawnPoint;
    public bool CombatActive => _combatActive;
    private Transform CurrentTarget => ResolveLiveTarget(_target);

    private void Awake()
    {
        CacheComponents();
        _config = BossConfigLoader.Sanitize(_config);
        EnsureHitboxVisuals();
    }

    private void OnEnable()
    {
        CacheComponents();
        if (_interaction != null)
        {
            _interaction.Respawned -= HandleRespawned;
            _interaction.Respawned += HandleRespawned;
        }
    }

    private void OnValidate()
    {
        _config = BossConfigLoader.Sanitize(_config);

        if (Application.isPlaying)
        {
            RefreshRuntimeConfig();
        }
    }

    private void CacheComponents()
    {
        if (visualRoot == null)
        {
            visualRoot = transform.Find("Visual");
        }

        if (visualRenderer == null)
        {
            _spriteRenderer = visualRoot != null
                ? visualRoot.GetComponent<SpriteRenderer>()
                : GetComponentInChildren<SpriteRenderer>();
            visualRenderer = _spriteRenderer;
        }
        else
        {
            _spriteRenderer = visualRenderer;
        }

        if (_boxCollider == null)
        {
            _boxCollider = GetComponent<BoxCollider2D>();
        }

        if (_interaction == null)
        {
            _interaction = GetComponent<BossInteraction>();
        }

        if (projectileSpawnPoint == null)
        {
            Transform sensorsRoot = transform.Find("Sensors");
            projectileSpawnPoint = sensorsRoot != null ? sensorsRoot.Find("ProjectileSpawn") : null;
        }

        if (debugRoot == null)
        {
            debugRoot = transform.Find("Debug");
        }
    }

    public void Initialize(BossConfig config, Transform target)
    {
        CacheComponents();

        _config = BossConfigLoader.DeepClone(config);
        _target = ResolveLiveTarget(target);
        _reacquireTimer = 0f;
        _initialized = true;
        RefreshRuntimeConfig(resetBossState: true, preserveHealthRatio: false);
    }

    public void Initialize(Transform target)
    {
        CacheComponents();

        _config = BossConfigLoader.DeepClone(_config);
        _target = ResolveLiveTarget(target);
        _reacquireTimer = 0f;
        _initialized = true;
        RefreshRuntimeConfig(resetBossState: true, preserveHealthRatio: false);
    }

    public void SetSerializedConfig(BossConfig config)
    {
        _config = BossConfigLoader.DeepClone(config);
    }

    public void SetCombatActive(bool active)
    {
        if (_combatActive == active)
        {
            return;
        }

        _combatActive = active;
        if (_boxCollider != null)
        {
            _boxCollider.enabled = true;
        }

        if (!_combatActive)
        {
            _state = BossState.Roam;
            _decisionTimer = 0f;
            _stateTimer = 0f;
            _touchTimer = 0f;
            _activePattern = null;
            _burstsRemaining = 0;
            _burstTimer = 0f;
            _leapDamageApplied = false;
            _damagedTargets.Clear();
            SetAnimationState(BossAnimationState.Idle);
            SetHitboxVisualActive(_dashHitboxVisual, false);
            SetHitboxVisualActive(_leapHitboxVisual, false);

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _currentPhase != null
                    ? _currentPhase.phaseColor.ToColor()
                    : _config.core.normalColor.ToColor();
            }

            return;
        }

        RefreshRuntimeConfig(resetBossState: true, preserveHealthRatio: true);
    }

    public void RefreshRuntimeConfig(bool resetBossState = false, bool preserveHealthRatio = true)
    {
        CacheComponents();
        _config = BossConfigLoader.Sanitize(_config);

        if (_config.phases.Length == 0)
        {
            _config = BossConfigLoader.CreateDefault();
        }

        gameObject.name = _config.core.bossName;
        _cooldowns = BuildCooldownCache(_config);
        _currentPhaseIndex = Mathf.Clamp(_currentPhaseIndex, 0, _config.phases.Length - 1);
        _currentPhase = _config.phases[_currentPhaseIndex];

        if (resetBossState)
        {
            _state = BossState.Roam;
            _decisionTimer = 0f;
            _touchTimer = 0f;
            _stateTimer = 0f;
            _activePattern = null;
            _burstsRemaining = 0;
            _burstTimer = 0f;
            _leapDamageApplied = false;
            SetAnimationState(BossAnimationState.Idle);
        }

        if (_interaction != null)
        {
            _interaction.ConfigureHealth(
                _config.core.maxHealth,
                _config.core.normalColor.ToColor(),
                _config.core.deadColor.ToColor(),
                preserveHealthRatio);
            _interaction.SetRespawnEnabled(true);
            _interaction.ConfigureRespawn(transform.position, 1.25f, new MonoBehaviour[] { this });
        }

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _state == BossState.Telegraph
                ? _config.core.telegraphColor.ToColor()
                : _currentPhase.phaseColor.ToColor();
            ApplyVisualFacing();
        }

        if (_boxCollider != null)
        {
            _boxCollider.size = new Vector2(_config.core.bodyColliderWidth, _config.core.bodyColliderHeight);
            _boxCollider.offset = new Vector2(_config.core.bodyColliderOffsetX, _config.core.bodyColliderOffsetY);
        }
    }

    public BossConfig CreateConfigSnapshot()
    {
        return BossConfigLoader.DeepClone(_config);
    }

    private void Update()
    {
        if (!_initialized)
        {
            return;
        }

        RefreshTarget();

        if (_interaction == null || _interaction.IsDead)
        {
            _state = BossState.Defeated;
            return;
        }

        if (!_combatActive)
        {
            SetAnimationState(BossAnimationState.Idle);
            UpdateHitboxVisuals();
            return;
        }

        float deltaTime = Time.deltaTime;
        TickCooldowns(deltaTime);
        UpdatePhase();
        FaceTarget();
        HandleTouchDamage(deltaTime);

        switch (_state)
        {
            case BossState.Roam:
                UpdateRoam(deltaTime);
                break;
            case BossState.Telegraph:
                UpdateTelegraph(deltaTime);
                break;
            case BossState.Execute:
                UpdateExecute(deltaTime);
                break;
            case BossState.Recover:
                UpdateRecover(deltaTime);
                break;
        }

        UpdateHitboxVisuals();
    }

    private void UpdateRoam(float deltaTime)
    {
        MoveWithinArena(deltaTime);

        _decisionTimer -= deltaTime;
        if (_decisionTimer > 0f)
        {
            return;
        }

        _decisionTimer = _config.core.attackDecisionInterval;
        TryStartPattern();
    }

    private void UpdateTelegraph(float deltaTime)
    {
        _stateTimer -= deltaTime;
        if (_stateTimer <= 0f)
        {
            EnterExecute();
        }
    }

    private void UpdateExecute(float deltaTime)
    {
        _stateTimer -= deltaTime;

        switch (_activePatternType)
        {
            case BossPatternType.DashStrike:
                ExecuteDash(deltaTime);
                break;
            case BossPatternType.LeapSlam:
                ExecuteLeap();
                break;
            case BossPatternType.ProjectileFan:
                ExecuteProjectileFan(deltaTime);
                break;
        }

        if (_stateTimer <= 0f)
        {
            FinishExecute();
        }
    }

    private void UpdateRecover(float deltaTime)
    {
        _stateTimer -= deltaTime;
        if (_stateTimer <= 0f)
        {
            _state = BossState.Roam;
        }
    }

    private void MoveWithinArena(float deltaTime)
    {
        Transform target = CurrentTarget;
        if (target == null)
        {
            return;
        }

        float phaseMoveSpeed = _config.core.idleMoveSpeed * _currentPhase.moveSpeedMultiplier;
        float distance = Mathf.Abs(target.position.x - transform.position.x);
        float moveDirection = 0f;

        if (distance > _config.core.preferredDistance + 0.45f)
        {
            moveDirection = Mathf.Sign(target.position.x - transform.position.x);
        }
        else if (distance < _config.core.preferredDistance - 0.75f)
        {
            moveDirection = -Mathf.Sign(target.position.x - transform.position.x);
        }

        float targetX = Mathf.Clamp(
            transform.position.x + moveDirection * phaseMoveSpeed * deltaTime,
            _config.core.arenaLeft,
            _config.core.arenaRight);

        transform.position = new Vector3(targetX, _config.core.groundY, 0f);
    }

    private void TryStartPattern()
    {
        Transform target = CurrentTarget;
        if (target == null || _currentPhase == null || _currentPhase.patterns == null || _currentPhase.patterns.Length == 0)
        {
            return;
        }

        float distance = Mathf.Abs(target.position.x - transform.position.x);
        float totalWeight = 0f;
        List<int> candidateIndices = new List<int>();
        float[] phaseCooldowns = _cooldowns != null && _currentPhaseIndex >= 0 && _currentPhaseIndex < _cooldowns.Length
            ? _cooldowns[_currentPhaseIndex]
            : null;

        for (int i = 0; i < _currentPhase.patterns.Length; i++)
        {
            BossPatternConfig pattern = _currentPhase.patterns[i];
            if (pattern == null)
            {
                continue;
            }

            if (!pattern.enabled || (phaseCooldowns != null && i < phaseCooldowns.Length && phaseCooldowns[i] > 0f))
            {
                continue;
            }

            if (distance < pattern.minDistance || distance > pattern.maxDistance)
            {
                continue;
            }

            candidateIndices.Add(i);
            totalWeight += Mathf.Max(0.01f, pattern.selectionWeight);
        }

        if (candidateIndices.Count == 0)
        {
            return;
        }

        float roll = Random.value * totalWeight;
        for (int i = 0; i < candidateIndices.Count; i++)
        {
            int candidateIndex = candidateIndices[i];
            BossPatternConfig pattern = _currentPhase.patterns[candidateIndex];
            roll -= Mathf.Max(0.01f, pattern.selectionWeight);
            if (roll <= 0f)
            {
                BeginPattern(pattern);
                if (phaseCooldowns != null && candidateIndex < phaseCooldowns.Length)
                {
                    phaseCooldowns[candidateIndex] = pattern.cooldown * _currentPhase.cooldownMultiplier;
                }
                return;
            }
        }
    }

    private void BeginPattern(BossPatternConfig pattern)
    {
        _activePattern = pattern;
        _activePatternType = ParsePatternType(pattern.type);
        _state = BossState.Telegraph;
        _stateTimer = Mathf.Max(0.05f, pattern.telegraphDuration);
        _spriteRenderer.color = _config.core.telegraphColor.ToColor();
        SetAnimationState(BossAnimationState.Telegraph);
        _damagedTargets.Clear();
        _leapDamageApplied = false;
        _burstsRemaining = 0;
        _burstTimer = 0f;

        if (_activePatternType == BossPatternType.LeapSlam)
        {
            PrepareLeapLanding();
        }

        Debug.Log($"{_config.core.bossName} pattern: {pattern.name}", this);
    }

    private void EnterExecute()
    {
        _state = BossState.Execute;
        _stateTimer = Mathf.Max(0.05f, _activePattern.executeDuration);
        _spriteRenderer.color = _currentPhase.phaseColor.ToColor();

        if (_activePatternType == BossPatternType.LeapSlam)
        {
            SetAnimationState(BossAnimationState.Leap);
            _leapStartPosition = transform.position;
        }
        else if (_activePatternType == BossPatternType.ProjectileFan)
        {
            SetAnimationState(BossAnimationState.Shoot);
            _burstsRemaining = Mathf.Max(1, _activePattern.volleyBursts);
            _burstTimer = 0f;
            FireProjectileVolley();
        }
        else if (_activePatternType == BossPatternType.DashStrike)
        {
            SetAnimationState(BossAnimationState.Dash);
            Transform target = CurrentTarget;
            _dashDirection = target != null ? Mathf.Sign(target.position.x - transform.position.x) : _facing;
            if (Mathf.Approximately(_dashDirection, 0f))
            {
                _dashDirection = _facing;
            }
        }
    }

    private void ExecuteDash(float deltaTime)
    {
        float nextX = Mathf.Clamp(
            transform.position.x + _dashDirection * _activePattern.dashSpeed * deltaTime,
            _config.core.arenaLeft,
            _config.core.arenaRight);
        transform.position = new Vector3(nextX, _config.core.groundY, 0f);
        _facing = _dashDirection;
        ApplyDashDamage(_dashDirection);
    }

    private void ExecuteLeap()
    {
        float duration = Mathf.Max(0.05f, _activePattern.executeDuration);
        float progress = 1f - Mathf.Clamp01(_stateTimer / duration);
        float x = Mathf.Lerp(_leapStartPosition.x, _leapEndPosition.x, progress);
        float y = _config.core.groundY + Mathf.Sin(progress * Mathf.PI) * _activePattern.leapHeight;
        transform.position = new Vector3(x, y, 0f);
    }

    private void ExecuteProjectileFan(float deltaTime)
    {
        if (_burstsRemaining <= 0)
        {
            return;
        }

        _burstTimer -= deltaTime;
        if (_burstTimer <= 0f)
        {
            FireProjectileVolley();
        }
    }

    private void FinishExecute()
    {
        if (_activePatternType == BossPatternType.LeapSlam && !_leapDamageApplied)
        {
            transform.position = _leapEndPosition;
            ApplyLeapDamage();
        }

        _state = BossState.Recover;
        _stateTimer = Mathf.Max(0.05f, _activePattern.recoveryDuration);
        _spriteRenderer.color = _currentPhase.phaseColor.ToColor();
        SetAnimationState(BossAnimationState.Idle);
    }

    private void ApplyDashDamage(float dashDirection)
    {
        Vector2 center = (Vector2)transform.position + new Vector2(dashDirection * (_activePattern.dashHitWidth * 0.4f), 0f);
        Vector2 size = new Vector2(_activePattern.dashHitWidth, _activePattern.dashHitHeight);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerInteraction target = hits[i].GetComponentInParent<PlayerInteraction>();
            if (target == null || _damagedTargets.Contains(target))
            {
                continue;
            }

            target.ReceiveHit(
                _activePattern.damage,
                new Vector2(dashDirection * _activePattern.knockback, _activePattern.knockback * 0.35f),
                gameObject);
            _damagedTargets.Add(target);
        }
    }

    private void ApplyLeapDamage()
    {
        _leapDamageApplied = true;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _activePattern.landingRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerInteraction target = hits[i].GetComponentInParent<PlayerInteraction>();
            if (target == null || _damagedTargets.Contains(target))
            {
                continue;
            }

            Vector2 direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
            if (direction.sqrMagnitude <= 0.01f)
            {
                direction = Vector2.up;
            }

            target.ReceiveHit(
                _activePattern.damage,
                direction * _activePattern.knockback + Vector2.up * (_activePattern.knockback * 0.55f),
                gameObject);
            _damagedTargets.Add(target);
        }
    }

    private void FireProjectileVolley()
    {
        _burstsRemaining--;
        _burstTimer = _activePattern.volleySpacing;

        Vector3 spawnPosition;
        if (projectileSpawnPoint != null)
        {
            Vector3 sensorOffset = projectileSpawnPoint.position - transform.position;
            spawnPosition = transform.position + new Vector3(Mathf.Abs(sensorOffset.x) * _facing, sensorOffset.y, 0f);
        }
        else
        {
            spawnPosition = transform.position + new Vector3(_activePattern.projectileSpawnX * _facing, _activePattern.projectileSpawnY, 0f);
        }
        Transform target = CurrentTarget;
        Vector2 centerDirection = target != null
            ? ((Vector2)target.position - (Vector2)spawnPosition).normalized
            : new Vector2(_facing, 0f);

        if (centerDirection.sqrMagnitude <= 0.001f)
        {
            centerDirection = new Vector2(_facing, 0f);
        }

        float centerAngle = Mathf.Atan2(centerDirection.y, centerDirection.x) * Mathf.Rad2Deg;
        float spread = _activePattern.projectileSpreadAngle;
        int count = Mathf.Max(1, _activePattern.projectileCount);
        float step = count == 1 ? 0f : spread / (count - 1);
        float startAngle = centerAngle - spread * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + step * i;
            Vector2 direction = DegreeToVector(angle);

            GameObject projectileObject = new GameObject($"{_activePattern.name}_Projectile");
            projectileObject.transform.position = spawnPosition;
            projectileObject.transform.localScale = Vector3.one * Mathf.Max(0.2f, _activePattern.projectileRadius * 3.5f);

            SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteUtility.WhiteSprite;

            CircleCollider2D circleCollider = projectileObject.AddComponent<CircleCollider2D>();
            circleCollider.isTrigger = true;

            BossProjectile projectile = projectileObject.AddComponent<BossProjectile>();
            projectile.Configure(
                direction,
                _activePattern.projectileSpeed,
                _activePattern.projectileLifetime,
                _activePattern.damage,
                _activePattern.knockback,
                _activePattern.projectileRadius,
                _currentPhase.phaseColor.ToColor(),
                gameObject);
        }
    }

    private void HandleTouchDamage(float deltaTime)
    {
        if (CurrentTarget == null || _config.core.contactDamage <= 0f)
        {
            return;
        }

        _touchTimer -= deltaTime;
        if (_touchTimer > 0f)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapBoxAll((Vector2)_boxCollider.bounds.center, _boxCollider.bounds.size, 0f);
        for (int i = 0; i < hits.Length; i++)
        {
            PlayerInteraction target = hits[i].GetComponentInParent<PlayerInteraction>();
            if (target == null)
            {
                continue;
            }

            Vector2 direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
            if (direction.sqrMagnitude <= 0.01f)
            {
                direction = new Vector2(_facing, 0.2f).normalized;
            }

            target.ReceiveHit(
                _config.core.contactDamage,
                direction * _config.core.contactKnockback,
                gameObject);
            _touchTimer = _config.core.contactInterval;
            return;
        }
    }

    private void UpdatePhase()
    {
        if (_interaction == null)
        {
            return;
        }

        int newPhaseIndex = 0;
        float healthRatio = _interaction.HealthNormalized;

        for (int i = 0; i < _config.phases.Length; i++)
        {
            if (healthRatio <= _config.phases[i].healthThreshold)
            {
                newPhaseIndex = i;
            }
        }

        if (newPhaseIndex == _currentPhaseIndex)
        {
            return;
        }

        _currentPhaseIndex = newPhaseIndex;
        _currentPhase = _config.phases[_currentPhaseIndex];
        _spriteRenderer.color = _currentPhase.phaseColor.ToColor();
        Debug.Log($"{_config.core.bossName} phase -> {_currentPhase.name}", this);
    }

    private void TickCooldowns(float deltaTime)
    {
        if (_cooldowns == null)
        {
            return;
        }

        for (int phaseIndex = 0; phaseIndex < _cooldowns.Length; phaseIndex++)
        {
            for (int patternIndex = 0; patternIndex < _cooldowns[phaseIndex].Length; patternIndex++)
            {
                if (_cooldowns[phaseIndex][patternIndex] > 0f)
                {
                    _cooldowns[phaseIndex][patternIndex] -= deltaTime;
                }
            }
        }
    }

    private void FaceTarget()
    {
        Transform target = CurrentTarget;
        if (target == null)
        {
            return;
        }

        if (_state == BossState.Execute && _activePatternType == BossPatternType.DashStrike)
        {
            return;
        }

        float direction = Mathf.Sign(target.position.x - transform.position.x);
        if (!Mathf.Approximately(direction, 0f))
        {
            _facing = direction;
            ApplyVisualFacing();
        }
    }

    private void ApplyVisualFacing()
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        bool faceRight = _facing >= 0f;
        _spriteRenderer.flipX = invertSpriteFacing ? faceRight : !faceRight;
    }

    private static float[][] BuildCooldownCache(BossConfig config)
    {
        float[][] result = new float[config.phases.Length][];
        for (int i = 0; i < config.phases.Length; i++)
        {
            int patternCount = config.phases[i].patterns == null ? 0 : config.phases[i].patterns.Length;
            result[i] = new float[patternCount];
        }

        return result;
    }

    private static BossPatternType ParsePatternType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return BossPatternType.DashStrike;
        }

        if (System.Enum.TryParse(typeName, true, out BossPatternType parsed))
        {
            return parsed;
        }

        return BossPatternType.DashStrike;
    }

    private static Vector2 DegreeToVector(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private void SetAnimationState(BossAnimationState newState)
    {
        _animationState = newState;
    }

    private void PrepareLeapLanding()
    {
        _leapStartPosition = transform.position;
        Transform target = CurrentTarget;
        float targetX = target != null ? target.position.x : transform.position.x;
        targetX = Mathf.Clamp(targetX + _activePattern.landingOffset * _facing, _config.core.arenaLeft, _config.core.arenaRight);
        _leapEndPosition = new Vector3(targetX, _config.core.groundY, 0f);
    }

    private void EnsureHitboxVisuals()
    {
        if (_dashHitboxVisual == null)
        {
            _dashHitboxVisual = CreateHitboxVisual("BossDashHitboxVisual", RuntimeSpriteUtility.WhiteSprite, 14);
            _dashHitboxRenderer = _dashHitboxVisual.GetComponent<SpriteRenderer>();
        }

        if (_leapHitboxVisual == null)
        {
            _leapHitboxVisual = CreateHitboxVisual("BossLeapHitboxVisual", RuntimeSpriteUtility.CircleSprite, 13);
            _leapHitboxRenderer = _leapHitboxVisual.GetComponent<SpriteRenderer>();
        }
    }

    private GameObject CreateHitboxVisual(string objectName, Sprite sprite, int sortingOrder)
    {
        GameObject visual = new GameObject(objectName);
        if (debugRoot != null)
        {
            visual.transform.SetParent(debugRoot, false);
        }

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        visual.SetActive(false);
        return visual;
    }

    private void UpdateHitboxVisuals()
    {
        EnsureHitboxVisuals();

        if (!showHitboxVisuals || _activePattern == null)
        {
            SetHitboxVisualActive(_dashHitboxVisual, false);
            SetHitboxVisualActive(_leapHitboxVisual, false);
            return;
        }

        bool dashVisible = _activePatternType == BossPatternType.DashStrike
            && (_state == BossState.Telegraph || _state == BossState.Execute);
        if (dashVisible)
        {
            float direction = _state == BossState.Execute ? _dashDirection : _facing;
            if (Mathf.Approximately(direction, 0f))
            {
                direction = 1f;
            }

            Vector3 position = transform.position + new Vector3(direction * (_activePattern.dashHitWidth * 0.4f), 0f, 0f);
            _dashHitboxVisual.transform.position = position;
            _dashHitboxVisual.transform.localScale = new Vector3(_activePattern.dashHitWidth, _activePattern.dashHitHeight, 1f);
            _dashHitboxRenderer.color = dashHitboxColor;
            SetHitboxVisualActive(_dashHitboxVisual, true);
        }
        else
        {
            SetHitboxVisualActive(_dashHitboxVisual, false);
        }

        bool leapVisible = _activePatternType == BossPatternType.LeapSlam
            && (_state == BossState.Telegraph || _state == BossState.Execute);
        if (leapVisible)
        {
            float diameter = _activePattern.landingRadius * 2f;
            _leapHitboxVisual.transform.position = _leapEndPosition;
            _leapHitboxVisual.transform.localScale = new Vector3(diameter, diameter, 1f);
            _leapHitboxRenderer.color = leapHitboxColor;
            SetHitboxVisualActive(_leapHitboxVisual, true);
        }
        else
        {
            SetHitboxVisualActive(_leapHitboxVisual, false);
        }
    }

    private static void SetHitboxVisualActive(GameObject visual, bool active)
    {
        if (visual != null && visual.activeSelf != active)
        {
            visual.SetActive(active);
        }
    }

    private void OnDisable()
    {
        if (_interaction != null)
        {
            _interaction.Respawned -= HandleRespawned;
        }

        SetHitboxVisualActive(_dashHitboxVisual, false);
        SetHitboxVisualActive(_leapHitboxVisual, false);
    }

    private void OnDestroy()
    {
        if (_interaction != null)
        {
            _interaction.Respawned -= HandleRespawned;
        }

        if (_dashHitboxVisual != null)
        {
            Destroy(_dashHitboxVisual);
        }

        if (_leapHitboxVisual != null)
        {
            Destroy(_leapHitboxVisual);
        }
    }

    private void HandleRespawned()
    {
        RefreshRuntimeConfig(resetBossState: true, preserveHealthRatio: false);
        transform.position = new Vector3(transform.position.x, _config.core.groundY, 0f);
    }

    private void RefreshTarget()
    {
        if (ResolveLiveTarget(_target) != null)
        {
            _reacquireTimer = 0f;
            return;
        }

        _reacquireTimer -= Time.deltaTime;
        if (_reacquireTimer > 0f)
        {
            return;
        }

        _reacquireTimer = Mathf.Max(0.05f, reacquireInterval);
        if (string.IsNullOrWhiteSpace(fallbackTargetTag))
        {
            _target = null;
            return;
        }

        GameObject fallbackObject = GameObject.FindGameObjectWithTag(fallbackTargetTag);
        _target = fallbackObject != null ? ResolveLiveTarget(fallbackObject.transform) : null;
    }

    private static Transform ResolveLiveTarget(Transform candidate)
    {
        if (candidate == null)
        {
            return null;
        }

        PlayerInteraction player = candidate.GetComponentInParent<PlayerInteraction>();
        if (player != null && !player.IsAlive)
        {
            return null;
        }

        return candidate;
    }

    private void OnDrawGizmosSelected()
    {
        if (_config == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            new Vector3(_config.core.arenaLeft, _config.core.groundY - 1.5f, 0f),
            new Vector3(_config.core.arenaLeft, _config.core.groundY + 1.5f, 0f));
        Gizmos.DrawLine(
            new Vector3(_config.core.arenaRight, _config.core.groundY - 1.5f, 0f),
            new Vector3(_config.core.arenaRight, _config.core.groundY + 1.5f, 0f));

        if (_activePattern != null && _activePatternType == BossPatternType.LeapSlam)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_leapEndPosition, _activePattern.landingRadius);
        }
    }
}
