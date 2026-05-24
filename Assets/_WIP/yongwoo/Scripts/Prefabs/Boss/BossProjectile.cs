using UnityEngine;

// 역할:
// - 보스가 발사한 탄의 이동·충돌·수명을 관리합니다.
// - 슬로우 모션에 영향받도록 Time.deltaTime을 사용합니다 (timeScale 적용).
// - 보스 자신 또는 보스 자식 콜라이더는 무시합니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(TrailRenderer))]
public class BossProjectile : MonoBehaviour
{
    [Header("Fallback Visual")]
    [SerializeField] private Color defaultColor = new Color(1f, 0.35f, 0.3f, 0.85f);
    [SerializeField] private Color accentColor = new Color(0f, 0.9f, 1f, 0.9f);
    [SerializeField] private Color hotCoreColor = new Color(1f, 0.95f, 0.72f, 1f);
    [SerializeField] private float defaultRadius = 0.1f;
    [SerializeField, Min(0.1f)] private float visualScaleMultiplier = 4.5f;
    [SerializeField, Min(0.05f)] private float trailTime = 0.16f;
    [SerializeField, Min(0.5f)] private float trailHeadWidthScale = 1.35f;
    [SerializeField, Range(0f, 0.35f)] private float trailTailWidthFactor = 0.04f;
    [SerializeField, Min(0f)] private float pulseScale = 0.18f;
    [SerializeField, Min(0f)] private float pulseSpeed = 22f;
    [SerializeField, Min(0f)] private float hitScanPadding = 0.04f;

