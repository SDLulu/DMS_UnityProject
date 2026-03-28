using System;
using System.Collections;
using UnityEngine;

public enum PrototypeFaction
{
    Neutral,
    Player,
    Enemy
}

[DisallowMultipleComponent]
public class PrototypeHealth : MonoBehaviour
{
    public event Action Damaged;
    public event Action Died;
    public event Action HealthChanged;
    public event Action Respawned;

    [Header("Health")]
    [SerializeField] private PrototypeFaction faction = PrototypeFaction.Neutral;
    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private float invulnerabilityDuration = 0.08f;

    [Header("Feedback")]
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private Color damageFlashColor = Color.white;
    [SerializeField] private Color deadTint = new Color(0.22f, 0.22f, 0.28f, 1f);

    [Header("Death")]
    [SerializeField] private bool destroyOnDeath;
    [SerializeField] private bool respawnOnDeath = true;
    [SerializeField] private float respawnDelay = 0.8f;

    private float _currentHealth;
    private float _invulnerabilityTimer;
    private Vector3 _spawnPosition;
    private Color _baseColor = Color.white;
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _body;
    private MonoBehaviour[] _behavioursToDisable = new MonoBehaviour[0];
    private Coroutine _flashRoutine;
    private bool _isDead;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthNormalized => maxHealth <= 0f ? 0f : Mathf.Clamp01(_currentHealth / maxHealth);
    public bool IsDead => _isDead;
    public PrototypeFaction Faction => faction;

    public PlayerHealthConfig CreatePlayerConfigSnapshot()
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

    public void ApplyPlayerConfig(PlayerHealthConfig config, bool preserveHealthRatio = true)
    {
        config = PrototypePlayerConfigLoader.Sanitize(new PrototypePlayerConfig { health = config }).health;
        invulnerabilityDuration = config.invulnerabilityDuration;
        flashDuration = config.flashDuration;
        damageFlashColor = config.damageFlashColor.ToColor();
        Configure(
            PrototypeFaction.Player,
            config.maxHealth,
            config.normalColor.ToColor(),
            config.deadTint.ToColor(),
            preserveHealthRatio);
    }

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        _body = GetComponent<Rigidbody2D>();
        _spawnPosition = transform.position;
        _currentHealth = maxHealth;

        if (_spriteRenderer != null)
        {
            _baseColor = _spriteRenderer.color;
        }
    }

    private void Update()
    {
        if (_invulnerabilityTimer > 0f)
        {
            _invulnerabilityTimer -= Time.deltaTime;
        }
    }

    public void Configure(PrototypeFaction newFaction, float newMaxHealth, Color baseColor, Color newDeadTint, bool preserveHealthRatio = false)
    {
        float currentRatio = HealthNormalized;
        faction = newFaction;
        maxHealth = Mathf.Max(1f, newMaxHealth);
        _currentHealth = preserveHealthRatio ? maxHealth * currentRatio : maxHealth;
        _isDead = false;
        deadTint = newDeadTint;
        _baseColor = baseColor;

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _baseColor;
        }

        HealthChanged?.Invoke();
    }

    public void ConfigureRespawn(Vector3 spawnPosition, float newRespawnDelay, MonoBehaviour[] behavioursToDisable)
    {
        _spawnPosition = spawnPosition;
        respawnDelay = Mathf.Max(0.05f, newRespawnDelay);
        _behavioursToDisable = behavioursToDisable ?? new MonoBehaviour[0];
    }

    public void SetRespawnEnabled(bool enabled)
    {
        respawnOnDeath = enabled;
    }

    public bool TryApplyDamage(float damage, Vector2 knockback, GameObject source)
    {
        if (_isDead || damage <= 0f || _invulnerabilityTimer > 0f)
        {
            return false;
        }

        if (source != null)
        {
            PrototypeHealth sourceHealth = source.GetComponentInParent<PrototypeHealth>();
            if (sourceHealth != null && sourceHealth != this && sourceHealth.Faction == faction && faction != PrototypeFaction.Neutral)
            {
                return false;
            }
        }

        _currentHealth = Mathf.Max(0f, _currentHealth - damage);
        _invulnerabilityTimer = invulnerabilityDuration;

        if (_body != null)
        {
            _body.linearVelocity = knockback;
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

    private void HandleDeath()
    {
        _isDead = true;
        Died?.Invoke();

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = deadTint;
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject);
            return;
        }

        SetBehavioursEnabled(false);

        if (_body != null)
        {
            _body.linearVelocity = Vector2.zero;
            _body.simulated = false;
        }

        if (respawnOnDeath)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        if (_spriteRenderer == null)
        {
            yield break;
        }

        _spriteRenderer.color = damageFlashColor;
        yield return new WaitForSeconds(flashDuration);

        if (!_isDead)
        {
            _spriteRenderer.color = _baseColor;
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        _isDead = false;
        _currentHealth = maxHealth;
        transform.position = _spawnPosition;

        if (_body != null)
        {
            _body.simulated = true;
            _body.linearVelocity = Vector2.zero;
        }

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _baseColor;
        }

        SetBehavioursEnabled(true);
        HealthChanged?.Invoke();
        Respawned?.Invoke();
    }

    private void SetBehavioursEnabled(bool enabled)
    {
        for (int i = 0; i < _behavioursToDisable.Length; i++)
        {
            if (_behavioursToDisable[i] != null)
            {
                _behavioursToDisable[i].enabled = enabled;
            }
        }
    }
}
