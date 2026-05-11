using System.Collections.Generic;
using UnityEngine;

// 역할:
// - 칼 휘두르기 이펙트에 붙는 트리거 콜라이더로, 접촉한 적에게 데미지를 전달합니다.
// - Activate/Deactivate로 SwordWeapon이 타이밍을 제어합니다.
//
// 구조 포인트:
// - 에디터에서 콜라이더 범위를 직접 조정해 판정 영역을 눈으로 확인합니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class SlashHitbox : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackUpForce = 2.5f;
    [SerializeField] private LayerMask hitLayers;

    private readonly HashSet<MonoBehaviour> _alreadyHit = new();
    private GameObject _owner;
    private Vector2 _aimDirection = Vector2.right;

    public void Activate(GameObject owner, Vector2 aimDirection)
    {
        _owner = owner;
        _aimDirection = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : Vector2.right;
        _alreadyHit.Clear();
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    private void Awake()
    {
        if (hitLayers.value == 0)
        {
            hitLayers = LayerMask.GetMask("Enemy");
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
        }

        collider.isTrigger = true;
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_owner != null && other.transform.IsChildOf(_owner.transform))
        {
            return;
        }

        if (((1 << other.gameObject.layer) & hitLayers) == 0)
        {
            return;
        }

        MonoBehaviour target = ResolveDamageReceiver(other);
        if (target == null || _alreadyHit.Contains(target) || target is not IDamageReceiver damageReceiver)
        {
            return;
        }

        _alreadyHit.Add(target);
        Vector2 knockback = _aimDirection * knockbackForce + Vector2.up * knockbackUpForce;
        if (damageReceiver.ReceiveHit(damage, knockback, _owner))
        {
            CombatHitFeedback.PlayLightHit();
        }
    }

    private MonoBehaviour ResolveDamageReceiver(Collider2D hit)
    {
        MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour is IDamageReceiver)
            {
                return behaviour;
            }
        }

        return null;
    }
}
