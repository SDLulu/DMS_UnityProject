using System;
using System.Collections;
using UnityEngine;

// 역할:
// - 일반 적 외부 상호작용 창구를 제공합니다.
// - 플레이어 공격이 적 내부 이동/공격 로직 대신 이 컴포넌트와만 통신하도록 만듭니다.
//
// 구조 포인트:
// - 적이 자기 체력/피격/사망 규칙을 직접 소유하고, 외부는 표면 계약만 사용합니다.

[DisallowMultipleComponent]
public class EnemyInteraction : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private float invulnerabilityDuration = 0.08f;

    [Header("Feedback")]
    [SerializeField] private float flashDuration = 0.08f;
    [SerializeField] private Color damageFlashColor = Color.white;
    [SerializeField] private Color deadTint = new Color(0.24f, 0.24f, 0.28f, 1f);

    [Header("Death")]
    [SerializeField] private bool destroyOnDeath;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D body;

    public event Action Damaged;
    public event Action Died;
    public event Action HealthChanged;

    private float _currentHealth;
    private float _invulnerabilityTimer;
    private Color _baseColor = Color.white;
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

    private void CacheReferences()
    {
        spriteRenderer ??= GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        body ??= GetComponent<Rigidbody2D>();
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

    private void HandleDeath()
    {
        _isDead = true;
        Died?.Invoke();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = deadTint;
        }

        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour != null && behaviour != this)
            {
                behaviour.enabled = false;
            }
        }

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}
