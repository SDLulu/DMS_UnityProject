using UnityEngine;

// 역할:
// - 기억조각/HOME 회수 코어처럼 영상 외 서사 오브젝트에 붙이는 가벼운 런타임 비주얼입니다.
// - 별도 아트가 없어도 씬에서 위치와 크기를 보며 조정할 수 있게 pulse/ring/glitch bar를 만듭니다.

[DisallowMultipleComponent]
public class StoryMemoryVisual : MonoBehaviour
{
    [SerializeField] private Color primaryColor = new(0f, 0.92f, 1f, 0.78f);
    [SerializeField] private Color accentColor = new(1f, 0.82f, 0.24f, 0.64f);
    [SerializeField, Min(0.1f)] private float pulseSpeed = 2.8f;
    [SerializeField, Min(0f)] private float pulseScale = 0.08f;
    [SerializeField] private bool buildChildLayers = true;

    private SpriteRenderer _body;
    private SpriteRenderer _ringA;
    private SpriteRenderer _ringB;
    private SpriteRenderer _bar;
    private Vector3 _baseScale = Vector3.one;
    private float _clockOffset;

    private void Awake()
    {
        EnsureVisuals();
    }

    private void OnEnable()
    {
        EnsureVisuals();
        _clockOffset = Random.Range(0f, 10f);
    }

    private void Update()
    {
        EnsureVisuals();

        float t = Time.unscaledTime + _clockOffset;
        float pulse = 0.5f + Mathf.Sin(t * pulseSpeed) * 0.5f;
        transform.localScale = _baseScale * (1f + pulse * pulseScale);

        SetRenderer(_body, primaryColor, Mathf.Lerp(0.48f, 0.82f, pulse));
        SetRenderer(_ringA, primaryColor, Mathf.Lerp(0.18f, 0.52f, 1f - pulse));
        SetRenderer(_ringB, accentColor, Mathf.Lerp(0.18f, 0.46f, pulse));
        SetRenderer(_bar, accentColor, Mathf.Lerp(0.22f, 0.64f, pulse));

        if (_ringA != null)
        {
            _ringA.transform.localRotation = Quaternion.Euler(0f, 0f, t * 18f);
        }
        if (_ringB != null)
        {
            _ringB.transform.localRotation = Quaternion.Euler(0f, 0f, -t * 26f);
        }
        if (_bar != null)
        {
            _bar.transform.localPosition = new Vector3(Mathf.Sin(t * 6.1f) * 0.16f, Mathf.Sin(t * 4.3f) * 0.12f, 0f);
            _bar.transform.localScale = new Vector3(Mathf.Lerp(0.2f, 0.42f, pulse), 0.035f, 1f);
        }
    }

    private void EnsureVisuals()
    {
        _body ??= GetComponent<SpriteRenderer>();
        if (_body == null)
        {
            _body = gameObject.AddComponent<SpriteRenderer>();
        }

        _body.sprite = RuntimeSpriteUtility.CircleSprite;
        _body.sortingLayerName = "Effect";
        _body.sortingOrder = 30;
        _body.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;

        if (_baseScale == Vector3.one)
        {
            _baseScale = transform.localScale;
        }

        if (!buildChildLayers)
        {
            return;
        }

        _ringA ??= EnsureChildRenderer("Ring_A", RuntimeSpriteUtility.RingSprite, 31, new Vector3(1.35f, 1.35f, 1f));
        _ringB ??= EnsureChildRenderer("Ring_B", RuntimeSpriteUtility.RingSprite, 32, new Vector3(1.85f, 0.72f, 1f));
        _bar ??= EnsureChildRenderer("Glitch_Bar", RuntimeSpriteUtility.WhiteSprite, 33, new Vector3(0.35f, 0.035f, 1f));
    }

    private SpriteRenderer EnsureChildRenderer(string objectName, Sprite sprite, int sortingOrder, Vector3 localScale)
    {
        Transform child = transform.Find(objectName);
        if (child == null)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = child.gameObject.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = sprite;
        renderer.sortingLayerName = "Effect";
        renderer.sortingOrder = sortingOrder;
        renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
        child.localScale = localScale;
        return renderer;
    }

    private static void SetRenderer(SpriteRenderer renderer, Color color, float alpha)
    {
        if (renderer == null)
        {
            return;
        }

        color.a = Mathf.Clamp01(alpha);
        renderer.color = color;
    }
}
