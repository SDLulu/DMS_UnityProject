using UnityEngine;

// 보스 잔상/임시 이펙트를 짧게 사라지게 하는 공통 컴포넌트입니다.

[DisallowMultipleComponent]
public class BossEffectFade : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float duration = 0.18f;
    [SerializeField] private bool shrink;
    [SerializeField] private bool scaleOverLifetime;
    [SerializeField, Min(0f)] private float endScaleMultiplier = 0.75f;

    private SpriteRenderer[] _renderers;
    private Color[] _startColors;
    private Vector3 _startScale;
    private float _age;

    public void Begin(float lifetime, bool shrinkOverLifetime)
    {
        duration = Mathf.Max(0.01f, lifetime);
        shrink = shrinkOverLifetime;
        scaleOverLifetime = shrinkOverLifetime;
        endScaleMultiplier = shrinkOverLifetime ? 0.75f : 1f;
        CacheState();
    }

    public void Begin(float lifetime, float targetScaleMultiplier)
    {
        duration = Mathf.Max(0.01f, lifetime);
        shrink = false;
        scaleOverLifetime = true;
        endScaleMultiplier = Mathf.Max(0f, targetScaleMultiplier);
        CacheState();
    }

    private void Awake()
    {
        CacheState();
    }

    private void CacheState()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>();
        _startColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _startColors[i] = _renderers[i] != null ? _renderers[i].color : Color.clear;
        }

        _startScale = transform.localScale;
        _age = 0f;
    }

    private void Update()
    {
        _age += Time.deltaTime;
        float t = Mathf.Clamp01(_age / duration);
        float alpha = 1f - t;

        for (int i = 0; i < _renderers.Length; i++)
        {
            SpriteRenderer renderer = _renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Color color = _startColors[i];
            color.a *= alpha;
            renderer.color = color;
        }

        if (shrink || scaleOverLifetime)
        {
            transform.localScale = Vector3.Lerp(_startScale, _startScale * endScaleMultiplier, t);
        }

        if (_age >= duration)
        {
            Destroy(gameObject);
        }
    }
}
