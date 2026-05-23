using UnityEngine;

// P1-5 / P2-B / P3-C 지연 폭발 장판.
// 플레이어 현재 위치와 이동 방향 주변에 예고 표식을 놓고, 지연 후 원형 폭발 판정을 냅니다.

public class BossPatternDelayedBlast : BossPatternBase
{
    [Header("Blast")]
    [SerializeField] private BossBlastZone blastPrefab;
    [SerializeField, Min(1)] private int blastCount = 2;
    [SerializeField, Min(0.1f)] private float warningDuration = 1f;
    [SerializeField, Min(0.01f)] private float activeDuration = 0.28f;
    [SerializeField, Min(0.1f)] private float blastRadius = 1.4f;
    [SerializeField, Min(0f)] private float damage = 1f;
    [SerializeField, Min(0f)] private float sideOffset = 2.4f;
    [SerializeField] private Color warningColor = new Color(1f, 0.78f, 0.25f, 0.22f);
    [SerializeField] private Color activeColor = new Color(1f, 0.78f, 0.25f, 0.55f);

    public override string PatternId => "Delayed Blast";

    protected override void UpdateTelegraphVisual(Vector2 aimDirection)
    {
        if (telegraphVisual == null)
        {
            return;
        }

        Vector3 playerPos = _ctx.player != null ? _ctx.player.position : transform.position;
        telegraphVisual.transform.position = playerPos;
        telegraphVisual.transform.rotation = Quaternion.identity;
    }

    protected override void OnFireBegin(Vector2 aimDirection)
    {
        Vector3[] positions = BuildBlastPositions();
        GameObject owner = _ctx.boss != null ? _ctx.boss.gameObject : gameObject;

        for (int i = 0; i < positions.Length; i++)
        {
            BossBlastZone zone = CreateZone(positions[i]);
            zone.Arm(owner, damage, blastRadius, warningDuration, activeDuration, warningColor, activeColor);
        }
    }

    protected override bool IsFireFinished() => true;

    private Vector3[] BuildBlastPositions()
    {
        int count = Mathf.Max(1, blastCount);
        Vector3[] positions = new Vector3[count];
        Vector3 playerPos = _ctx.player != null ? _ctx.player.position : transform.position;
        float floorY = playerPos.y;

        float sign = 1f;
        if (_ctx.player != null && _ctx.player.TryGetComponent(out Rigidbody2D body) && Mathf.Abs(body.linearVelocity.x) > 0.1f)
        {
            sign = Mathf.Sign(body.linearVelocity.x);
        }

        positions[0] = new Vector3(playerPos.x, floorY, 0f);
        if (count >= 2)
        {
            positions[1] = new Vector3(playerPos.x + sign * sideOffset, floorY, 0f);
        }
        if (count >= 3)
        {
            positions[2] = new Vector3(playerPos.x - sign * sideOffset, floorY, 0f);
        }
        for (int i = 3; i < count; i++)
        {
            float offset = sideOffset * (i - 1) * (i % 2 == 0 ? 1f : -1f);
            positions[i] = new Vector3(playerPos.x + offset, floorY, 0f);
        }

        return positions;
    }

    private BossBlastZone CreateZone(Vector3 position)
    {
        if (blastPrefab != null)
        {
            return Instantiate(blastPrefab, position, Quaternion.identity);
        }

        GameObject go = new GameObject("Boss_BlastZone");
        go.transform.position = position;
        return go.AddComponent<BossBlastZone>();
    }
}
