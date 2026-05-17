using System.Collections.Generic;
using UnityEngine;

// 역할:
// - DeadRevolver 근접 적의 주먹/검/방패 판정 창구를 맡습니다.
// - 콜라이더는 항상 enable: 영역 안 플레이어를 항상 추적합니다.
// - Activate가 호출된 동안에만 추적 중인 플레이어에게 피해를 줍니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class DeadRevolverEnemyMeleeHitbox : MonoBehaviour
{
    private readonly HashSet<PlayerInteraction> _alreadyHit = new();
    private readonly HashSet<PlayerInteraction> _inRange = new();

    private Collider2D _collider;
    private GameObject _owner;
    private float _damage;
    private Vector2 _knockback;
    private bool _isActive;

    public bool HasPlayerInRange => _inRange.Count > 0;

    public void Activate(GameObject owner, float damage, Vector2 knockback)
    {
        _owner = owner;
        _damage = Mathf.Max(0f, damage);
        _knockback = knockback;
        _alreadyHit.Clear();
        _isActive = true;

        // 활성화 시 이미 영역 안에 있는 플레이어에게도 즉시 데미지 시도
        foreach (var target in _inRange)
        {
            TryHit(target);
        }
    }

    public void Deactivate()
    {
        _isActive = false;
        _alreadyHit.Clear();
    }

    private void Awake()
    {
        EnsureCollider();
        _collider.isTrigger = true;
        _collider.enabled = true;
    }

    private void OnDisable()
    {
        Deactivate();
        _inRange.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TrackOverlap(other);
    }

    // Enter 이벤트가 음수 scale 폴리곤 등에서 누락되는 케이스를 보완.
    // Stay는 매 FixedUpdate에 호출되어 영역 안 상태를 항상 신뢰 가능.
    private void OnTriggerStay2D(Collider2D other)
    {
        TrackOverlap(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerInteraction target = other.GetComponentInParent<PlayerInteraction>();
        if (target == null)
        {
            return;
        }

        _inRange.Remove(target);
    }

    private void TrackOverlap(Collider2D other)
    {
        if (_owner != null && other.transform.IsChildOf(_owner.transform))
        {
            return;
        }

        PlayerInteraction target = other.GetComponentInParent<PlayerInteraction>();
        if (target == null)
        {
            return;
        }

        bool wasInRange = _inRange.Contains(target);
        _inRange.Add(target);

        if (_isActive && !wasInRange)
        {
            TryHit(target);
        }
    }

    private void TryHit(PlayerInteraction target)
    {
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
