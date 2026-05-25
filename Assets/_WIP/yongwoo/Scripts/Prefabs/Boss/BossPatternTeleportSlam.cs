using System.Collections.Generic;
using UnityEngine;

// P2-A3 / P3-A3 순간이동 내려찍기.
// 플레이어 위치를 예고한 뒤 그 지점으로 순간이동해 원형 판정을 냅니다.

public class BossPatternTeleportSlam : BossPatternBase
{
    [Header("Teleport Slam")]
    [SerializeField, Min(0.1f)] private float hitRadius = 1.25f;
    [SerializeField, Min(0.01f)] private float activeDuration = 0.22f;
    [SerializeField, Min(0f)] private float damage = 1f;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.1f, 0.12f, 0.35f);

    private readonly HashSet<IDamageReceiver> _hitTargets = new();
    private readonly Collider2D[] _overlapResults = new Collider2D[12];
    private Vector3 _slamPosition;
    private float _activeTimer;

    public override string PatternId => "Teleport Slam";

    public override void BeginPattern(BossPatternContext context)
    {
        keepTelegraphDuringPrefire = true;
        prefireDelay = 0.15f;
        base.BeginPattern(context);
    }

    private void Reset()
    {
        keepTelegraphDuringPrefire = true;
        prefireDelay = 0.15f;
    }

    protected override void UpdateTelegraphVisual(Vector2 aimDirection)
    {
        if (telegraphVisual == null)
        {
            return;
        }

        Vector3 target = _ctx.player != null ? _ctx.player.position : transform.position;
        telegraphVisual.transform.position = target;
        telegraphVisual.transform.rotation = Quaternion.identity;

        SpriteRenderer renderer = telegraphVisual.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = RuntimeSpriteUtility.CircleSprite;
            if (RuntimeSpriteUtility.UnlitSpriteMaterial != null)
            {
                renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
            }
        }

        telegraphVisual.transform.localScale = RuntimeSpriteUtility.UniformWorldScale(
            RuntimeSpriteUtility.CircleSprite,
            hitRadius * 2f);
    }

    protected override void OnFireBegin(Vector2 aimDirection)
    {
        _hitTargets.Clear();
        _slamPosition = _ctx.player != null ? _ctx.player.position : transform.position;
        if (_ctx.teleporter != null)
        {
            _slamPosition = _ctx.teleporter.ClampToArena(_slamPosition);
        }

        if (_ctx.boss != null)
        {
            BossVfxUtility.SpawnRingBurst(_ctx.boss.position, new Color(0.45f, 0.85f, 1f, 0.55f), 1.4f);
            _ctx.boss.position = _slamPosition;
            transform.position = _slamPosition;
            BossVfxUtility.SpawnRingBurst(_slamPosition, new Color(1f, 0.15f, 0.12f, 0.75f), hitRadius * 2.2f);
            BossVfxUtility.SpawnFlashDisc(_slamPosition, new Color(1f, 0.2f, 0.15f, 0.45f), hitRadius * 1.6f);
        }

        _activeTimer = activeDuration;
        SampleHit();
    }

    protected override void OnFireTick(float deltaTime)
    {
        SampleHit();
        _activeTimer -= deltaTime;
    }

    protected override bool IsFireFinished() => _activeTimer <= 0f;

    protected override void OnFireEnd()
    {
        _hitTargets.Clear();
    }

    private void SampleHit()
    {
        int count = Physics2D.OverlapCircleNonAlloc(_slamPosition, hitRadius, _overlapResults);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _overlapResults[i];
            if (hit == null || (_ctx.boss != null && hit.transform.IsChildOf(_ctx.boss)))
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
