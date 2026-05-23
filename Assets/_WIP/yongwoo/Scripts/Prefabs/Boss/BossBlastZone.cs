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
        transform.localScale = Vector3.one * (safeRadius * 2f);
        _renderer.color = _warningColor;
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
    }

    private IEnumerator LifetimeRoutine(float warningDuration, float activeDuration)
    {
        _collider.enabled = false;
        yield return new WaitForSeconds(warningDuration);

        _hitTargets.Clear();
        _renderer.color = _activeColor;
        _collider.enabled = true;

        yield return new WaitForSeconds(activeDuration);
        Destroy(gameObject);
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
