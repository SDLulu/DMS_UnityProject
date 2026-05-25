using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 보스 지연 장판의 런타임 판정/임시 비주얼.
// 경고 원이 먼저 보이고, activeDuration 동안만 IDamageReceiver에 피해를 줍니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class BossBlastZone : MonoBehaviour
{
    private readonly HashSet<IDamageReceiver> _hitTargets = new();

    private CircleCollider2D _collider;
    private SpriteRenderer _renderer;
    private SpriteRenderer _ringRenderer;
    private SpriteRenderer _coreRenderer;
    private GameObject _owner;
    private float _damage;
    private Color _warningColor;
    private Color _activeColor;

    public void Arm(GameObject owner, float damage, float radius, float warningDuration, float activeDuration, Color warningColor, Color activeColor)
    {
        EnsureSetup();
        _owner = owner;
        _damage = Mathf.Max(0f, damage);
        _warningColor = warningColor;
        _activeColor = activeColor;
        float safeRadius = Mathf.Max(0.05f, radius);
        _collider.radius = 0.5f;
        transform.localScale = RuntimeSpriteUtility.UniformWorldScale(RuntimeSpriteUtility.CircleSprite, safeRadius * 2f);
        _renderer.color = _warningColor;
        if (_ringRenderer != null)
        {
            _ringRenderer.color = WithAlpha(_warningColor, 0.85f);
        }
        if (_coreRenderer != null)
        {
            _coreRenderer.color = new Color(1f, 1f, 1f, 0f);
        }
        StartCoroutine(LifetimeRoutine(Mathf.Max(0f, warningDuration), Mathf.Max(0.01f, activeDuration)));
    }

    private void Awake()
    {
        EnsureSetup();
    }

    private void EnsureSetup()
    {
        _collider ??= GetComponent<CircleCollider2D>();
        _collider.isTrigger = true;
        _collider.enabled = false;

        _renderer ??= GetComponent<SpriteRenderer>();
        if (_renderer == null)
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (_renderer.sprite == null)
        {
            _renderer.sprite = RuntimeSpriteUtility.CircleSprite;
            _renderer.sortingLayerName = "Effect";
            _renderer.sortingOrder = 40;
            if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
            {
                _renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
            }
        }

        if (_ringRenderer == null)
        {
            GameObject ring = new GameObject("PulseRing");
            ring.transform.SetParent(transform, false);
            ring.transform.localScale = Vector3.one * 1.08f;
            _ringRenderer = ring.AddComponent<SpriteRenderer>();
            _ringRenderer.sprite = RuntimeSpriteUtility.RingSprite;
            _ringRenderer.sortingLayerName = "Effect";
            _ringRenderer.sortingOrder = 42;
            if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
            {
                _ringRenderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
            }
        }

        if (_coreRenderer == null)
        {
            GameObject core = new GameObject("HotCore");
            core.transform.SetParent(transform, false);
            core.transform.localScale = Vector3.one * 0.28f;
            _coreRenderer = core.AddComponent<SpriteRenderer>();
            _coreRenderer.sprite = RuntimeSpriteUtility.CircleSprite;
            _coreRenderer.sortingLayerName = "Effect";
            _coreRenderer.sortingOrder = 43;
            if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
            {
                _coreRenderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
            }
        }
    }

    private IEnumerator LifetimeRoutine(float warningDuration, float activeDuration)
    {
        _collider.enabled = false;
        YongwooAudioManager.Play(YongwooSfxId.BossBlastArm, 0.5f, 0.04f);
        float warningTimer = 0f;
        while (warningTimer < warningDuration)
        {
            float pulse = 0.5f + Mathf.Sin(Time.time * 18f) * 0.5f;
            _renderer.color = WithAlpha(_warningColor, Mathf.Lerp(0.18f, 0.34f, pulse));
            if (_ringRenderer != null)
            {
                _ringRenderer.color = WithAlpha(_warningColor, Mathf.Lerp(0.55f, 0.95f, pulse));
                _ringRenderer.transform.localScale = Vector3.one * Mathf.Lerp(1.02f, 1.13f, pulse);
            }

            warningTimer += Time.deltaTime;
            yield return null;
        }

        _hitTargets.Clear();
        _renderer.color = _activeColor;
        if (_ringRenderer != null)
        {
            _ringRenderer.color = Color.white;
            _ringRenderer.transform.localScale = Vector3.one * 1.1f;
        }
        if (_coreRenderer != null)
        {
            _coreRenderer.color = new Color(1f, 1f, 1f, 0.55f);
        }
        _collider.enabled = true;
        YongwooAudioManager.Play(YongwooSfxId.BossBlastExplode, 0.68f, 0.04f);

        yield return new WaitForSeconds(activeDuration);
        Destroy(gameObject);
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
