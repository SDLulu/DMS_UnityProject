using UnityEngine;

// 역할:
// - 플레이어가 발사한 투사체의 이동, 충돌, 수명 종료를 관리합니다.
// - 충돌 시 객체별 Interaction 창구를 찾아 피해를 전달합니다.
//
// 구조 포인트:
// - 발사 결정은 GunWeapon이 하고, 이 파일은 개별 탄의 실행만 맡습니다.
// - 프리팹에 스프라이트/트레일을 세팅해두면 그대로 사용하고, 없으면 폴백 비주얼을 생성합니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(TrailRenderer))]
public class SimplePlayerProjectile : MonoBehaviour
{
    [Header("Fallback Visual")]
    [SerializeField] private Color defaultColor = new Color(1f, 0.6f, 0.2f, 0.85f);
    [SerializeField] private float defaultRadius = 0.1f;

    private float _lifetime = 1f;
    private float _damage = 1f;
    private Vector2 _knockback;
    private GameObject _owner;
    private Rigidbody2D _body;

    public void Launch(
        Vector2 direction,
        float speed,
        float lifetime,
        float damage,
        Vector2 knockback,
        GameObject owner)
    {
        Vector2 dir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        _lifetime = Mathf.Max(0.05f, lifetime);
        _damage = Mathf.Max(0f, damage);
        _knockback = knockback;
        _owner = owner;

        _body ??= GetComponent<Rigidbody2D>();
        _body.linearVelocity = dir * Mathf.Max(0.1f, speed);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _body.bodyType = RigidbodyType2D.Kinematic;
        _body.gravityScale = 0f;
        _body.freezeRotation = true;
        _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        SetupFallbackVisuals();
    }

    private void SetupFallbackVisuals()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null && renderer.sprite == null)
        {
            renderer.sprite = RuntimeSpriteUtility.CircleSprite;
            renderer.color = defaultColor;
            renderer.sortingLayerName = "Effect";
            renderer.sortingOrder = 42;
            if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
            {
                renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
            }
        }

        transform.localScale = Vector3.one * Mathf.Max(0.22f, defaultRadius * 4.5f);

        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        if (circleCollider.radius < 0.02f)
        {
            circleCollider.radius = Mathf.Max(0.02f, defaultRadius);
        }

        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null && trail.sharedMaterial == null)
        {
            trail.time = 0.14f;
            trail.startWidth = Mathf.Max(0.08f, defaultRadius * 2f);
            trail.endWidth = 0.01f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sortingLayerName = "Effect";
            trail.sortingOrder = 41;
            Material trailMaterial = RuntimeSpriteUtility.CreateUnlitColorMaterial(defaultColor);
            if (trailMaterial != null)
            {
                trail.sharedMaterial = trailMaterial;
            }

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(defaultColor, 0f),
                    new GradientColorKey(defaultColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(Mathf.Clamp01(defaultColor.a), 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            trail.colorGradient = gradient;
            trail.emitting = true;
        }
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

        MonoBehaviour receiver = ResolveDamageReceiver(other);
        if (receiver is IDamageReceiver damageReceiver)
        {
            damageReceiver.ReceiveHit(_damage, _knockback, _owner);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }

    private static MonoBehaviour ResolveDamageReceiver(Collider2D hit)
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
