using System;
using System.Collections;
using UnityEngine;

// 역할:
// - 플레이어 외부 상호작용 창구를 제공합니다.
// - HUD, 보스전 연출, 대화 시스템, 적 공격이 플레이어 내부 구현 대신 이 컴포넌트와만 통신하도록 만듭니다.
//
// 구조 포인트:
// - 플레이어가 자기 체력/피격/부활 규칙을 직접 소유하고, 씬/시스템에는 이 표면만 공개합니다.

[DisallowMultipleComponent]
public class PlayerInteraction : MonoBehaviour, IDamageReceiver
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 1f;
    [SerializeField] private float invulnerabilityDuration = 0.08f;

    [Header("Feedback")]
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private Color damageFlashColor = Color.white;
    [SerializeField] private Color deadTint = new Color(0.2f, 0.2f, 0.25f, 1f);

    [Header("Respawn")]
    [SerializeField] private bool respawnOnDeath = true;
    [SerializeField] private float respawnDelay = 0.75f;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private MonoBehaviour controller;
    [SerializeField] private SimplePlayerCombat combat;
    [SerializeField] private Rigidbody2D body;

    public event Action Damaged;
    public event Action Died;
    public event Action HealthChanged;
    public event Action Respawned;

    private float _currentHealth;
    private float _invulnerabilityTimer;
    private Vector3 _spawnPosition;
    private Color _baseColor = Color.white;
    private MonoBehaviour[] _behavioursToDisable = Array.Empty<MonoBehaviour>();
    private Coroutine _flashRoutine;
    private SimplePlayerController _simpleController;
    private bool _isDead;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthNormalized => maxHealth <= 0f ? 0f : Mathf.Clamp01(_currentHealth / maxHealth);
    public bool IsAlive => !_isDead;
    public bool IsDead => _isDead;
    public float RespawnDelay => respawnDelay;
    public bool HasGameplayControl =>
        (controller == null || controller.enabled) &&
        (combat == null || combat.enabled);

    private void Awake()
    {
        CacheReferences();
        _spawnPosition = transform.position;
        _currentHealth = Mathf.Max(1f, maxHealth);
        _baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
    }

    private void OnEnable()
    {
        CacheReferences();
    }

    private void Update()
    {
        if (_invulnerabilityTimer > 0f)
        {
            _invulnerabilityTimer -= Time.deltaTime;
        }
    }

    public PlayerHealthConfig CreateHealthConfigSnapshot()
    {
        return new PlayerHealthConfig
        {
            maxHealth = maxHealth,
            invulnerabilityDuration = invulnerabilityDuration,
            flashDuration = flashDuration,
            respawnDelay = respawnDelay,
            normalColor = new SerializableColor(_baseColor.r, _baseColor.g, _baseColor.b, _baseColor.a),
            damageFlashColor = new SerializableColor(damageFlashColor.r, damageFlashColor.g, damageFlashColor.b, damageFlashColor.a),
            deadTint = new SerializableColor(deadTint.r, deadTint.g, deadTint.b, deadTint.a)
        };
    }

    public void ApplyHealthConfig(PlayerHealthConfig config, bool preserveHealthRatio = true)
    {
        config = PlayerConfigLoader.Sanitize(new PlayerConfig { health = config }).health;

        float currentRatio = HealthNormalized;
        maxHealth = Mathf.Max(1f, config.maxHealth);
        invulnerabilityDuration = Mathf.Max(0f, config.invulnerabilityDuration);
        flashDuration = Mathf.Max(0f, config.flashDuration);
        respawnDelay = Mathf.Max(0.05f, config.respawnDelay);
        damageFlashColor = config.damageFlashColor.ToColor();
        deadTint = config.deadTint.ToColor();
        _baseColor = config.normalColor.ToColor();
        _currentHealth = preserveHealthRatio ? maxHealth * currentRatio : maxHealth;
        _isDead = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = _baseColor;
        }

        HealthChanged?.Invoke();
    }

    public bool ReceiveHit(float damage, Vector2 knockback, GameObject source)
    {
        if (_isDead || damage <= 0f || _invulnerabilityTimer > 0f || IsRollingInvulnerable())
        {
            return false;
        }

        if (source != null && source.transform.IsChildOf(transform))
        {
            return false;
        }

        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        _invulnerabilityTimer = invulnerabilityDuration;

        if (body != null)
        {
            body.linearVelocity = knockback;
        }

        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
        }

        _flashRoutine = StartCoroutine(FlashRoutine());
        Damaged?.Invoke();
        HealthChanged?.Invoke();

        if (_currentHealth <= 0f)
        {
            HandleDeath();
        }

        return true;
    }

    private bool IsRollingInvulnerable()
    {
        return _simpleController != null && _simpleController.IsRolling;
    }

    public void OnDie()
    {
        if (_isDead)
        {
            return;
        }

        _currentHealth = 0f;
        _invulnerabilityTimer = 0f;
        HealthChanged?.Invoke();
        HandleDeath();
    }

    public void ConfigureRespawn(Vector3 spawnPosition, float newRespawnDelay, MonoBehaviour[] behavioursToDisable = null)
    {
        _spawnPosition = spawnPosition;
        respawnDelay = Mathf.Max(0.05f, newRespawnDelay);
        _behavioursToDisable = behavioursToDisable ?? Array.Empty<MonoBehaviour>();
    }

    public void SetSpawnPosition(Vector3 position)
    {
        _spawnPosition = position;
    }

    public void SetRespawnEnabled(bool enabled)
    {
        respawnOnDeath = enabled;
    }

    public void RestoreFullHealth(bool notifyListeners = true, bool reactivateBehaviours = true)
    {
        _isDead = false;
        _invulnerabilityTimer = 0f;
        _currentHealth = maxHealth;

        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
            _flashRoutine = null;
        }

        if (body != null)
        {
            body.simulated = true;
            body.linearVelocity = Vector2.zero;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = _baseColor;
        }

        if (reactivateBehaviours)
        {
            SetManagedBehavioursEnabled(true);
        }

        if (notifyListeners)
        {
            HealthChanged?.Invoke();
        }
    }

    public void SetGameplayControlEnabled(bool enabled, bool clearVelocity = true)
    {
        CacheReferences();

        if (controller != null)
        {
            controller.enabled = enabled;
        }

        if (combat != null)
        {
            combat.enabled = enabled;
        }

        if (clearVelocity && body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    public void MoveToPosition(Vector3 position, bool clearVelocity = true)
    {
        CacheReferences();
        transform.position = position;

        if (clearVelocity && body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    public void RestoreAtPosition(Vector3 position, bool notifyListeners = true, bool reactivateBehaviours = true)
    {
        MoveToPosition(position);
        SetSpawnPosition(position);
        RestoreFullHealth(notifyListeners, reactivateBehaviours);
    }

    private void CacheReferences()
    {
        controller ??= GetComponent<SimplePlayerController>();
        controller ??= GetComponent<P_PlayerController>();
        _simpleController = controller as SimplePlayerController;
        combat ??= GetComponent<SimplePlayerCombat>();
        body ??= GetComponent<Rigidbody2D>();
        spriteRenderer ??= GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void HandleDeath()
    {
        _isDead = true;
        Died?.Invoke();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = deadTint;
        }

        SetManagedBehavioursEnabled(false);

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        if (respawnOnDeath)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null)
        {
            yield break;
        }

        spriteRenderer.color = damageFlashColor;
        yield return new WaitForSeconds(flashDuration);

        if (!_isDead)
        {
            spriteRenderer.color = _baseColor;
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        _isDead = false;
        _currentHealth = maxHealth;
        transform.position = _spawnPosition;

        if (body != null)
        {
            body.simulated = true;
            body.linearVelocity = Vector2.zero;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = _baseColor;
        }

        SetManagedBehavioursEnabled(true);
        HealthChanged?.Invoke();
        Respawned?.Invoke();
    }

    private void SetManagedBehavioursEnabled(bool enabled)
    {
        if (_behavioursToDisable == null || _behavioursToDisable.Length == 0)
        {
            if (controller != null)
            {
                controller.enabled = enabled;
            }

            if (combat != null)
            {
                combat.enabled = enabled;
            }

            return;
        }

        for (int i = 0; i < _behavioursToDisable.Length; i++)
        {
            MonoBehaviour behaviour = _behavioursToDisable[i];
            if (behaviour != null)
            {
                behaviour.enabled = enabled;
            }
        }
    }
}
