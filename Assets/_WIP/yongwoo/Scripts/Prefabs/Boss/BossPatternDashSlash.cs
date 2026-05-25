using System.Collections.Generic;
using UnityEngine;

// P1-4 / P2-A / P3-A 대시 베기.
// 예고선 표시 → 선딜 → 현재 위치에서 조준 방향으로 돌진 타격.
// (이동=로직, 잔상/스트라이프=연출만. 텔레포 VFX는 사용하지 않음)

public class BossPatternDashSlash : BossPatternBase
{
    [Header("Dash Slash")]
    [SerializeField, Min(0.1f)] private float dashDistanceMultiplier = 2f;
    [SerializeField, Min(0.1f)] private float minDashDistance = 1.25f;
    [SerializeField, Min(0.1f)] private float maxDashDistance = 18f;
    [SerializeField, Min(0.01f)] private float dashDuration = 0.66f;
    [SerializeField, Min(0.1f)] private float hitRadius = 0.95f;
    [SerializeField, Min(0f)] private float damage = 1f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField, Min(0.01f)] private float telegraphLineWidth = 0.48f;
    [SerializeField, Range(0.05f, 1f)] private float telegraphAlpha = 0.38f;

    [Header("Fallback Visual")]
    [SerializeField] private Color slashColor = new Color(1f, 0.25f, 0.28f, 0.45f);
    [SerializeField] private Color afterimageColor = new Color(1f, 0.2f, 0.15f, 0.5f);
    [SerializeField, Min(0.01f)] private float afterimageInterval = 0.025f;
    [SerializeField, Min(0.01f)] private float afterimageLifetime = 0.22f;
    [SerializeField, Min(0.05f)] private float dashTrailWidth = 0.22f;

    private readonly HashSet<IDamageReceiver> _hitTargets = new();
    private readonly Collider2D[] _overlapResults = new Collider2D[12];

    private float _dashTimer;
    private float _afterimageTimer;
    private float _dashSpeed;
    private float _plannedDashDistance;
    private float _dashDistanceRemaining;
    private Vector3 _lastTrailPoint;
    private float _trailSegmentTimer;

    public override string PatternId => "Dash Slash";

    public override void BeginPattern(BossPatternContext context)
    {
        keepTelegraphDuringPrefire = true;
        base.BeginPattern(context);
    }

    private void Reset()
    {
        keepTelegraphDuringPrefire = true;
        telegraphDuration = 0.6f;
        prefireDelay = 0.14f;
    }

    protected override void OnTelegraphBegin()
    {
        EnsureDashTelegraphVisual();
    }

    protected override void UpdateTelegraphVisual(Vector2 aimDirection)
    {
        if (telegraphVisual == null)
        {
            return;
        }

        EnsureDashTelegraphVisual();

        Vector2 direction = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Vector2.right;
        _plannedDashDistance = ComputePlannedDashDistance(direction);
        SetTelegraphWorldSize(new Vector2(_plannedDashDistance, telegraphLineWidth));

        Transform visualTransform = telegraphVisual.transform;
        Vector3 origin = GetAimOriginPosition();
        visualTransform.position = origin + (Vector3)(direction * (_plannedDashDistance * 0.5f));
        visualTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }

    protected override void OnFireBegin(Vector2 aimDirection)
    {
        SetTelegraphVisible(false);
        _hitTargets.Clear();

        Vector2 aim = _lockedAim.sqrMagnitude > 0.0001f ? _lockedAim.normalized : Vector2.right;
        Vector3 origin = _ctx.boss != null ? _ctx.boss.position : transform.position;

        _dashDistanceRemaining = _plannedDashDistance;
        _dashSpeed = _dashDistanceRemaining / Mathf.Max(0.01f, dashDuration);
        _dashTimer = dashDuration;
        _afterimageTimer = 0f;
        _trailSegmentTimer = 0f;
        _lastTrailPoint = origin;

        SpawnDashLaunchVfx(origin, aim);
        SpawnAfterimage();
        SampleHit();
    }

    protected override void OnFireTick(float deltaTime)
    {
        if (_ctx.boss != null && _dashDistanceRemaining > 0f)
        {
            float stepDistance = Mathf.Min(_dashSpeed * deltaTime, _dashDistanceRemaining);
            Vector3 delta = (Vector3)(_lockedAim.normalized * stepDistance);
            Vector3 nextPosition = _ctx.boss.position + delta;
            if (_ctx.teleporter != null)
            {
                nextPosition = _ctx.teleporter.ClampToArena(nextPosition);
            }

            _dashDistanceRemaining -= Vector3.Distance(_ctx.boss.position, nextPosition);
            _ctx.boss.position = nextPosition;
            transform.position = _ctx.boss.position;
        }

        _afterimageTimer -= deltaTime;
        if (_afterimageTimer <= 0f)
        {
            SpawnAfterimage();
            _afterimageTimer = afterimageInterval;
        }

        _trailSegmentTimer -= deltaTime;
        if (_trailSegmentTimer <= 0f && _ctx.boss != null)
        {
            Vector3 current = _ctx.boss.position;
            if ((current - _lastTrailPoint).sqrMagnitude > 0.0001f)
            {
                BossVfxUtility.SpawnMotionStripe(_lastTrailPoint, current, slashColor, dashTrailWidth * 0.85f, 0.12f);
            }

            _lastTrailPoint = current;
            _trailSegmentTimer = afterimageInterval;
        }

        SampleHit();
        _dashTimer -= deltaTime;
    }

    protected override bool IsFireFinished() => _dashTimer <= 0f;

    protected override void OnFireEnd()
    {
        _hitTargets.Clear();
    }

    private float ComputePlannedDashDistance(Vector2 aimDirection)
    {
        Vector2 direction = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Vector2.right;
        Vector3 origin = GetAimOriginPosition();
        float distance = minDashDistance;

        if (_ctx.player != null)
        {
            float distanceToPlayer = GetDistanceToPlayerAlongAim(origin, direction);
            distance = Mathf.Max(minDashDistance, distanceToPlayer * dashDistanceMultiplier);
        }

        return Mathf.Clamp(distance, minDashDistance, maxDashDistance);
    }

    private float GetDistanceToPlayerAlongAim(Vector3 origin, Vector2 direction)
    {
        if (_ctx.player == null)
        {
            return minDashDistance;
        }

        Vector2 toPlayer = (Vector2)(_ctx.player.position - origin);
        return Mathf.Max(0f, Vector2.Dot(toPlayer, direction));
    }

    private void EnsureDashTelegraphVisual()
    {
        if (telegraphVisual == null)
        {
            return;
        }

        SpriteRenderer renderer = telegraphVisual.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            return;
        }

        if (renderer.sprite == null)
        {
            renderer.sprite = RuntimeSpriteUtility.WhiteSprite;
        }

        renderer.sortingLayerName = "Effect";
        renderer.sortingOrder = 44;
        if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
        {
            renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
        }

        Color color = renderer.color;
        color.r = 1f;
        color.g = 0.1f;
        color.b = 0.12f;
        color.a = telegraphAlpha;
        renderer.color = color;
        InvalidateTelegraphCache();
    }

    private void SampleHit()
    {
        Vector2 center = _ctx.boss != null ? _ctx.boss.position : transform.position;
        center += _lockedAim.normalized * 0.65f;

        int mask = targetLayers.value;
        int count = mask == 0
            ? Physics2D.OverlapCircleNonAlloc(center, hitRadius, _overlapResults)
            : Physics2D.OverlapCircleNonAlloc(center, hitRadius, _overlapResults, mask);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _overlapResults[i];
            if (hit == null)
            {
                continue;
            }

            if (_ctx.boss != null && hit.transform.IsChildOf(_ctx.boss))
            {
                continue;
            }

            PlayerInteraction receiver = ResolveDamageReceiver(hit);
            if (receiver == null || _hitTargets.Contains(receiver))
            {
                continue;
            }

            _hitTargets.Add(receiver);
            receiver.ReceiveHit(damage, Vector2.zero, _ctx.boss != null ? _ctx.boss.gameObject : gameObject);
        }
    }

    private static PlayerInteraction ResolveDamageReceiver(Collider2D hit)
    {
        return hit.GetComponentInParent<PlayerInteraction>();
    }

    private void SpawnDashLaunchVfx(Vector3 origin, Vector2 direction)
    {
        BossVfxUtility.SpawnFlashDisc(origin, new Color(slashColor.r, slashColor.g, slashColor.b, 0.35f), 0.9f);
        BossVfxUtility.SpawnMotionStripe(origin, origin + (Vector3)(direction * 0.35f), slashColor, dashTrailWidth * 0.6f, 0.1f);
    }

    private void SpawnAfterimage()
    {
        if (_ctx.boss == null)
        {
            return;
        }

        SpriteRenderer source = _ctx.boss.GetComponentInChildren<SpriteRenderer>();
        if (source == null || source.sprite == null)
        {
            return;
        }

        GameObject ghost = new GameObject("Boss_DashAfterimage");
        ghost.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        ghost.transform.localScale = source.transform.lossyScale;

        SpriteRenderer renderer = ghost.AddComponent<SpriteRenderer>();
        renderer.sprite = source.sprite;
        renderer.flipX = source.flipX;
        renderer.flipY = source.flipY;
        renderer.color = afterimageColor;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingLayerName = source.sortingLayerName;
        renderer.sortingOrder = source.sortingOrder - 1;
        renderer.sharedMaterial = source.sharedMaterial;

        BossEffectFade fade = ghost.AddComponent<BossEffectFade>();
        fade.Begin(afterimageLifetime, shrinkOverLifetime: false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = slashColor;
        Vector3 center = transform.position + (Vector3)(_lockedAim == Vector2.zero ? Vector2.right : _lockedAim) * 0.65f;
        Gizmos.DrawWireSphere(center, hitRadius);
    }
}
