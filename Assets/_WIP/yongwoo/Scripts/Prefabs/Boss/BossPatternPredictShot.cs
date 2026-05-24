using UnityEngine;

// P1-6 / P2-B / P3-B 예측 3점 사격.
// 현재 위치, 이동 방향 앞, 반대쪽을 순차로 쏴서 단순 한 방향 회피를 흔듭니다.

public class BossPatternPredictShot : BossPatternBase
{
    [Header("Predict Shot")]
    [SerializeField] private BossProjectile projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField, Min(1)] private int shotCount = 3;
    [SerializeField, Min(0.01f)] private float interShotInterval = 0.12f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 12f;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 3f;
    [SerializeField, Min(0f)] private float damage = 1f;
    [SerializeField, Min(0f)] private float leadDistance = 2.2f;
    [SerializeField, Min(0f)] private float backDistance = 1.7f;

    private readonly Vector2[] _directions = new Vector2[3];
    private int _shotsFired;
    private float _nextShotTimer;

    public override string PatternId => "Predict 3 Shot";

    protected override Vector3 GetAimOriginPosition()
    {
        return muzzle != null ? muzzle.position : base.GetAimOriginPosition();
    }

    protected override void OnFireBegin(Vector2 aimDirection)
    {
        BuildDirections();
        _shotsFired = 0;
        _nextShotTimer = 0f;
        FireOne();
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
            FireOne();
        }
    }

    protected override bool IsFireFinished() => _shotsFired >= shotCount;

    private void BuildDirections()
    {
        Vector3 origin = GetAimOriginPosition();
        Vector3 bossPos = _ctx.boss != null ? _ctx.boss.position : transform.position;
        Vector3 playerPos = _ctx.player != null ? _ctx.player.position : origin + Vector3.right;

        Vector2 velocity = Vector2.zero;
        if (_ctx.player != null && _ctx.player.TryGetComponent(out Rigidbody2D body))
        {
            velocity = body.linearVelocity;
        }

        float sign = Mathf.Abs(velocity.x) > 0.1f ? Mathf.Sign(velocity.x) : Mathf.Sign(playerPos.x - bossPos.x);
        if (Mathf.Approximately(sign, 0f))
        {
            sign = 1f;
        }

        Vector3 lead = playerPos + Vector3.right * sign * leadDistance;
        Vector3 back = playerPos - Vector3.right * sign * backDistance;

        _directions[0] = DirectionTo(origin, playerPos);
        _directions[1] = DirectionTo(origin, lead);
        _directions[2] = DirectionTo(origin, back);
    }

    private static Vector2 DirectionTo(Vector3 from, Vector3 to)
    {
        Vector2 delta = to - from;
        return delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
    }

    private void FireOne()
    {
        if (projectilePrefab == null)
        {
            _shotsFired = shotCount;
            return;
        }

        int index = Mathf.Clamp(_shotsFired, 0, _directions.Length - 1);
        Vector3 spawn = muzzle != null ? muzzle.position : (_ctx.boss != null ? _ctx.boss.position : transform.position);
        BossProjectile projectile = Instantiate(projectilePrefab, spawn, Quaternion.identity);
        projectile.Launch(_directions[index], projectileSpeed, projectileLifetime, damage, _ctx.boss != null ? _ctx.boss.gameObject : gameObject);
        _shotsFired++;
        _nextShotTimer = interShotInterval;
    }
}
