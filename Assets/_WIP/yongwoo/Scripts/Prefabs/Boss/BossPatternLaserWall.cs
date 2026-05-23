using UnityEngine;

// P3-C2 레이저 벽.
// 플레이어 위치를 기준으로 가로/세로 벽을 번갈아 깔아 이동 경로를 닫습니다.

public class BossPatternLaserWall : BossPatternBase
{
    [Header("Laser Wall")]
    [SerializeField, Min(0.1f)] private float wallLength = 18f;
    [SerializeField, Min(0.05f)] private float wallWidth = 1.52f;
    [SerializeField, Min(0f)] private float warningDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float activeDuration = 0.45f;
    [SerializeField, Min(0f)] private float damage = 1f;
    [SerializeField] private Color warningColor = new Color(0.45f, 0.85f, 1f, 0.22f);
    [SerializeField] private Color activeColor = new Color(0.45f, 0.85f, 1f, 0.65f);

    private bool _verticalNext = true;

    public override string PatternId => "Laser Wall";

    protected override void OnFireBegin(Vector2 aimDirection)
    {
        Vector3 playerPos = _ctx.player != null ? _ctx.player.position : transform.position;
        bool vertical = _verticalNext;
        _verticalNext = !_verticalNext;

        GameObject go = new GameObject(vertical ? "Boss_LaserWall_V" : "Boss_LaserWall_H");
        go.transform.position = playerPos;

        BossLaserWallZone zone = go.AddComponent<BossLaserWallZone>();
        Vector2 size = vertical
            ? new Vector2(wallWidth, wallLength)
            : new Vector2(wallLength, wallWidth);
        zone.Arm(_ctx.boss != null ? _ctx.boss.gameObject : gameObject, damage, size, warningDuration, activeDuration, warningColor, activeColor);
    }

    protected override bool IsFireFinished() => true;
}
