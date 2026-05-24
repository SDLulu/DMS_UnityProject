using UnityEngine;

// P3-C2 레이저 벽.
// 텔레그래프 동안 아레나 가로/세로 전체를 덮는 벽을 예고하고, 선딜 후 판정이 활성화됩니다.

public class BossPatternLaserWall : BossPatternBase
{
    [Header("Laser Wall")]
    [SerializeField, Min(0.05f)] private float wallWidth = 1.52f;
    [SerializeField, Min(0f)] private float wallSpanPadding = 0.35f;
    [SerializeField, Min(0f)] private float postTelegraphWarning = 0.12f;
    [SerializeField, Min(0.01f)] private float activeDuration = 0.45f;
    [SerializeField, Min(0f)] private float damage = 1f;
    [SerializeField] private Color warningColor = new Color(0.45f, 0.85f, 1f, 0.28f);
    [SerializeField] private Color activeColor = new Color(0.45f, 0.85f, 1f, 0.65f);

    private bool _verticalNext = true;
    private bool _vertical;
    private Vector3 _wallCenter;
    private Vector2 _wallSize;
    private GameObject _previewWall;
    private SpriteRenderer _previewRenderer;

    public override string PatternId => "Laser Wall";

    public override void BeginPattern(BossPatternContext context)
    {
        keepTelegraphDuringPrefire = true;
        prefireDelay = 0.15f;
        base.BeginPattern(context);
    }

    private void Reset()
    {
        keepTelegraphDuringPrefire = true;
        telegraphDuration = 0.75f;
        prefireDelay = 0.15f;
    }

    protected override void OnTelegraphBegin()
    {
        _vertical = _verticalNext;
        _verticalNext = !_verticalNext;
        ResolveWallPlacement(out _wallCenter, out _wallSize);
        BuildPreviewWall();
    }

    protected override void UpdateTelegraphVisual(Vector2 aimDirection)
    {
        UpdatePreviewPulse();
    }

    protected override void OnFireBegin(Vector2 aimDirection)
    {
        DestroyPreviewWall();
        SpawnLaserWall();
    }

    protected override void OnFireEnd()
    {
        DestroyPreviewWall();
    }

    public override void EndPattern()
    {
        DestroyPreviewWall();
        base.EndPattern();
    }

    protected override bool IsFireFinished() => true;

    private void ResolveWallPlacement(out Vector3 center, out Vector2 size)
    {
        Bounds arena = ResolveArenaBounds();
        Vector3 playerPos = _ctx.player != null ? _ctx.player.position : transform.position;

        float spanX = arena.size.x + wallSpanPadding;
        float spanY = arena.size.y + wallSpanPadding;

        if (_vertical)
        {
            center = new Vector3(playerPos.x, arena.center.y, playerPos.z);
            size = new Vector2(wallWidth, spanY);
        }
        else
        {
            center = new Vector3(arena.center.x, playerPos.y, playerPos.z);
            size = new Vector2(spanX, wallWidth);
        }

        if (_ctx.teleporter != null)
        {
            center = _ctx.teleporter.ClampToArena(center);
        }
    }

    private Bounds ResolveArenaBounds()
    {
        BossBattleArena arena = FindFirstObjectByType<BossBattleArena>();
        if (arena != null)
        {
            return arena.ArenaBounds;
        }

        Camera cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            return new Bounds(cam.transform.position, new Vector3(halfWidth * 2f, halfHeight * 2f, 0f));
        }

        return new Bounds(transform.position, new Vector3(20f, 12f, 0f));
    }

    private void BuildPreviewWall()
    {
        DestroyPreviewWall();

        _previewWall = new GameObject(_vertical ? "Boss_LaserWallPreview_V" : "Boss_LaserWallPreview_H");
        _previewWall.transform.position = _wallCenter;

        _previewRenderer = _previewWall.AddComponent<SpriteRenderer>();
        _previewRenderer.sprite = RuntimeSpriteUtility.WhiteSprite;
        _previewRenderer.color = warningColor;
        _previewRenderer.sortingLayerName = "Effect";
        _previewRenderer.sortingOrder = 40;
        if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
        {
            _previewRenderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
        }

        _previewWall.transform.localScale = RuntimeSpriteUtility.WorldSizeToLocalScale(RuntimeSpriteUtility.WhiteSprite, _wallSize);
    }

    private void UpdatePreviewPulse()
    {
        if (_previewRenderer == null)
        {
            return;
        }

        float pulse = 0.5f + Mathf.Sin(Time.time * 20f) * 0.5f;
        Color color = warningColor;
        color.a = Mathf.Lerp(warningColor.a * 0.55f, warningColor.a, pulse);
        _previewRenderer.color = color;
    }

    private void SpawnLaserWall()
    {
        GameObject go = new GameObject(_vertical ? "Boss_LaserWall_V" : "Boss_LaserWall_H");
        go.transform.position = _wallCenter;

        BossLaserWallZone zone = go.AddComponent<BossLaserWallZone>();
        zone.Arm(
            _ctx.boss != null ? _ctx.boss.gameObject : gameObject,
            damage,
            _wallSize,
            postTelegraphWarning,
            activeDuration,
            warningColor,
            activeColor);
    }

    private void DestroyPreviewWall()
    {
        if (_previewWall != null)
        {
            Destroy(_previewWall);
            _previewWall = null;
            _previewRenderer = null;
        }
    }
}
