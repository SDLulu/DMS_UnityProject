using System.Collections.Generic;
using UnityEngine;

// 역할:
// - DeadRevolver 근접 적의 주먹/검/방패 판정 창구를 맡습니다.
// - 애니메이션 이벤트가 열고 닫는 동안만 플레이어에게 피해를 줍니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class DeadRevolverEnemyMeleeHitbox : MonoBehaviour
{
    private readonly HashSet<PlayerInteraction> _alreadyHit = new();

    private Collider2D _collider;
    private GameObject _owner;
    private float _damage;
    private Vector2 _knockback;
    private bool _isActive;

    public void Activate(GameObject owner, float damage, Vector2 knockback)
    {
        _owner = owner;
        _damage = Mathf.Max(0f, damage);
        _knockback = knockback;
        _alreadyHit.Clear();
        _isActive = true;

        EnsureCollider();
        _collider.enabled = true;
    }

    public void Deactivate()
    {
        _isActive = false;
        _alreadyHit.Clear();

        EnsureCollider();
        _collider.enabled = false;
    }

    private void Awake()
    {
        EnsureCollider();
        _collider.isTrigger = true;
        _collider.enabled = false;
    }

    private void OnDisable()
    {
        Deactivate();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isActive)
        {
            return;
        }

        if (_owner != null && other.transform.IsChildOf(_owner.transform))
        {
            return;
        }

        PlayerInteraction target = other.GetComponentInParent<PlayerInteraction>();
        if (target == null || target.IsDead || _alreadyHit.Contains(target))
        {
            return;
        }

        _alreadyHit.Add(target);
        if (target.ReceiveHit(_damage, _knockback, _owner))
        {
            CombatHitFeedback.PlayLightHit();
        }
    }

    private void EnsureCollider()
    {
        _collider ??= GetComponent<Collider2D>();
    }
}
