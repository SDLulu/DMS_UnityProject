using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 보스 레이저 벽의 경고/활성 판정.

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class BossLaserWallZone : MonoBehaviour
{
    private readonly HashSet<IDamageReceiver> _hitTargets = new();

    private BoxCollider2D _collider;
    private SpriteRenderer _renderer;
    private SpriteRenderer _coreRenderer;
    private GameObject _owner;
    private float _damage;
    private bool _vertical;
    private Color _warningColor;
    private Vector2 _worldSize;

    public void Arm(GameObject owner, float damage, Vector2 size, float warningDuration, float activeDuration, Color warningColor, Color activeColor)
    {
        EnsureSetup();
        _owner = owner;
        _damage = Mathf.Max(0f, damage);
        _warningColor = warningColor;
        _worldSize = new Vector2(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y));
        _vertical = _worldSize.y >= _worldSize.x;
        ApplyVisualAndColliderSize(_worldSize);
        _collider.enabled = false;
        _renderer.color = warningColor;
        ConfigureCoreScale();
        StartCoroutine(LifetimeRoutine(Mathf.Max(0f, warningDuration), Mathf.Max(0.01f, activeDuration), activeColor));
    }

    private void Awake()
    {
        EnsureSetup();
    }

    private void EnsureSetup()
    {
        _collider ??= GetComponent<BoxCollider2D>();
        _collider.isTrigger = true;

        _renderer ??= ResolveVisualRenderer();

        if (_renderer.sprite == null)
        {
            _renderer.sprite = RuntimeSpriteUtility.WhiteSprite;
            _renderer.sortingLayerName = "Effect";
            _renderer.sortingOrder = 41;
            if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
            {
                _renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
            }
        }

        if (_coreRenderer == null)
        {
            Transform coreTransform = transform.Find("HotCore");
            if (coreTransform != null)
            {
                _coreRenderer = coreTransform.GetComponent<SpriteRenderer>();
            }
        }

        if (_coreRenderer == null)
        {
            GameObject core = new GameObject("HotCore");
            core.transform.SetParent(transform, false);
            _coreRenderer = core.AddComponent<SpriteRenderer>();
            _coreRenderer.sprite = RuntimeSpriteUtility.WhiteSprite;
            _coreRenderer.sortingLayerName = "Effect";
            _coreRenderer.sortingOrder = 42;
            _coreRenderer.color = new Color(1f, 1f, 1f, 0f);
            if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
            {
                _coreRenderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
            }
        }
    }

    private IEnumerator LifetimeRoutine(float warningDuration, float activeDuration, Color activeColor)
    {
        float warningTimer = 0f;
        while (warningTimer < warningDuration)
        {
            float pulse = 0.5f + Mathf.Sin(Time.time * 20f) * 0.5f;
            _renderer.color = WithAlpha(_warningColor, Mathf.Lerp(0.18f, 0.36f, pulse));
            if (_coreRenderer != null)
            {
                _coreRenderer.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.06f, 0.18f, pulse));
            }

            warningTimer += Time.deltaTime;
            yield return null;
        }

        _hitTargets.Clear();
        _renderer.color = activeColor;
        if (_coreRenderer != null)
        {
            _coreRenderer.color = new Color(1f, 1f, 1f, 0.6f);
        }
        _collider.enabled = true;
        Physics2D.SyncTransforms();

        yield return new WaitForSeconds(activeDuration);
        Destroy(gameObject);
    }

    private void ConfigureCoreScale()
    {
        if (_coreRenderer == null)
        {
            return;
        }

        _coreRenderer.transform.localPosition = Vector3.zero;
        if (_renderer != null)
        {
            Vector3 visualScale = _renderer.transform.localScale;
            _coreRenderer.transform.localScale = _vertical
                ? new Vector3(Mathf.Clamp(visualScale.x * 0.35f, 0.08f, 1f), visualScale.y, 1f)
                : new Vector3(visualScale.x, Mathf.Clamp(visualScale.y * 0.35f, 0.08f, 1f), 1f);
            return;
        }

        _coreRenderer.transform.localScale = _vertical
            ? new Vector3(0.18f, 1f, 1f)
            : new Vector3(1f, 0.18f, 1f);
    }

    private void ApplyVisualAndColliderSize(Vector2 worldSize)
    {
        if (_renderer.sprite == null)
        {
            _renderer.sprite = RuntimeSpriteUtility.WhiteSprite;
        }

        transform.localScale = Vector3.one;
        _collider.offset = Vector2.zero;
        _collider.size = worldSize;

        Transform visualTransform = _renderer.transform;
        visualTransform.localPosition = Vector3.zero;
        visualTransform.localScale = RuntimeSpriteUtility.WorldSizeToLocalScale(_renderer.sprite, worldSize);
    }

    private SpriteRenderer ResolveVisualRenderer()
    {
        Transform visualTransform = transform.Find("Visual");
        if (visualTransform != null)
        {
            SpriteRenderer visualRenderer = visualTransform.GetComponent<SpriteRenderer>();
            if (visualRenderer != null)
            {
                return visualRenderer;
            }
        }

        SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
        if (rootRenderer != null)
        {
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = rootRenderer.transform.localPosition;
            visual.transform.localRotation = rootRenderer.transform.localRotation;
            visual.transform.localScale = rootRenderer.transform.localScale;

            SpriteRenderer migrated = visual.AddComponent<SpriteRenderer>();
            migrated.sprite = rootRenderer.sprite;
            migrated.color = rootRenderer.color;
            migrated.sortingLayerName = rootRenderer.sortingLayerName;
            migrated.sortingOrder = rootRenderer.sortingOrder;
            migrated.sharedMaterial = rootRenderer.sharedMaterial;
            Destroy(rootRenderer);
            return migrated;
        }

        GameObject created = new GameObject("Visual");
        created.transform.SetParent(transform, false);
        return created.AddComponent<SpriteRenderer>();
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (_owner != null && other.transform.IsChildOf(_owner.transform))
        {
            return;
        }

        PlayerInteraction receiver = ResolveDamageReceiver(other);
        if (receiver == null || _hitTargets.Contains(receiver))
        {
            return;
        }

        _hitTargets.Add(receiver);
        receiver.ReceiveHit(_damage, Vector2.zero, _owner);
    }

    private static PlayerInteraction ResolveDamageReceiver(Collider2D hit)
    {
        return hit.GetComponentInParent<PlayerInteraction>();
    }
}
