using System.Collections.Generic;
using UnityEngine;

// 역할:
// - 대시 중 플레이어 비주얼의 현재 스프라이트를 짧은 청록 잔상으로 남깁니다.
// - 본체 Visual, Animator, 이동 로직은 건드리지 않습니다.

[DisallowMultipleComponent]
[RequireComponent(typeof(SimplePlayerController))]
public class PlayerDashAfterimage : MonoBehaviour
{
    [Header("Afterimage")]
    [SerializeField] private Material afterimageMaterial;
    [SerializeField] private float spawnInterval = 0.035f;
    [SerializeField] private float lifetime = 0.18f;
    [SerializeField] private int maxActiveAfterimages = 8;
    [SerializeField] private Color tint = new Color(0.15f, 1f, 0.92f, 0.72f);
    [SerializeField] private int sortingOrderOffset = -1;

    private readonly Queue<AfterimageInstance> _activeAfterimages = new Queue<AfterimageInstance>();
    private SimplePlayerController _controller;
    private SpriteRenderer _visualRenderer;
    private float _spawnTimer;

    private void Awake()
    {
        _controller = GetComponent<SimplePlayerController>();
        CacheVisualRenderer();
    }

    private void LateUpdate()
    {
        TickAfterimages();

        if (_controller == null || !_controller.IsDashing)
        {
            _spawnTimer = 0f;
            return;
        }

        if (_visualRenderer == null)
        {
            CacheVisualRenderer();
        }

        if (_visualRenderer == null || _visualRenderer.sprite == null)
        {
            return;
        }

        _spawnTimer -= Time.unscaledDeltaTime;
        if (_spawnTimer > 0f)
        {
            return;
        }

        SpawnAfterimage();
        _spawnTimer = Mathf.Max(0.005f, spawnInterval);
    }

    private void CacheVisualRenderer()
    {
        Transform visualRoot = _controller != null ? _controller.VisualRoot : null;
        _visualRenderer = visualRoot != null ? visualRoot.GetComponent<SpriteRenderer>() : null;
    }

    private void SpawnAfterimage()
    {
        while (_activeAfterimages.Count >= Mathf.Max(1, maxActiveAfterimages))
        {
            DestroyAfterimage(_activeAfterimages.Dequeue());
        }

        GameObject instance = new GameObject("DashAfterimage");
        instance.transform.SetPositionAndRotation(_visualRenderer.transform.position, _visualRenderer.transform.rotation);
        instance.transform.localScale = _visualRenderer.transform.lossyScale;

        SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
        renderer.sprite = _visualRenderer.sprite;
        renderer.flipX = _visualRenderer.flipX;
        renderer.flipY = _visualRenderer.flipY;
        renderer.drawMode = _visualRenderer.drawMode;
        renderer.size = _visualRenderer.size;
        renderer.maskInteraction = _visualRenderer.maskInteraction;
        renderer.sortingLayerID = _visualRenderer.sortingLayerID;
        renderer.sortingOrder = _visualRenderer.sortingOrder + sortingOrderOffset;
        renderer.color = tint;

        if (afterimageMaterial != null)
        {
            renderer.sharedMaterial = afterimageMaterial;
        }

        _activeAfterimages.Enqueue(new AfterimageInstance(instance, renderer, lifetime, tint));
    }

    private void TickAfterimages()
    {
        int count = _activeAfterimages.Count;
        for (int i = 0; i < count; i++)
        {
            AfterimageInstance afterimage = _activeAfterimages.Dequeue();
            if (afterimage.Renderer == null)
            {
                continue;
            }

            afterimage.Age += Time.unscaledDeltaTime;
            float normalizedAge = lifetime > 0f ? Mathf.Clamp01(afterimage.Age / lifetime) : 1f;
            Color color = afterimage.StartColor;
            color.a *= 1f - normalizedAge;
            afterimage.Renderer.color = color;

            if (afterimage.Age >= lifetime)
            {
                DestroyAfterimage(afterimage);
            }
            else
            {
                _activeAfterimages.Enqueue(afterimage);
            }
        }
    }

    private static void DestroyAfterimage(AfterimageInstance afterimage)
    {
        if (afterimage.GameObject != null)
        {
            Destroy(afterimage.GameObject);
        }
    }

    private void OnDisable()
    {
        while (_activeAfterimages.Count > 0)
        {
            DestroyAfterimage(_activeAfterimages.Dequeue());
        }

        _spawnTimer = 0f;
    }

    private sealed class AfterimageInstance
    {
        public readonly GameObject GameObject;
        public readonly SpriteRenderer Renderer;
        public readonly Color StartColor;
        public float Age;

        public AfterimageInstance(GameObject gameObject, SpriteRenderer renderer, float lifetime, Color startColor)
        {
            GameObject = gameObject;
            Renderer = renderer;
            StartColor = startColor;
            Age = Mathf.Max(0f, lifetime) * 0.08f;
        }
    }
}
