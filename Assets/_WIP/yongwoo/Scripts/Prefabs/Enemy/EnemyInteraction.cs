using System;
using System.Collections;
using UnityEngine;

// 역할:
// - Blind Huntress 적의 체력, 피격, 사망 창구를 맡습니다.
// - 플레이어 공격은 이 컴포넌트만 보고 피해를 넣도록 유지합니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyInteraction : MonoBehaviour, IDamageReceiver
{
    [Header("Health")]
    [Tooltip("적의 최대 체력입니다.")]
    [SerializeField] private float maxHealth = 4f;
    [Tooltip("피격 후 무적 시간입니다. 같은 공격에 연속으로 맞는 걸 막습니다.")]
    [SerializeField] private float invulnerabilityDuration = 0.08f;

    [Header("Feedback")]
    [Tooltip("피격 플래시가 유지되는 시간입니다.")]
    [SerializeField] private float flashDuration = 0.08f;
    [Tooltip("맞았을 때 잠깐 바뀌는 색입니다.")]
    [SerializeField] private Color damageFlashColor = Color.white;
    [Tooltip("죽었을 때 적용할 색입니다.")]
    [SerializeField] private Color deadTint = new Color(0.2f, 0.2f, 0.25f, 1f);

    [Header("Respawn")]
    [Tooltip("죽은 뒤 자동으로 다시 살릴지 여부입니다. 테스트용으로만 켜는 편이 좋습니다.")]
    [SerializeField] private bool respawnOnDeath = false;
    [Tooltip("자동 부활이 켜져 있을 때 다시 살아날 때까지 기다리는 시간입니다.")]
    [SerializeField] private float respawnDelay = 1.2f;

    [Header("References")]
    [Tooltip("피격 색 변화를 적용할 스프라이트 렌더러입니다. 비워두면 자식에서 자동으로 찾습니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("피격 넉백과 사망 시 정지를 적용할 Rigidbody2D입니다.")]
    [SerializeField] private Rigidbody2D body;
    [Tooltip("죽을 때 꺼둘 Brain입니다. 비워두면 자동으로 찾습니다.")]
    [SerializeField] private BlindHuntressEnemyBrain brain;
    [Tooltip("죽을 때 꺼둘 Combat입니다. 비워두면 자동으로 찾습니다.")]
    [SerializeField] private BlindHuntressEnemyCombat combat;
    [Tooltip("죽을 때 꺼둘 AnimationDriver입니다. 비워두면 자동으로 찾습니다.")]
    [SerializeField] private BlindHuntressEnemyAnimationDriver animationDriver;

    public event Action Damaged;
    public event Action Died;
    public event Action Respawned;

    private float _currentHealth;
    private float _invulnerabilityTimer;
    private Vector3 _spawnPosition;
    private Color _baseColor = Color.white;
    private Coroutine _flashRoutine;
    private bool _isDead;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => _isDead;
    public bool IsAlive => !_isDead;

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

    public bool ReceiveHit(float damage, Vector2 knockback, GameObject source)
    {
        if (_isDead || damage <= 0f || _invulnerabilityTimer > 0f)
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

        if (_currentHealth <= 0f)
        {
            HandleDeath();
        }

        return true;
    }

    public void RestoreFullHealth()
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

        SetManagedBehavioursEnabled(true);
        Respawned?.Invoke();
    }

    private void CacheReferences()
    {
        body ??= GetComponent<Rigidbody2D>();
        brain ??= GetComponent<BlindHuntressEnemyBrain>();
        combat ??= GetComponent<BlindHuntressEnemyCombat>();
        animationDriver ??= GetComponent<BlindHuntressEnemyAnimationDriver>();
        spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
    }

    private void HandleDeath()
    {
        _isDead = true;
        Died?.Invoke();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = deadTint;
        }

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        SetManagedBehavioursEnabled(false);

        if (respawnOnDeath)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    private void SetManagedBehavioursEnabled(bool enabled)
    {
        if (brain != null)
        {
            brain.enabled = enabled;
        }

        if (combat != null)
        {
            combat.enabled = enabled;
        }

        if (animationDriver != null)
        {
            animationDriver.enabled = enabled;
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
        transform.position = _spawnPosition;
        RestoreFullHealth();
    }
}
