using System.Collections.Generic;
using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class SimplePlayerCombat : MonoBehaviour
{
    public event Action AttackPerformed;

    [Header("Attack")]
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float attackCooldown = 0.28f;
    [SerializeField] private float attackAnimationDuration = 0.18f;
    [SerializeField] private Vector2 attackSize = new Vector2(1.2f, 0.9f);
    [SerializeField] private Vector2 attackOffset = new Vector2(0.95f, 0f);
    [SerializeField] private float attackKnockbackX = 6f;
    [SerializeField] private float attackKnockbackY = 2.5f;
    [SerializeField] private LayerMask hitLayers;

    [Header("Attack Visual")]
    [SerializeField] private bool showAttackVisual = true;
    [SerializeField] private float attackVisualDuration = 0.12f;
    [SerializeField] private Color attackVisualColor = new Color(1f, 0.9f, 0.2f, 0.28f);

    private float _cooldownTimer;
    private float _attackVisualTimer;
    private GameObject _attackVisualObject;
    private SpriteRenderer _attackVisualRenderer;
    private PrototypeHealth _selfHealth;
    private SimplePlayerController _controller;

    public float AttackCooldown => attackCooldown;
    public float AttackAnimationDuration => attackAnimationDuration;

    public PlayerAttackConfig CreateConfigSnapshot()
    {
        return new PlayerAttackConfig
        {
            attackDamage = attackDamage,
            attackCooldown = attackCooldown,
            attackAnimationDuration = attackAnimationDuration,
            attackSize = attackSize,
            attackOffset = attackOffset,
            attackKnockbackX = attackKnockbackX,
            attackKnockbackY = attackKnockbackY,
            showAttackVisual = showAttackVisual,
            attackVisualDuration = attackVisualDuration,
            attackVisualColor = new SerializableColor(
                attackVisualColor.r,
                attackVisualColor.g,
                attackVisualColor.b,
                attackVisualColor.a)
        };
    }

    public void ApplyConfig(PlayerAttackConfig config)
    {
        config = PrototypePlayerConfigLoader.Sanitize(new PrototypePlayerConfig { attack = config }).attack;
        attackDamage = config.attackDamage;
        attackCooldown = config.attackCooldown;
        attackAnimationDuration = config.attackAnimationDuration;
        attackSize = config.attackSize;
        attackOffset = config.attackOffset;
        attackKnockbackX = config.attackKnockbackX;
        attackKnockbackY = config.attackKnockbackY;
        showAttackVisual = config.showAttackVisual;
        attackVisualDuration = config.attackVisualDuration;
        attackVisualColor = config.attackVisualColor.ToColor();
    }

    private void Awake()
    {
        if (hitLayers.value == 0)
        {
            hitLayers = LayerMask.GetMask("Default");
        }

        _selfHealth = GetComponent<PrototypeHealth>();
        _controller = GetComponent<SimplePlayerController>();
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
        }

        UpdateAttackVisual();

        if (_cooldownTimer <= 0f && ReadAttackPressed())
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        _cooldownTimer = attackCooldown;
        AttackPerformed?.Invoke();

        float facing = GetFacingDirection();
        Vector2 center = (Vector2)transform.position + new Vector2(attackOffset.x * facing, attackOffset.y);
        ShowAttackVisual(center);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackSize, 0f, hitLayers);
        HashSet<PrototypeHealth> damaged = new HashSet<PrototypeHealth>();

        for (int i = 0; i < hits.Length; i++)
        {
            PrototypeHealth target = hits[i].GetComponentInParent<PrototypeHealth>();
            if (target == null || target == _selfHealth || damaged.Contains(target))
            {
                continue;
            }

            damaged.Add(target);
            target.TryApplyDamage(
                attackDamage,
                new Vector2(facing * attackKnockbackX, attackKnockbackY),
                gameObject);
        }
    }

    private void ShowAttackVisual(Vector2 center)
    {
        if (!showAttackVisual)
        {
            return;
        }

        EnsureAttackVisual();
        _attackVisualTimer = attackVisualDuration;
        _attackVisualObject.transform.position = new Vector3(center.x, center.y, 0f);
        _attackVisualObject.transform.localScale = new Vector3(attackSize.x, attackSize.y, 1f);
        _attackVisualRenderer.color = attackVisualColor;
        _attackVisualObject.SetActive(true);
    }

    private void UpdateAttackVisual()
    {
        if (_attackVisualObject == null || !_attackVisualObject.activeSelf)
        {
            return;
        }

        _attackVisualTimer -= Time.deltaTime;
        if (_attackVisualTimer <= 0f)
        {
            _attackVisualObject.SetActive(false);
            return;
        }

        float ratio = Mathf.Clamp01(_attackVisualTimer / Mathf.Max(0.01f, attackVisualDuration));
        Color color = attackVisualColor;
        color.a *= ratio;
        _attackVisualRenderer.color = color;
    }

    private void EnsureAttackVisual()
    {
        if (_attackVisualObject != null)
        {
            return;
        }

        _attackVisualObject = new GameObject("PlayerAttackVisual");
        _attackVisualRenderer = _attackVisualObject.AddComponent<SpriteRenderer>();
        _attackVisualRenderer.sprite = PrototypeRuntimeSpriteUtility.WhiteSprite;
        _attackVisualRenderer.sortingOrder = 16;
        _attackVisualObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (_attackVisualObject != null)
        {
            _attackVisualObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_attackVisualObject != null)
        {
            Destroy(_attackVisualObject);
        }
    }

    private bool ReadAttackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.jKey.wasPressedThisFrame
                || Keyboard.current.kKey.wasPressedThisFrame
                || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        }
#endif
        return Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.K) || Input.GetMouseButtonDown(0);
    }

    private void OnDrawGizmosSelected()
    {
        float facing = GetFacingDirection();
        Vector2 center = (Vector2)transform.position + new Vector2(attackOffset.x * facing, attackOffset.y);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, attackSize);
    }

    private float GetFacingDirection()
    {
        if (_controller == null)
        {
            _controller = GetComponent<SimplePlayerController>();
        }

        if (_controller != null)
        {
            return _controller.FacingDirection >= 0f ? 1f : -1f;
        }

        return 1f;
    }
}