    private float _lifetime = 4f;
    private float _damage = 1f;
    private GameObject _owner;
    private Rigidbody2D _body;
    private SpriteRenderer _bodyRenderer;
    private SpriteRenderer _coreRenderer;
    private SpriteRenderer _ringRenderer;
    private SpriteRenderer _streakRenderer;
    private Vector3 _coreBaseScale;
    private Vector3 _ringBaseScale;
    private Vector3 _streakBaseScale;
    private CircleCollider2D _collider;
    private float _visualWorldRadius;
    private float _age;
    private bool _burstSpawned;
    private bool _hitResolved;

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

        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.Clear();
            trail.emitting = true;
        }

        SpawnFlashBurst(transform.position, dir, defaultColor, accentColor, 5, 0.11f, 0.04f);
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
        _bodyRenderer = GetComponent<SpriteRenderer>();
        if (_bodyRenderer != null)
        {
            if (_bodyRenderer.sprite == null)
            {
                _bodyRenderer.sprite = RuntimeSpriteUtility.CircleSprite;
            }

            _bodyRenderer.color = defaultColor;
            _bodyRenderer.sortingLayerName = "Effect";
            _bodyRenderer.sortingOrder = 42;
            if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
            {
                _bodyRenderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
            }
        }

        ApplyVisualScale();
        SyncColliderToVisual();

        _coreRenderer = EnsureLayer("Projectile_HotCore", RuntimeSpriteUtility.CircleSprite, 46, hotCoreColor, new Vector3(0.55f, 0.55f, 1f));
        _ringRenderer = EnsureLayer("Projectile_Ring", RuntimeSpriteUtility.RingSprite, 45, accentColor, new Vector3(1.15f, 1.15f, 1f));
        _streakRenderer = EnsureLayer("Projectile_Streak", RuntimeSpriteUtility.WhiteSprite, 44, accentColor, new Vector3(1.35f, 0.18f, 1f));
        _coreBaseScale = _coreRenderer.transform.localScale;
        _ringBaseScale = _ringRenderer.transform.localScale;
        _streakBaseScale = _streakRenderer.transform.localScale;

        SetupTrail();
    }

    private void ApplyVisualScale()
    {
        float uniformScale = Mathf.Max(0.22f, defaultRadius * visualScaleMultiplier);
        transform.localScale = Vector3.one * uniformScale;
        _visualWorldRadius = ComputeVisualWorldRadius();
    }

    private float ComputeVisualWorldRadius()
    {
        Sprite sprite = _bodyRenderer != null && _bodyRenderer.sprite != null
            ? _bodyRenderer.sprite
            : RuntimeSpriteUtility.CircleSprite;
        float spriteRadius = Mathf.Max(0.001f, sprite.bounds.extents.x);
        float scale = Mathf.Max(0.001f, Mathf.Abs(transform.localScale.x));
        return spriteRadius * scale;
    }

    private void SyncColliderToVisual()
    {
        _collider ??= GetComponent<CircleCollider2D>();
        _collider.isTrigger = true;

        Sprite sprite = _bodyRenderer != null && _bodyRenderer.sprite != null
            ? _bodyRenderer.sprite
            : RuntimeSpriteUtility.CircleSprite;
        _collider.radius = Mathf.Max(0.001f, sprite.bounds.extents.x);
    }

    private SpriteRenderer EnsureLayer(string objectName, Sprite sprite, int sortingOrder, Color color, Vector3 localScale)
    {
        Transform child = transform.Find(objectName);
        if (child == null)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        if (!child.TryGetComponent(out SpriteRenderer renderer))
        {
            renderer = child.gameObject.AddComponent<SpriteRenderer>();
        }

        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = localScale;
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerName = "Effect";
        renderer.sortingOrder = sortingOrder;
        if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
        {
            renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
        }

        return renderer;
    }

    private void SetupTrail()
    {
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail == null)
        {
            return;
        }

        float headWidth = Mathf.Max(0.08f, _visualWorldRadius * 2f * trailHeadWidthScale);

        trail.time = trailTime;
        trail.minVertexDistance = 0.012f;
        // widthCurve/colorGradient t=0 → 총알(머리), t=1 → 꼬리(오래된 궤적)
        trail.widthMultiplier = headWidth;
        trail.widthCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.22f, 0.74f),
            new Keyframe(0.5f, 0.42f),
            new Keyframe(0.78f, 0.14f),
            new Keyframe(1f, trailTailWidthFactor));
        trail.numCapVertices = 4;
        trail.numCornerVertices = 3;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.sortingLayerName = "Effect";
        trail.sortingOrder = 41;
        trail.textureMode = LineTextureMode.Stretch;
        trail.alignment = LineAlignment.TransformZ;

        Material trailMaterial = RuntimeSpriteUtility.CreateUnlitColorMaterial(defaultColor);
        if (trailMaterial != null)
        {
            trail.sharedMaterial = trailMaterial;
        }

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(accentColor, 0f),
                new GradientColorKey(defaultColor, 0.55f),
                new GradientColorKey(defaultColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(Mathf.Clamp01(defaultColor.a), 0f),
                new GradientAlphaKey(0.82f, 0.18f),
                new GradientAlphaKey(0.45f, 0.45f),
                new GradientAlphaKey(0.12f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            });
        trail.colorGradient = gradient;
        trail.emitting = true;
    }

    private void Update()
    {
        _age += Time.deltaTime;
        _lifetime -= Time.deltaTime;
        if (_lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        UpdateHybridProjectileVisuals();
        ScanPlayerOverlap();
    }

    private void UpdateHybridProjectileVisuals()
    {
        float pulse = 0.5f + Mathf.Sin(_age * pulseSpeed) * 0.5f;

        SetAlpha(_bodyRenderer, Mathf.Lerp(0.62f, 0.95f, pulse));
        SetAlpha(_coreRenderer, Mathf.Lerp(0.78f, 1f, pulse));
        SetAlpha(_ringRenderer, Mathf.Lerp(0.18f, 0.55f, pulse));
        SetAlpha(_streakRenderer, Mathf.Lerp(0.24f, 0.58f, 1f - pulse));

        if (_coreRenderer != null)
        {
            _coreRenderer.transform.localScale = _coreBaseScale * (1f + pulseScale * pulse);
        }

        if (_ringRenderer != null)
        {
            _ringRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, _age * 180f);
            _ringRenderer.transform.localScale = _ringBaseScale * (1f + pulseScale * (1f - pulse));
        }
        if (_streakRenderer != null)
        {
            _streakRenderer.transform.localScale = new Vector3(
                _streakBaseScale.x * Mathf.Lerp(0.82f, 1.12f, pulse),
                _streakBaseScale.y * Mathf.Lerp(0.72f, 1.22f, 1f - pulse),
                _streakBaseScale.z);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (TryResolveHit(other, destroyOnWorld: true))
        {
            return;
        }
    }

    private void OnDestroy()
    {
        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.emitting = false;
        }
    }

    private void SpawnImpactBurst()
    {
        if (_burstSpawned)
        {
            return;
        }

        _burstSpawned = true;
        Vector2 direction = _body != null && _body.linearVelocity.sqrMagnitude > 0.001f
            ? _body.linearVelocity.normalized
            : (Vector2)transform.right;
        SpawnFlashBurst(transform.position, direction, defaultColor, accentColor, 8, 0.14f, 0.055f);
        SpawnImpactRing(transform.position, accentColor);
    }

    private static void SpawnFlashBurst(Vector3 position, Vector2 direction, Color primary, Color secondary, int count, float lifetime, float scatter)
    {
        Vector2 forward = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        Vector2 tangent = new Vector2(-forward.y, forward.x);

        for (int i = 0; i < count; i++)
        {
            float side = count <= 1 ? 0f : (i / (float)(count - 1) - 0.5f) * 2f;
            Vector3 offset = (Vector3)(tangent * side * scatter) - (Vector3)(forward * Random.Range(0.02f, 0.11f));

            GameObject spark = new GameObject("Boss_ProjectileSpark");
            spark.transform.position = position + offset;
            spark.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg + Random.Range(-38f, 38f));
            spark.transform.localScale = new Vector3(Random.Range(0.08f, 0.24f), Random.Range(0.006f, 0.018f), 1f);

            SpriteRenderer renderer = spark.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteUtility.WhiteSprite;
            renderer.sortingLayerName = "Effect";
            renderer.sortingOrder = 72 + i;
            renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
            Color color = Random.value > 0.45f ? primary : secondary;
            color.a = Random.Range(0.36f, 0.82f);
            renderer.color = color;

            BossEffectFade fade = spark.AddComponent<BossEffectFade>();
            fade.Begin(lifetime * Random.Range(0.7f, 1.15f), shrinkOverLifetime: true);
        }
    }

    private static void SpawnImpactRing(Vector3 position, Color color)
    {
        GameObject ring = new GameObject("Boss_ProjectileImpactRing");
        ring.transform.position = position;

        SpriteRenderer renderer = ring.AddComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteUtility.RingSprite;
        renderer.sortingLayerName = "Effect";
        renderer.sortingOrder = 70;
        renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
        color.a = 0.62f;
        renderer.color = color;

        float diameter = 0.34f;
        Vector3 spriteSize = renderer.sprite != null ? renderer.sprite.bounds.size : Vector3.one;
        ring.transform.localScale = new Vector3(
            diameter / Mathf.Max(0.0001f, spriteSize.x),
            diameter / Mathf.Max(0.0001f, spriteSize.y),
            1f);

        BossEffectFade fade = ring.AddComponent<BossEffectFade>();
        fade.Begin(0.18f, 2.4f);
    }

    private static void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null)
        {
            return;
        }

        Color color = renderer.color;
        color.a = Mathf.Clamp01(alpha);
        renderer.color = color;
    }

    private void ScanPlayerOverlap()
    {
        if (_hitResolved || _collider == null)
        {
            return;
        }

        float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        float radius = Mathf.Max(0.01f, _collider.radius * scale + hitScanPadding);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit == _collider)
            {
                continue;
            }

            PlayerInteraction player = hit.GetComponentInParent<PlayerInteraction>();
            if (player == null)
            {
                continue;
            }

            TryDamageReceiver(player);
            return;
        }
    }

    private bool TryResolveHit(Collider2D other, bool destroyOnWorld)
    {
        if (_hitResolved || other == null)
        {
            return true;
        }

        if (_owner != null && other.transform.IsChildOf(_owner.transform))
        {
            return true;
        }

        MonoBehaviour receiver = ResolveDamageReceiver(other);
        if (receiver is IDamageReceiver damageReceiver)
        {
            TryDamageReceiver(damageReceiver);
            return true;
        }

        if (destroyOnWorld && !other.isTrigger)
        {
            ResolveImpactOnly();
            return true;
        }

        return false;
    }

    private void TryDamageReceiver(IDamageReceiver damageReceiver)
    {
        if (_hitResolved || damageReceiver == null)
        {
            return;
        }

        _hitResolved = true;
        damageReceiver.ReceiveHit(_damage, Vector2.zero, _owner);
        SpawnImpactBurst();
        Destroy(gameObject);
    }

    private void ResolveImpactOnly()
    {
        if (_hitResolved)
        {
            return;
        }

        _hitResolved = true;
        SpawnImpactBurst();
        Destroy(gameObject);
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
