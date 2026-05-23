using UnityEngine;

// P3-C3 안전지대 축소.
// 안전 원 안은 비우고, 아레나 나머지를 빨간 위험 영역으로 표시합니다.

public class BossPatternSafeZoneCollapse : BossPatternBase
{
    [Header("Safe Zone")]
    [SerializeField, Min(0.1f)] private float safeRadius = 2.2f;
    [SerializeField, Min(0f)] private float warningDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float activeDuration = 1.1f;
    [SerializeField, Min(0f)] private float damage = 1f;

    public override string PatternId => "Safe Zone Collapse";

    protected override void OnTelegraphBegin()
    {
        SetTelegraphVisible(false);
    }

    protected override void UpdateTelegraphVisual(Vector2 aimDirection)
    {
        // 위험 영역은 OnFireBegin에서 한 번에 생성합니다.
    }

    protected override void OnFireBegin(Vector2 aimDirection)
    {
        Vector3 safeCenter = _ctx.player != null ? _ctx.player.position : transform.position;
        if (_ctx.teleporter != null)
        {
            safeCenter = _ctx.teleporter.ClampToArena(safeCenter);
        }

        Bounds arenaBounds = ResolveArenaBounds();

        GameObject go = new GameObject("Boss_SafeZoneDanger");
        BossSafeZoneCollapse safeZone = go.AddComponent<BossSafeZoneCollapse>();
        safeZone.Arm(_ctx.player, _ctx.boss != null ? _ctx.boss.gameObject : gameObject, arenaBounds, safeCenter, safeRadius, warningDuration, activeDuration, damage);
    }

    protected override bool IsFireFinished() => true;

    private Bounds ResolveArenaBounds()
    {
        BossBattleArena arena = FindFirstObjectByType<BossBattleArena>();
        if (arena != null)
        {
            return arena.ArenaBounds;
        }

        return new Bounds(safeCenterFallback(), new Vector3(16f, 9f, 0f));
    }

    private Vector3 safeCenterFallback()
    {
        return _ctx.player != null ? _ctx.player.position : transform.position;
    }
}
