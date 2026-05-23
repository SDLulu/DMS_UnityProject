using UnityEngine;

// P1-2 연사.
// 표 기준: 텔레그래프 0.7s / 선딜 0.1s / 0.15s 간격 4발 탄속 12u/s 동일 직선 / 후딜 0.6s = 1.4s 사이클.
// 텔레그래프 = 노란 발사 자세.

public class BossPatternVolley : BossPatternBase
{
    [Header("Volley")]
    [SerializeField] private BossProjectile projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField, Min(1)] private int shotCount = 4;
    [SerializeField, Min(0.01f)] private float interShotInterval = 0.15f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 12f;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 3f;
    [SerializeField, Min(0f)] private float damage = 1f;

    private int _shotsFired;
    private float _nextShotTimer;

    public override string PatternId => "P1-2 Volley";

    protected override void OnFireBegin(Vector2 aimDirection)
    {
        _shotsFired = 0;
        _nextShotTimer = 0f;
        FireOne(aimDirection);
    }

    protected override void OnFireTick(float deltaTime)
    {
        if (_shotsFired >= shotCount)
        {
            return;
        }

        _nextShotTimer -= deltaTime;
        if (_nextShotTimer <= 0f)
        {
            FireOne(_lockedAim);
        }
    }

    protected override bool IsFireFinished() => _shotsFired >= shotCount;

    private void FireOne(Vector2 aimDirection)
    {
        if (projectilePrefab == null)
        {
            _shotsFired = shotCount;
            return;
        }

        Vector3 spawn = muzzle != null ? muzzle.position : (_ctx.boss != null ? _ctx.boss.position : transform.position);
        BossProjectile p = Instantiate(projectilePrefab, spawn, Quaternion.identity);
        p.Launch(aimDirection, projectileSpeed, projectileLifetime, damage, _ctx.boss != null ? _ctx.boss.gameObject : gameObject);
        _shotsFired++;
        _nextShotTimer = interShotInterval;
    }
}
