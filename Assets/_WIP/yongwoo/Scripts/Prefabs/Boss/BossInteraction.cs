using System;
using System.Collections;
using UnityEngine;

// 역할:
// - 보스 외부 상호작용 창구를 제공합니다.
// - 플레이어 공격, HUD, 조우 디렉터가 보스 내부 패턴 규칙 대신 이 컴포넌트와만 통신하도록 만듭니다.
//
// 구조 포인트:
// - 보스가 자기 체력/피격/부활 규칙을 직접 소유하고, 외부에는 얇은 계약만 공개합니다.

[DisallowMultipleComponent]
public class BossInteraction : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 12f;
    [SerializeField] private float invulnerabilityDuration = 0.08f;

    [Header("Feedback")]
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private Color damageFlashColor = Color.white;
    [SerializeField] private Color deadTint = new Color(0.22f, 0.22f, 0.28f, 1f);

    [Header("Respawn")]
    [SerializeField] private bool respawnOnDeath = true;
    [SerializeField] private float respawnDelay = 1.25f;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
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
    private bool _isDead;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthNormalized => maxHealth <= 0f ? 0f : Mathf.Clamp01(_currentHealth / maxHealth);
    public bool IsAlive => !_isDead;
    public bool IsDead => _isDead;

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

    public void ReceiveHit(float damage, Vector2 knockback, GameObject source)
    {
        if (_isDead || damage <= 0f || _invulnerabilityTimer > 0f)
        {
            return;
        }

        if (source != null && source.transform.IsChildOf(transform))
        {
            return;
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
    }

    public void ConfigureHealth(float newMaxHealth, Color baseColor, Color newDeadTint, bool preserveHealthRatio = false)
    {
        float currentRatio = HealthNormalized;
        maxHealth = Mathf.Max(1f, newMaxHealth);
        _currentHealth = preserveHealthRatio ? maxHealth * currentRatio : maxHealth;
        _isDead = false;
        deadTint = newDeadTint;
        _baseColor = baseColor;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = _baseColor;
        }

        HealthChanged?.Invoke();
    }

    public void ConfigureRespawn(Vector3 spawnPosition, float newRespawnDelay, MonoBehaviour[] behavioursToDisable = null)
    {
        _spawnPosition = spawnPosition;
        respawnDelay = Mathf.Max(0.05f, newRespawnDelay);
        _behavioursToDisable = behavioursToDisable ?? Array.Empty<MonoBehaviour>();
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

    private void CacheReferences()
    {
        spriteRenderer ??= GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        body ??= GetComponent<Rigidbody2D>();
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
