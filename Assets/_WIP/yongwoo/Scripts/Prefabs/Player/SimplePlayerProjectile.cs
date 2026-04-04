using UnityEngine;

// 역할:
// - 플레이어가 발사한 투사체의 이동, 충돌, 수명 종료를 관리합니다.
// - 충돌 시 객체별 Interaction 창구를 찾아 피해를 전달합니다.
//
// 구조 포인트:
// - 발사 결정은 SimplePlayerCombat이 하고, 이 파일은 개별 탄의 실행만 맡습니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(TrailRenderer))]
public class SimplePlayerProjectile : MonoBehaviour
{
    private Vector2 _direction = Vector2.right;
    private float _speed = 15f;
    private float _lifetime = 1f;
    private float _damage = 1f;
    private Vector2 _knockback = new Vector2(5f, 1f);
    private GameObject _owner;
    private Rigidbody2D _body;

    public void Configure(
        Vector2 direction,
        float speed,
        float lifetime,
        float damage,
        Vector2 knockback,
        float radius,
        Color color,
        GameObject owner)
    {
        _direction = direction.sqrMagnitude <= 0.001f ? Vector2.right : direction.normalized;
        _speed = Mathf.Max(0.1f, speed);
        _lifetime = Mathf.Max(0.05f, lifetime);
        _owner = owner;
        _damage = Mathf.Max(0f, damage);
        _knockback = knockback;

        _body ??= GetComponent<Rigidbody2D>();
        _body.bodyType = RigidbodyType2D.Kinematic;
        _body.gravityScale = 0f;
        _body.freezeRotation = true;
        _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _body.linearVelocity = _direction * _speed;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteUtility.CircleSprite;
        renderer.color = color;
        renderer.sortingLayerName = "Effect";
        renderer.sortingOrder = 42;
        if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
        {
            renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
        }

        transform.localScale = Vector3.one * Mathf.Max(0.22f, radius * 4.5f);

        TrailRenderer trail = GetComponent<TrailRenderer>();
        trail.time = 0.14f;
        trail.startWidth = Mathf.Max(0.08f, radius * 2f);
        trail.endWidth = 0.01f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.sortingLayerName = "Effect";
        trail.sortingOrder = 41;
        Material trailMaterial = RuntimeSpriteUtility.CreateUnlitColorMaterial(color);
        if (trailMaterial != null)
        {
            trail.sharedMaterial = trailMaterial;
        }

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(Mathf.Clamp01(color.a), 0f),
                new GradientAlphaKey(0f, 1f)
            });
        trail.colorGradient = gradient;
        trail.emitting = true;

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

        BossInteraction boss = other.GetComponentInParent<BossInteraction>();
        if (boss != null)
        {
            boss.ReceiveHit(_damage, _knockback, _owner);
            Destroy(gameObject);
            return;
        }

        EnemyInteraction enemy = other.GetComponentInParent<EnemyInteraction>();
        if (enemy != null)
        {
            enemy.ReceiveHit(_damage, _knockback, _owner);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
