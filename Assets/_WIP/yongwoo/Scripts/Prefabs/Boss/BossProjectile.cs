using UnityEngine;

// 역할:
// - 보스가 발사한 탄의 이동·충돌·수명을 관리합니다.
// - 슬로우 모션에 영향받도록 Time.deltaTime을 사용합니다 (timeScale 적용).
// - 보스 자신 또는 보스 자식 콜라이더는 무시합니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BossProjectile : MonoBehaviour
{
    [Header("Fallback Visual")]
    [SerializeField] private Color defaultColor = new Color(1f, 0.25f, 0.25f, 0.9f);
    [SerializeField] private float defaultRadius = 0.12f;

    private float _lifetime = 4f;
    private float _damage = 1f;
    private GameObject _owner;
    private Rigidbody2D _body;

    public void Launch(Vector2 direction, float speed, float lifetime, float damage, GameObject owner)
    {
        Vector2 dir = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        _lifetime = Mathf.Max(0.05f, lifetime);
        _damage = Mathf.Max(0f, damage);
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

        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        if (circleCollider.radius < 0.02f)
        {
            circleCollider.radius = Mathf.Max(0.02f, defaultRadius);
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
            damageReceiver.ReceiveHit(_damage, Vector2.zero, _owner);
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
