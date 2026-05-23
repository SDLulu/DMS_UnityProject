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

    public void Arm(GameObject owner, float damage, Vector2 size, float warningDuration, float activeDuration, Color warningColor, Color activeColor)
    {
        EnsureSetup();
        _owner = owner;
        _damage = Mathf.Max(0f, damage);
        _warningColor = warningColor;
        _vertical = size.y >= size.x;
        transform.localScale = new Vector3(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y), 1f);
        _collider.size = Vector2.one;
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

        _renderer ??= GetComponent<SpriteRenderer>();
        if (_renderer == null)
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
        }

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
        _coreRenderer.transform.localScale = _vertical
            ? new Vector3(0.18f, 1f, 1f)
            : new Vector3(1f, 0.18f, 1f);
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

        IDamageReceiver receiver = ResolveDamageReceiver(other);
        if (receiver == null || _hitTargets.Contains(receiver))
        {
            return;
        }

        _hitTargets.Add(receiver);
        receiver.ReceiveHit(_damage, Vector2.zero, _owner);
    }

    private static IDamageReceiver ResolveDamageReceiver(Collider2D hit)
    {
        MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IDamageReceiver receiver)
            {
                return receiver;
            }
        }

        return null;
    }
}
