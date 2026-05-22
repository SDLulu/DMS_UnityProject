using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(P_PlayerController))]
public class P_PlayerDashAfterimage : MonoBehaviour
{
    [Header("Afterimage")]
    [SerializeField] private Material afterimageMaterial;
    [SerializeField] private float spawnInterval = 0.035f;
    [SerializeField] private float lifetime = 0.18f;
    [SerializeField] private int maxActiveAfterimages = 8;
    [SerializeField] private Color tint = new Color(0.15f, 1f, 0.92f, 0.72f);
    [SerializeField] private int sortingOrderOffset = -1;

    private readonly Queue<AfterimageInstance> activeAfterimages = new();
    private P_PlayerController controller;
    private SpriteRenderer visualRenderer;
    private float spawnTimer;

    private void Awake()
    {
        controller = GetComponent<P_PlayerController>();
        CacheVisualRenderer();
    }

    private void LateUpdate()
    {
        TickAfterimages();

        if (controller == null || !controller.IsDashing)
        {
            spawnTimer = 0f;
            return;
        }

        if (visualRenderer == null)
        {
            CacheVisualRenderer();
        }

        if (visualRenderer == null || visualRenderer.sprite == null)
        {
            return;
        }

        spawnTimer -= Time.unscaledDeltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        SpawnAfterimage();
        spawnTimer = Mathf.Max(0.005f, spawnInterval);
    }

    private void CacheVisualRenderer()
    {
        Transform visualRoot = controller != null ? controller.VisualRoot : null;
        visualRenderer = visualRoot != null
            ? visualRoot.GetComponentInChildren<SpriteRenderer>()
            : GetComponentInChildren<SpriteRenderer>();
    }

    private void SpawnAfterimage()
    {
        while (activeAfterimages.Count >= Mathf.Max(1, maxActiveAfterimages))
        {
            DestroyAfterimage(activeAfterimages.Dequeue());
        }

        GameObject instance = new GameObject("DashAfterimage");
        instance.transform.SetPositionAndRotation(visualRenderer.transform.position, visualRenderer.transform.rotation);
        instance.transform.localScale = visualRenderer.transform.lossyScale;

        SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
        renderer.sprite = visualRenderer.sprite;
        renderer.flipX = visualRenderer.flipX;
        renderer.flipY = visualRenderer.flipY;
        renderer.drawMode = visualRenderer.drawMode;
        renderer.size = visualRenderer.size;
        renderer.maskInteraction = visualRenderer.maskInteraction;
        renderer.sortingLayerID = visualRenderer.sortingLayerID;
        renderer.sortingOrder = visualRenderer.sortingOrder + sortingOrderOffset;
        renderer.color = tint;

        if (afterimageMaterial != null)
        {
            renderer.sharedMaterial = afterimageMaterial;
        }

        activeAfterimages.Enqueue(new AfterimageInstance(instance, renderer, lifetime, tint));
    }

    private void TickAfterimages()
    {
        int count = activeAfterimages.Count;
        for (int i = 0; i < count; i++)
        {
            AfterimageInstance afterimage = activeAfterimages.Dequeue();
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
                activeAfterimages.Enqueue(afterimage);
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
        while (activeAfterimages.Count > 0)
        {
            DestroyAfterimage(activeAfterimages.Dequeue());
        }

        spawnTimer = 0f;
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