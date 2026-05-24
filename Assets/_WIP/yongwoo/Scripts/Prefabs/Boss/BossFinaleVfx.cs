using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 보스 페이즈 결합 / Hollow Knight식 처치 파티클.

public static class BossFinaleVfx
{
    public static IEnumerator PlayConvergence(
        MonoBehaviour host,
        IReadOnlyList<Vector3> sourcePositions,
        Vector3 center,
        float duration,
        Color primary,
        Color secondary)
    {
        if (host == null || sourcePositions == null || sourcePositions.Count == 0)
        {
            yield break;
        }

        duration = Mathf.Max(0.15f, duration);
        List<Transform> ghosts = new(sourcePositions.Count);

        for (int i = 0; i < sourcePositions.Count; i++)
        {
            Vector3 start = sourcePositions[i];
            GameObject ghost = new GameObject($"Boss_MergeGhost_{i:00}");
            ghost.transform.SetPositionAndRotation(start, Quaternion.identity);
            ghost.transform.localScale = Vector3.one * 0.95f;

            SpriteRenderer renderer = ghost.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteUtility.CircleSprite;
            renderer.sortingLayerName = "Effect";
            renderer.sortingOrder = 74 + i;
            if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
            {
                renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
            }

            Color color = i % 2 == 0 ? primary : secondary;
            color.a = 0.82f;
            renderer.color = color;
            ghosts.Add(ghost.transform);

            BossVfxUtility.SpawnRingBurst(start, color, 1.1f, 0.22f, 70 + i);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            for (int i = 0; i < ghosts.Count; i++)
            {
                Transform ghost = ghosts[i];
                if (ghost == null)
                {
                    continue;
                }

                Vector3 start = sourcePositions[i];
                ghost.position = Vector3.Lerp(start, center, t);
                ghost.localScale = Vector3.one * Mathf.Lerp(0.95f, 0.35f, t);

                if (ghost.TryGetComponent(out SpriteRenderer renderer))
                {
                    Color color = renderer.color;
                    color.a = Mathf.Lerp(0.82f, 0.18f, t);
                    renderer.color = color;
                }

                if (i == 0 && elapsed > 0f && Mathf.Repeat(elapsed, 0.05f) < Time.deltaTime)
                {
                    BossVfxUtility.SpawnMotionStripe(ghost.position, center, primary, 0.12f, 0.08f, 68);
                }
            }

            yield return null;
        }

        BossVfxUtility.SpawnFlashDisc(center, new Color(1f, 1f, 1f, 0.72f), 1.6f, 0.28f, 82);
        BossVfxUtility.SpawnRingBurst(center, primary, 2f, 0.42f, 83);

        for (int i = 0; i < ghosts.Count; i++)
        {
            if (ghosts[i] != null)
            {
                UnityEngine.Object.Destroy(ghosts[i].gameObject);
            }
        }
    }

    public static void SpawnHollowKnightSoulBurst(Vector3 center, Color primary, Color accent, int moteCount = 64)
    {
        moteCount = Mathf.Max(12, moteCount);
        const float rangeScale = 3f;
        const float sizeScale = 2f;
        const float lifetimeCenter = 3f;
        UnityEngine.Random.State randomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(Mathf.RoundToInt(Time.time * 1000f) ^ center.GetHashCode());

        for (int i = 0; i < moteCount; i++)
        {
            Vector2 velocity = SampleFountainVelocity(rangeScale, spread: 1f);

            GameObject mote = new GameObject("Boss_SoulMote");
            mote.transform.position = center + new Vector3(
                UnityEngine.Random.Range(-0.1f, 0.1f) * rangeScale,
                UnityEngine.Random.Range(-0.04f, 0.08f) * rangeScale,
                0f);

            BossSoulMote soul = mote.AddComponent<BossSoulMote>();
            Color color = UnityEngine.Random.value > 0.35f
                ? new Color(0.92f, 0.98f, 1f, UnityEngine.Random.Range(0.55f, 0.95f))
                : Color.Lerp(primary, accent, UnityEngine.Random.value);
            color.a = UnityEngine.Random.Range(0.45f, 0.92f);
            soul.Launch(
                velocity,
                UnityEngine.Random.Range(lifetimeCenter - 0.25f, lifetimeCenter + 0.25f),
                color,
                UnityEngine.Random.Range(0.04f, 0.14f) * sizeScale,
                75 + i % 20);
        }

        int orbCount = 6;
        for (int i = 0; i < orbCount; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            Vector2 velocity = SampleFountainVelocity(rangeScale, spread: 0.72f);
            velocity.x = side * Mathf.Abs(velocity.x) * UnityEngine.Random.Range(0.65f, 1.05f);
            velocity.y *= UnityEngine.Random.Range(1.05f, 1.25f);

            GameObject orb = new GameObject("Boss_SoulOrb");
            orb.transform.position = center + new Vector3(
                UnityEngine.Random.Range(-0.08f, 0.08f) * rangeScale,
                UnityEngine.Random.Range(-0.03f, 0.06f) * rangeScale,
                0f);

            BossSoulMote soul = orb.AddComponent<BossSoulMote>();
            Color orbColor = Color.Lerp(Color.white, accent, 0.35f);
            orbColor.a = UnityEngine.Random.Range(0.65f, 0.95f);
            soul.Launch(
                velocity,
                UnityEngine.Random.Range(lifetimeCenter - 0.15f, lifetimeCenter + 0.35f),
                orbColor,
                UnityEngine.Random.Range(0.16f, 0.28f) * sizeScale,
                88 + i);
        }

        for (int i = 0; i < 18; i++)
        {
            float side = UnityEngine.Random.value > 0.5f ? 1f : -1f;
            float fan = UnityEngine.Random.Range(18f, 62f) * side * Mathf.Deg2Rad;
            float reach = UnityEngine.Random.Range(1.4f, 3.6f) * rangeScale;
            Vector3 start = center + new Vector3(UnityEngine.Random.Range(-0.08f, 0.08f) * rangeScale, 0.02f * rangeScale, 0f);
            Vector3 end = start + new Vector3(Mathf.Sin(fan), Mathf.Cos(fan), 0f) * reach;
            Color streak = Color.Lerp(primary, Color.white, 0.55f);
            streak.a = UnityEngine.Random.Range(0.18f, 0.42f);
            BossVfxUtility.SpawnMotionStripe(
                start,
                end,
                streak,
                UnityEngine.Random.Range(0.03f, 0.07f) * sizeScale,
                UnityEngine.Random.Range(0.55f, 1.05f),
                72);
        }

        UnityEngine.Random.state = randomState;
    }

    private static Vector2 SampleFountainVelocity(float rangeScale, float spread)
    {
        float side = UnityEngine.Random.Range(-1f, 1f);
        if (Mathf.Abs(side) < 0.12f)
        {
            side = side >= 0f ? 0.35f : -0.35f;
        }

        float upward = UnityEngine.Random.Range(4.2f, 10.5f) * rangeScale;
        float outward = side * UnityEngine.Random.Range(2.8f, 7.5f) * rangeScale * spread;
        outward += UnityEngine.Random.Range(-0.35f, 0.35f) * rangeScale * spread;

        float angleJitter = UnityEngine.Random.Range(-12f, 12f) * Mathf.Deg2Rad;
        float magnitude = new Vector2(outward, upward).magnitude;
        float baseAngle = Mathf.Atan2(outward, upward) + angleJitter;
        return new Vector2(Mathf.Sin(baseAngle), Mathf.Cos(baseAngle)) * magnitude;
    }
}
