using UnityEngine;

// 역할:
// - 보스가 발사하는 탄의 이동, 수명, 충돌 시 피해 적용을 관리합니다.
// - 생성 시 받은 속도와 데미지를 기준으로 PlayerInteraction 대상에게만 피해를 전달합니다.
//
// 구조 포인트:
// - 패턴 선택은 BossController가 하고, 이 파일은 투사체 한 발의 생명주기만 담당합니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class BossProjectile : MonoBehaviour
{
    private Vector2 _direction = Vector2.right;
    private float _speed = 6f;
    private float _lifetime = 2f;
    private float _knockback = 4f;
    private GameObject _owner;
    private float _damage = 1f;

    public void Configure(
        Vector2 direction,
        float speed,
        float lifetime,
        float damage,
        float knockback,
        float radius,
        Color color,
        GameObject owner)
    {
        _direction = direction.sqrMagnitude <= 0.001f ? Vector2.right : direction.normalized;
        _speed = speed;
        _lifetime = lifetime;
        _knockback = knockback;
        _owner = owner;
        _damage = damage;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteUtility.CircleSprite;
        renderer.color = new Color(color.r, color.g, color.b, 0.78f);
        renderer.sortingOrder = 18;

        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        circleCollider.radius = radius;
    }

    private void Update()
    {
        transform.position += (Vector3)(_direction * (_speed * Time.deltaTime));
        _lifetime -= Time.deltaTime;

        if (_lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_owner != null && other.transform.IsChildOf(_owner.transform))
        {
            return;
        }

        PlayerInteraction target = other.GetComponentInParent<PlayerInteraction>();
        if (target == null)
        {
            if (!other.isTrigger)
            {
                Destroy(gameObject);
            }
            return;
        }

        Vector2 knockback = _direction * _knockback + Vector2.up * (_knockback * 0.35f);
        if (target.ReceiveHit(_damage, knockback, _owner))
        {
            CombatHitFeedback.PlayLightHit();
        }

        Destroy(gameObject);
    }
}
