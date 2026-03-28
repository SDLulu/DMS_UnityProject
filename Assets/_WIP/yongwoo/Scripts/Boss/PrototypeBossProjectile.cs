using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class PrototypeBossProjectile : MonoBehaviour
{
    private Vector2 _direction = Vector2.right;
    private float _speed = 6f;
    private float _lifetime = 2f;
    private float _damage = 1f;
    private float _knockback = 4f;
    private GameObject _owner;
    private PrototypeFaction _faction = PrototypeFaction.Enemy;

    public void Configure(
        Vector2 direction,
        float speed,
        float lifetime,
        float damage,
        float knockback,
        float radius,
        Color color,
        GameObject owner,
        PrototypeFaction faction)
    {
        _direction = direction.sqrMagnitude <= 0.001f ? Vector2.right : direction.normalized;
        _speed = speed;
        _lifetime = lifetime;
        _damage = damage;
        _knockback = knockback;
        _owner = owner;
        _faction = faction;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        renderer.sprite = PrototypeRuntimeSpriteUtility.CircleSprite;
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

        PrototypeHealth target = other.GetComponentInParent<PrototypeHealth>();
        if (target == null)
        {
            if (!other.isTrigger)
            {
                Destroy(gameObject);
            }
            return;
        }

        if (target.Faction == _faction && _faction != PrototypeFaction.Neutral)
        {
            return;
        }

        Vector2 knockback = _direction * _knockback + Vector2.up * (_knockback * 0.35f);
        if (target.TryApplyDamage(_damage, knockback, _owner))
        {
            Destroy(gameObject);
        }
    }
}
