using UnityEngine;

// 역할:
// - 가장 단순한 추격형 적의 이동과 근접 공격 예시를 제공합니다.
// - EnemyInteraction을 중심으로 자기 체력과 플레이어 추적을 직접 처리합니다.
//
// 구조 포인트:
// - 복잡한 적 추가 전에 재사용 가능한 기본 적 구조를 확인할 때 보는 파일입니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyInteraction))]
[RequireComponent(typeof(SpriteRenderer))]
public class ChaserEnemyController : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private string fallbackTargetTag = "Player";
    [SerializeField] private float reacquireInterval = 0.35f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.25f;
    [SerializeField] private float attackRange = 1.1f;
    [SerializeField] private float stopDistance = 0.75f;

    [Header("Attack")]
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float attackKnockback = 5.5f;
    [SerializeField] private float attackCooldown = 0.9f;
    [SerializeField] private float attackRadius = 0.8f;
    [SerializeField] private Color bodyColor = new Color(1f, 0.62f, 0.24f, 1f);
    [SerializeField] private Color deadColor = new Color(0.24f, 0.24f, 0.28f, 1f);
    [SerializeField] private bool invertSpriteFacing = false;

    private EnemyInteraction _interaction;
    private PlayerInteraction _target;
    private SpriteRenderer _spriteRenderer;
    private float _attackCooldownTimer;
    private float _facing = 1f;
    private float _reacquireTimer;

    private void Awake()
    {
        CacheComponents();
        ConfigureHealth();
        ApplyFacing();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        CacheComponents();
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = bodyColor;
        }
    }

    private void Update()
    {
        CacheComponents();
        RefreshTarget();

        if (_interaction == null || !_interaction.IsAlive)
        {
            return;
        }

        if (_attackCooldownTimer > 0f)
        {
            _attackCooldownTimer -= Time.deltaTime;
        }

        Transform target = _target != null ? _target.transform : null;
        if (target == null)
        {
            return;
        }

        float signedDistance = target.position.x - transform.position.x;
        float distance = Mathf.Abs(signedDistance);
        if (!Mathf.Approximately(signedDistance, 0f))
        {
            _facing = Mathf.Sign(signedDistance);
            ApplyFacing();
        }

        if (distance > Mathf.Max(stopDistance, attackRange))
        {
            transform.position += new Vector3(Mathf.Sign(signedDistance) * moveSpeed * Time.deltaTime, 0f, 0f);
            return;
        }

        if (distance <= attackRange && _attackCooldownTimer <= 0f)
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        _attackCooldownTimer = Mathf.Max(0.1f, attackCooldown);

        Vector2 attackCenter = (Vector2)transform.position + new Vector2(_facing * attackRadius * 0.55f, 0f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, attackRadius);
        Vector2 knockback = new Vector2(_facing * attackKnockback, attackKnockback * 0.25f);

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerInteraction target = hits[i].GetComponentInParent<PlayerInteraction>();
            if (target == null)
            {
                continue;
            }

            target.ReceiveHit(attackDamage, knockback, gameObject);
            break;
        }
    }

    private void ConfigureHealth()
    {
        if (_interaction == null)
        {
            return;
        }

        float configuredMaxHealth = _interaction.MaxHealth > 0f ? _interaction.MaxHealth : 1f;
        _interaction.ConfigureHealth(configuredMaxHealth, bodyColor, deadColor, preserveHealthRatio: true);
    }

    private void CacheComponents()
    {
        _interaction ??= GetComponent<EnemyInteraction>();
        _spriteRenderer ??= GetComponent<SpriteRenderer>();
    }

    private void RefreshTarget()
    {
        if (_target != null && _target.IsAlive)
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
        _target = fallbackObject != null ? fallbackObject.GetComponentInParent<PlayerInteraction>() : null;
        if (_target != null && !_target.IsAlive)
        {
            _target = null;
        }
    }

    private void ApplyFacing()
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        bool faceRight = _facing >= 0f;
        _spriteRenderer.flipX = invertSpriteFacing ? faceRight : !faceRight;
        _spriteRenderer.color = bodyColor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.62f, 0.24f, 0.9f);
        Vector3 center = transform.position + new Vector3(_facing * attackRadius * 0.55f, 0f, 0f);
        Gizmos.DrawWireSphere(center, attackRadius);
    }
}
