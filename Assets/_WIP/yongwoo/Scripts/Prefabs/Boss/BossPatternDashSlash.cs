using System.Collections.Generic;
using UnityEngine;

// P1-4 / P2-A / P3-A 대시 베기.
// HTML 튜닝 기준: P1 0.32s x 100u/s, P2/P3 0.30s x 100u/s.
// 보스가 락온 방향으로 빠르게 돌진하면서 반경 판정을 직접 샘플링합니다.

public class BossPatternDashSlash : BossPatternBase
{
    [Header("Dash Slash")]
    [SerializeField, Min(0.01f)] private float dashDuration = 0.32f;
    [SerializeField, Min(0.1f)] private float dashSpeed = 100f;
    [SerializeField, Min(0.1f)] private float hitRadius = 0.95f;
    [SerializeField, Min(0f)] private float damage = 1f;
    [SerializeField] private LayerMask targetLayers;

    [Header("Fallback Visual")]
    [SerializeField] private Color slashColor = new Color(1f, 0.25f, 0.28f, 0.28f);
    [SerializeField] private Color afterimageColor = new Color(1f, 0.2f, 0.15f, 0.32f);
    [SerializeField, Min(0.01f)] private float afterimageInterval = 0.035f;
    [SerializeField, Min(0.01f)] private float afterimageLifetime = 0.16f;

    private readonly HashSet<IDamageReceiver> _hitTargets = new();
    private readonly Collider2D[] _overlapResults = new Collider2D[12];

    private float _dashTimer;
    private float _afterimageTimer;

    public override string PatternId => "Dash Slash";

    protected override void OnFireBegin(Vector2 aimDirection)
    {
        _hitTargets.Clear();
        _dashTimer = dashDuration;
        _afterimageTimer = 0f;
        SpawnAfterimage();
        SampleHit();
    }

    protected override void OnFireTick(float deltaTime)
    {
        if (_ctx.boss != null)
        {
            Vector3 delta = (Vector3)(_lockedAim * dashSpeed * deltaTime);
            Vector3 nextPosition = _ctx.boss.position + delta;
            if (_ctx.teleporter != null)
            {
                nextPosition = _ctx.teleporter.ClampToArena(nextPosition);
            }

            _ctx.boss.position = nextPosition;
            transform.position = _ctx.boss.position;
        }

        _afterimageTimer -= deltaTime;
        if (_afterimageTimer <= 0f)
        {
            SpawnAfterimage();
            _afterimageTimer = afterimageInterval;
        }

        SampleHit();
        _dashTimer -= deltaTime;
    }

    protected override bool IsFireFinished() => _dashTimer <= 0f;

    protected override void OnFireEnd()
    {
        _hitTargets.Clear();
    }

    private void SampleHit()
    {
        Vector2 center = _ctx.boss != null ? _ctx.boss.position : transform.position;
        center += _lockedAim * 0.65f;

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

            IDamageReceiver receiver = ResolveDamageReceiver(hit);
            if (receiver == null || _hitTargets.Contains(receiver))
            {
                continue;
            }

            _hitTargets.Add(receiver);
            receiver.ReceiveHit(damage, Vector2.zero, _ctx.boss != null ? _ctx.boss.gameObject : gameObject);
        }
    }

    private static IDamageReceiver ResolveDamageReceiver(Collider2D hit)
    {
        MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IDamageReceiver receiver)
            {
                return receiver;
            }
        }

        return null;
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
