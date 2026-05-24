using UnityEngine;

// Hollow Knight 스타일 소울 파편 — 중력 포물선 후 수명 종료 시 소멸.

[DisallowMultipleComponent]
public class BossSoulMote : MonoBehaviour
{
    private Vector2 _velocity;
    private float _lifetime = 3f;
    private float _age;
    private float _gravity = -5.5f;
    private float _horizontalDrag = 0.992f;
    private SpriteRenderer _renderer;
    private Color _startColor;
    private Vector3 _startScale;

    public void Launch(Vector2 velocity, float lifetime, Color color, float scale, int sortingOrder, float gravity = -5.5f)
    {
        _velocity = velocity;
        _lifetime = Mathf.Max(0.2f, lifetime);
        _gravity = gravity;
        _age = 0f;

        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null)
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
        }

        _renderer.sprite = RuntimeSpriteUtility.CircleSprite;
        _renderer.sortingLayerName = "Effect";
        _renderer.sortingOrder = sortingOrder;
        if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
        {
            _renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
        }

        _startColor = color;
        _renderer.color = color;
        _startScale = Vector3.one * scale;
        transform.localScale = _startScale;
    }

    private void Update()
    {
        _age += Time.deltaTime;
        float t = Mathf.Clamp01(_age / _lifetime);

        _velocity.y += _gravity * Time.deltaTime;
        _velocity.x *= _horizontalDrag;
        transform.position += (Vector3)(_velocity * Time.deltaTime);

        if (_renderer != null)
        {
            Color color = _startColor;
            float fadeStart = 0.72f;
            float alphaFactor = t < fadeStart ? 1f : 1f - ((t - fadeStart) / (1f - fadeStart));
            color.a = _startColor.a * alphaFactor;
            _renderer.color = color;
        }

        transform.localScale = Vector3.Lerp(_startScale, _startScale * 0.5f, t);

        if (_age >= _lifetime)
        {
            Destroy(gameObject);
        }
    }
}
