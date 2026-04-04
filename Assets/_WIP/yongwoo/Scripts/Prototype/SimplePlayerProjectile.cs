using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class SimplePlayerProjectile : MonoBehaviour
{
    private Vector2 _direction = Vector2.right;
    private float _speed = 15f;
    private float _lifetime = 1f;
    private float _damage = 1f;
    private Vector2 _knockback = new Vector2(5f, 1f);
    private GameObject _owner;
    private PrototypeFaction _sourceFaction = PrototypeFaction.Player;
    private Rigidbody2D _body;

    public void Configure(
        Vector2 direction,
        float speed,
        float lifetime,
        float damage,
        Vector2 knockback,
        float radius,
        Color color,
        GameObject owner,
        PrototypeFaction sourceFaction)
    {
        _direction = direction.sqrMagnitude <= 0.001f ? Vector2.right : direction.normalized;
        _speed = Mathf.Max(0.1f, speed);
        _lifetime = Mathf.Max(0.05f, lifetime);
        _damage = Mathf.Max(0f, damage);
        _knockback = knockback;
        _owner = owner;
        _sourceFaction = sourceFaction;

        _body ??= GetComponent<Rigidbody2D>();
        _body.bodyType = RigidbodyType2D.Kinematic;
        _body.gravityScale = 0f;
        _body.freezeRotation = true;
        _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _body.linearVelocity = _direction * _speed;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        renderer.sprite = PrototypeRuntimeSpriteUtility.CircleSprite;
        renderer.color = color;
        renderer.sortingLayerName = "Effect";
        renderer.sortingOrder = 18;

        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        circleCollider.radius = Mathf.Max(0.02f, radius);

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
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
        if (target != null)
        {
            if (target.Faction == _sourceFaction && _sourceFaction != PrototypeFaction.Neutral)
            {
                return;
            }

            if (target.TryApplyDamage(_damage, _knockback, _owner))
            {
                Destroy(gameObject);
            }

            return;
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
