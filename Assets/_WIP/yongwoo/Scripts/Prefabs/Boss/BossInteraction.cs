using System;
using System.Collections;
using UnityEngine;

// 역할:
// - 보스(P1 본체)의 체력, 피격, 사망 창구를 맡습니다.
// - 보스시나리오 확정 가정에 따라 "경직·넉백 없음"입니다. 콤보 5타가 빠르게 누적될 수 있도록 무적 시간은 짧게 둡니다.
// - 텔레포트 무적 윈도우 동안에는 ReceiveHit이 거부됩니다.

[DisallowMultipleComponent]
public class BossInteraction : MonoBehaviour, IDamageReceiver
{
    [Header("Health")]
    [Tooltip("P1 본체 HP. 보스시나리오: 5타.")]
    [SerializeField] private int maxHealth = 5;
    [Tooltip("같은 공격에 연속으로 또 맞지 않게 막는 짧은 무적 시간입니다. 콤보 누적은 가능해야 합니다.")]
    [SerializeField, Min(0f)] private float invulnerabilityDuration = 0.05f;

    [Header("Feedback")]
    [Tooltip("피격 플래시가 유지되는 시간입니다.")]
    [SerializeField] private float flashDuration = 0.08f;
    [Tooltip("피격 시 잠깐 바뀌는 색입니다.")]
    [SerializeField] private Color damageFlashColor = Color.white;

    [Header("References")]
    [Tooltip("피격 색 변화를 적용할 스프라이트 렌더러입니다. 비워두면 자식에서 자동으로 찾습니다.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    public event Action<int> Damaged;      // current health
    public event Action Died;

    private int _currentHealth;
    private float _invulnerabilityTimer;
    private bool _isDead;
    private bool _isTeleportInvulnerable;
    private Color _baseColor = Color.white;
    private Coroutine _flashRoutine;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => _isDead;
    public bool IsAlive => !_isDead;

    public void ResetHealth(int newMaxHealth)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        _currentHealth = maxHealth;
        _invulnerabilityTimer = 0f;
        _isDead = false;
        _isTeleportInvulnerable = false;

        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
            _flashRoutine = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = _baseColor;
        }
    }

    public void SetTeleportInvulnerable(bool active)
    {
        _isTeleportInvulnerable = active;
    }

    public void SetBaseVisualColor(Color color)
    {
        _baseColor = color;
        if (spriteRenderer != null && !_isDead)
        {
            spriteRenderer.color = _baseColor;
        }
    }

    private void Awake()
    {
        _currentHealth = Mathf.Max(1, maxHealth);
        spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            _baseColor = spriteRenderer.color;
        }
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
        if (_isDead || damage <= 0f)
        {
            return false;
        }

        if (_isTeleportInvulnerable || _invulnerabilityTimer > 0f)
        {
            return false;
        }

        if (source != null && source.transform.IsChildOf(transform))
        {
            return false;
        }

        int amount = Mathf.Max(1, Mathf.RoundToInt(damage));
        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        _invulnerabilityTimer = invulnerabilityDuration;

        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
        }

        _flashRoutine = StartCoroutine(FlashRoutine());
        Damaged?.Invoke(_currentHealth);

        if (_currentHealth <= 0)
        {
            _isDead = true;
            Died?.Invoke();
        }

        return true;
    }

    private IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null)
        {
            yield break;
        }

        spriteRenderer.color = damageFlashColor;
        yield return new WaitForSeconds(flashDuration);

        if (!_isDead && spriteRenderer != null)
        {
            spriteRenderer.color = _baseColor;
        }
    }
}
