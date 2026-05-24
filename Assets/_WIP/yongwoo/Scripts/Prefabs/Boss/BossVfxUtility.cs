using UnityEngine;

// 보스전 코드 기반 VFX 헬퍼. 스프라이트 월드 크기 보정 + 텔포/대시 링 등 공통 이펙트.

public static class BossVfxUtility
{
    public static void SpawnRingBurst(Vector3 position, Color color, float diameter, float lifetime = 0.28f, int sortingOrder = 44)
    {
        GameObject go = new GameObject("Boss_RingBurst");
        go.transform.SetPositionAndRotation(position, Quaternion.identity);
        go.transform.localScale = RuntimeSpriteUtility.UniformWorldScale(RuntimeSpriteUtility.RingSprite, diameter);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteUtility.RingSprite;
        renderer.color = color;
        renderer.sortingLayerName = "Effect";
        renderer.sortingOrder = sortingOrder;
        if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
        {
            renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
        }

        BossEffectFade fade = go.AddComponent<BossEffectFade>();
        fade.Begin(lifetime, targetScaleMultiplier: 1.35f);
    }

    public static void SpawnFlashDisc(Vector3 position, Color color, float diameter, float lifetime = 0.14f, int sortingOrder = 43)
    {
        GameObject go = new GameObject("Boss_FlashDisc");
        go.transform.SetPositionAndRotation(position, Quaternion.identity);
        go.transform.localScale = RuntimeSpriteUtility.UniformWorldScale(RuntimeSpriteUtility.CircleSprite, diameter);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteUtility.CircleSprite;
        renderer.color = color;
        renderer.sortingLayerName = "Effect";
        renderer.sortingOrder = sortingOrder;
        if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
        {
            renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
        }

        BossEffectFade fade = go.AddComponent<BossEffectFade>();
        fade.Begin(lifetime, shrinkOverLifetime: true);
    }

    public static GameObject SpawnMotionStripe(Vector3 start, Vector3 end, Color color, float width, float lifetime = 0.22f, int sortingOrder = 39)
    {
        Vector3 delta = end - start;
        float length = delta.magnitude;
        if (length <= 0.01f)
        {
            return null;
        }

        GameObject go = new GameObject("Boss_MotionStripe");
        Vector3 center = (start + end) * 0.5f;
        go.transform.SetPositionAndRotation(center, Quaternion.identity);
        go.transform.localScale = RuntimeSpriteUtility.WorldSizeToLocalScale(RuntimeSpriteUtility.WhiteSprite, new Vector2(length, width));

        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteUtility.WhiteSprite;
        renderer.color = color;
        renderer.sortingLayerName = "Effect";
        renderer.sortingOrder = sortingOrder;
        if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
        {
            renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
        }

        BossEffectFade fade = go.AddComponent<BossEffectFade>();
        fade.Begin(lifetime, shrinkOverLifetime: false);
        return go;
    }
}
