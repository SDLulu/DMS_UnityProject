using UnityEngine;

// P1-1 단발 직선탄.
// 표 기준: 텔레그래프 0.6s / 선딜 0.2s / 탄속 14u/s 1발 / 후딜 0.5s = 1.3s 사이클.
// 텔레그래프 = 보스→플레이어 빨간 조준선.

public class BossPatternStraightShot : BossPatternBase
{
    [Header("Shot")]
    [SerializeField] private BossProjectile projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 14f;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 3f;
    [SerializeField, Min(0f)] private float damage = 1f;

    public override string PatternId => "P1-1 StraightShot";

    protected override void OnFireBegin(Vector2 aimDirection)
    {
        if (projectilePrefab == null)
        {
            return;
        }

        Vector3 spawn = muzzle != null ? muzzle.position : (_ctx.boss != null ? _ctx.boss.position : transform.position);
        BossProjectile p = Instantiate(projectilePrefab, spawn, Quaternion.identity);
        p.Launch(aimDirection, projectileSpeed, projectileLifetime, damage, _ctx.boss != null ? _ctx.boss.gameObject : gameObject);
    }

    protected override bool IsFireFinished() => true;
}
