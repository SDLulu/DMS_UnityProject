using UnityEngine;

// 역할:
// - DeadRevolver 총병이 발사하는 탄의 이동과 플레이어 충돌만 처리합니다.
// - 적끼리의 오발 판정은 막고, PlayerInteraction 대상에게만 피해를 줍니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class DeadRevolverEnemyProjectile : MonoBehaviour
{
    private Vector2 _direction = Vector2.right;
    private float _speed = 8f;
    private float _lifetime = 1.2f;
    private float _damage = 1f;
    private float _knockback = 5f;
    private GameObject _owner;

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
        _speed = Mathf.Max(0.1f, speed);
        _lifetime = Mathf.Max(0.05f, lifetime);
        _damage = Mathf.Max(0f, damage);
        _knockback = Mathf.Max(0f, knockback);
        _owner = owner;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteUtility.CircleSprite;
        renderer.color = new Color(color.r, color.g, color.b, 0.82f);
        renderer.sortingLayerName = "Effect";
        renderer.sortingOrder = 24;

        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        circleCollider.radius = Mathf.Max(0.02f, radius);
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
