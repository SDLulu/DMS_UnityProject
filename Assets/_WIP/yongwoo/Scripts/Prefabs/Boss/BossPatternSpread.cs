using UnityEngine;

// P1-3 확산 5way.
// 표 기준: 텔레그래프 0.9s / 선딜 0.15s / 5발 동시 각도 30° 탄속 10u/s / 후딜 0.7s = 1.75s 사이클.
// 텔레그래프 = 부채꼴 5방향 가이드라인.

public class BossPatternSpread : BossPatternBase
{
    [Header("Spread")]
    [SerializeField] private BossProjectile projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField, Min(1)] private int shotCount = 5;
    [Tooltip("바깥 두 발 사이의 총 각도(도). 5발이면 가운데+양쪽 두 발씩.")]
    [SerializeField, Min(0f)] private float totalSpreadDegrees = 120f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 10f;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 3f;
    [SerializeField, Min(0f)] private float damage = 1f;

    public override string PatternId => "P1-3 Spread";

    protected override Vector3 GetAimOriginPosition()
    {
        return muzzle != null ? muzzle.position : base.GetAimOriginPosition();
    }

    protected override void OnFireBegin(Vector2 aimDirection)
    {
        if (projectilePrefab == null || shotCount <= 0)
        {
            return;
        }

        Vector3 spawn = muzzle != null ? muzzle.position : (_ctx.boss != null ? _ctx.boss.position : transform.position);
        GameObject owner = _ctx.boss != null ? _ctx.boss.gameObject : gameObject;

        if (shotCount == 1)
        {
            SpawnOne(spawn, aimDirection, owner);
            return;
        }

        float startDeg = -totalSpreadDegrees * 0.5f;
        float stepDeg = totalSpreadDegrees / (shotCount - 1);
        float baseAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        for (int i = 0; i < shotCount; i++)
        {
            float angleDeg = baseAngle + startDeg + stepDeg * i;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
            SpawnOne(spawn, dir, owner);
        }
    }

    protected override bool IsFireFinished() => true;

    private void SpawnOne(Vector3 spawn, Vector2 dir, GameObject owner)
    {
        BossProjectile p = Instantiate(projectilePrefab, spawn, Quaternion.identity);
        p.Launch(dir, projectileSpeed, projectileLifetime, damage, owner);
    }
}
