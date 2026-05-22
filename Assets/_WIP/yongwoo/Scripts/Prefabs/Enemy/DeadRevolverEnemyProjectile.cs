using UnityEngine;

// 역할:
// - DeadRevolver 총병이 발사하는 탄의 이동과 플레이어 충돌만 처리합니다.
// - 적끼리의 오발 판정은 막고, PlayerInteraction 대상에게만 피해를 줍니다.
//
// 구조 포인트:
// - 비주얼/콜라이더 매칭과 TrailRenderer 셋업은 SimplePlayerProjectile과 동일한 패턴을 따릅니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(TrailRenderer))]
public class DeadRevolverEnemyProjectile : MonoBehaviour
{
    private Vector2 _direction = Vector2.right;
    private float _speed = 8f;
    private float _lifetime = 1.2f;
    private float _damage = 1f;
    private float _knockback = 5f;
    private GameObject _owner;
    private Rigidbody2D _body;

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

        SetupVisuals(radius, color);

        _body ??= GetComponent<Rigidbody2D>();
        _body.linearVelocity = _direction * _speed;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _body.bodyType = RigidbodyType2D.Kinematic;
        _body.gravityScale = 0f;
        _body.freezeRotation = true;
        _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void SetupVisuals(float radius, Color color)
    {
        float effectiveRadius = Mathf.Max(0.02f, radius);
        Color bodyColor = new Color(color.r, color.g, color.b, 0.82f);

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteUtility.CircleSprite;
        renderer.color = bodyColor;
        renderer.sortingLayerName = "Effect";
        renderer.sortingOrder = 24;
        if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
        {
            renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
        }

        transform.localScale = Vector3.one * Mathf.Max(0.22f, effectiveRadius * 4.5f);

        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        circleCollider.radius = effectiveRadius;

        TrailRenderer trail = GetComponent<TrailRenderer>();
        trail.time = 0.14f;
        trail.startWidth = Mathf.Max(0.08f, effectiveRadius * 2f);
        trail.endWidth = 0.01f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.sortingLayerName = "Effect";
        trail.sortingOrder = 23;
        Material trailMaterial = RuntimeSpriteUtility.CreateUnlitColorMaterial(bodyColor);
        if (trailMaterial != null)
        {
            trail.sharedMaterial = trailMaterial;
        }

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(bodyColor, 0f),
                new GradientColorKey(bodyColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(Mathf.Clamp01(bodyColor.a), 0f),
                new GradientAlphaKey(0f, 1f)
            });
        trail.colorGradient = gradient;
        trail.Clear();
        trail.emitting = true;
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
