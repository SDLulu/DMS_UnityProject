using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// P3-C3 안전지대 축소.
// 안전 원 바깥(아레나 나머지)을 빨간 위험 영역으로 표시하고, 플레이어가 밖에 있으면 1회 피해.

[DisallowMultipleComponent]
public class BossSafeZoneCollapse : MonoBehaviour
{
    [Header("Danger Overlay")]
    [SerializeField] private Color warningDangerColor = new Color(1f, 0.12f, 0.1f, 0.38f);
    [SerializeField] private Color activeDangerColor = new Color(1f, 0.08f, 0.06f, 0.55f);

    private Transform _player;
    private GameObject _owner;
    private float _safeRadius;
    private float _damage;
    private bool _hit;
    private SpriteRenderer _dangerFill;
    private SpriteRenderer _safeRing;
    private SpriteMask _safeMask;

    public void Arm(
        Transform player,
        GameObject owner,
        Bounds arenaBounds,
        Vector3 safeCenter,
        float safeRadius,
        float warningDuration,
        float activeDuration,
        float damage)
    {
        _player = player;
        _owner = owner;
        _safeRadius = Mathf.Max(0.1f, safeRadius);
        _damage = Mathf.Max(0f, damage);

        BuildVisuals(arenaBounds, safeCenter);
        StartCoroutine(LifetimeRoutine(Mathf.Max(0f, warningDuration), Mathf.Max(0.01f, activeDuration)));
    }

    private void BuildVisuals(Bounds arenaBounds, Vector3 safeCenter)
    {
        Vector3 arenaCenter = arenaBounds.center;
        arenaCenter.z = safeCenter.z;
        transform.position = arenaCenter;

        GameObject maskObject = new GameObject("SafeMask");
        maskObject.transform.SetParent(transform, false);
        maskObject.transform.position = safeCenter;

        _safeMask = maskObject.AddComponent<SpriteMask>();
        _safeMask.sprite = RuntimeSpriteUtility.CircleSprite;
        _safeMask.alphaCutoff = 0.05f;
        _safeMask.frontSortingLayerID = SortingLayer.NameToID("Effect");
        _safeMask.frontSortingOrder = 36;
        _safeMask.backSortingLayerID = SortingLayer.NameToID("Effect");
        _safeMask.backSortingOrder = 34;
        maskObject.transform.localScale = RuntimeSpriteUtility.UniformWorldScale(RuntimeSpriteUtility.CircleSprite, _safeRadius * 2f);

        GameObject ringObject = new GameObject("SafeRing");
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.position = safeCenter;

        _safeRing = ringObject.AddComponent<SpriteRenderer>();
        _safeRing.sprite = RuntimeSpriteUtility.RingSprite;
        _safeRing.color = new Color(0.45f, 0.85f, 1f, 0.85f);
        _safeRing.sortingLayerName = "Effect";
        _safeRing.sortingOrder = 37;
        _safeRing.transform.localScale = RuntimeSpriteUtility.UniformWorldScale(RuntimeSpriteUtility.RingSprite, _safeRadius * 2f);
        if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
        {
            _safeRing.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
        }

        GameObject fillObject = new GameObject("DangerFill");
        fillObject.transform.SetParent(transform, false);
        fillObject.transform.localPosition = Vector3.zero;

        _dangerFill = fillObject.AddComponent<SpriteRenderer>();
        _dangerFill.sprite = RuntimeSpriteUtility.WhiteSprite;
        _dangerFill.color = warningDangerColor;
        _dangerFill.sortingLayerName = "Effect";
        _dangerFill.sortingOrder = 35;
        _dangerFill.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
        {
            _dangerFill.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
        }

        Vector3 arenaSize = arenaBounds.size;
        arenaSize.z = 1f;
        _dangerFill.transform.localScale = RuntimeSpriteUtility.WorldSizeToLocalScale(RuntimeSpriteUtility.WhiteSprite, new Vector2(arenaSize.x, arenaSize.y));
    }

    private IEnumerator LifetimeRoutine(float warningDuration, float activeDuration)
    {
        if (_dangerFill != null)
        {
            _dangerFill.color = warningDangerColor;
        }

        float warningTimer = 0f;
        while (warningTimer < warningDuration)
        {
            float pulse = 0.5f + Mathf.Sin(Time.time * 18f) * 0.5f;
            if (_safeRing != null)
            {
                _safeRing.color = new Color(0.45f, 0.85f, 1f, Mathf.Lerp(0.45f, 0.95f, pulse));
                _safeRing.transform.localScale = RuntimeSpriteUtility.UniformWorldScale(
                    RuntimeSpriteUtility.RingSprite,
                    _safeRadius * 2f * Mathf.Lerp(0.98f, 1.06f, pulse));
            }

            warningTimer += Time.deltaTime;
            yield return null;
        }

        if (_dangerFill != null)
        {
            _dangerFill.color = activeDangerColor;
        }
        if (_safeRing != null)
        {
            _safeRing.color = Color.white;
            _safeRing.transform.localScale = RuntimeSpriteUtility.UniformWorldScale(RuntimeSpriteUtility.RingSprite, _safeRadius * 2.08f);
        }

        float timer = activeDuration;
        while (timer > 0f)
        {
            TryHitOutsidePlayer();
            timer -= Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void TryHitOutsidePlayer()
    {
        if (_hit || _player == null)
        {
            return;
        }

        Vector3 safeCenter = _safeMask != null ? _safeMask.transform.position : transform.position;
        float distance = Vector2.Distance(safeCenter, _player.position);
        if (distance <= _safeRadius)
        {
            return;
        }

        PlayerInteraction receiver = ResolveDamageReceiver(_player);
        if (receiver == null)
        {
            return;
        }

        _hit = true;
        receiver.ReceiveHit(_damage, Vector2.zero, _owner);
    }

    private static PlayerInteraction ResolveDamageReceiver(Transform target)
    {
        return target.GetComponentInParent<PlayerInteraction>();
    }
}
