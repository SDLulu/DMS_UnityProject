using UnityEngine;

// 역할:
// - 보스 패턴 4단계(텔레그래프 → 선딜 → 액티브 → 후딜) 공통 시간 흐름을 베이스로 제공합니다.
// - 구체 패턴(단발/연사/확산 등)은 OnFireBegin/OnFireTick/IsFireFinished만 채우면 됩니다.
// - 텔레그래프 종료 시점에 플레이어 방향을 락해서 OnFireBegin에 전달합니다.

public abstract class BossPatternBase : MonoBehaviour, IBossPattern
{
    [Header("Timing")]
    [SerializeField, Min(0f)] protected float telegraphDuration = 0.6f;
    [SerializeField, Min(0f)] protected float prefireDelay = 0.2f;
    [SerializeField, Min(0f)] protected float recoveryDelay = 0.5f;

    [Header("Visuals (optional)")]
    [Tooltip("텔레그래프 동안 켜두는 GameObject. 보스→플레이어 방향선 등.")]
    [SerializeField] protected GameObject telegraphVisual;

    protected enum Phase { Idle, Telegraph, Prefire, Active, Recovery }

    protected Phase _phase = Phase.Idle;
    protected float _phaseTimer;
    protected Vector2 _lockedAim = Vector2.right;
    protected BossPatternContext _ctx;
    protected bool _isActive;

    public bool IsActive => _isActive;
    public abstract string PatternId { get; }

    public virtual void BeginPattern(BossPatternContext context)
    {
        _ctx = context;
        _isActive = true;
        EnterPhase(Phase.Telegraph);
    }

    public virtual void TickPattern(float deltaTime)
    {
        if (!_isActive)
        {
            return;
        }

        _phaseTimer -= deltaTime;

        switch (_phase)
        {
            case Phase.Telegraph:
                if (_phaseTimer <= 0f)
                {
                    _lockedAim = ComputeAimDirection();
                    EnterPhase(Phase.Prefire);
                }
                break;

            case Phase.Prefire:
                if (_phaseTimer <= 0f)
                {
                    EnterPhase(Phase.Active);
                    OnFireBegin(_lockedAim);
                }
                break;

            case Phase.Active:
                OnFireTick(deltaTime);
                if (IsFireFinished())
                {
                    EnterPhase(Phase.Recovery);
                }
                break;

            case Phase.Recovery:
                if (_phaseTimer <= 0f)
                {
                    EndPattern();
                }
                break;
        }
    }

    public virtual void EndPattern()
    {
        SetTelegraphVisible(false);
        _phase = Phase.Idle;
        _isActive = false;
    }

    protected virtual void EnterPhase(Phase next)
    {
        _phase = next;
        switch (next)
        {
            case Phase.Telegraph:
                _phaseTimer = telegraphDuration;
                SetTelegraphVisible(true);
                OnTelegraphBegin();
                break;
            case Phase.Prefire:
                _phaseTimer = prefireDelay;
                SetTelegraphVisible(false);
                break;
            case Phase.Active:
                _phaseTimer = 0f;
                break;
            case Phase.Recovery:
                _phaseTimer = recoveryDelay;
                OnFireEnd();
                break;
        }
    }

    protected void SetTelegraphVisible(bool visible)
    {
        if (telegraphVisual != null)
        {
            telegraphVisual.SetActive(visible);
        }
    }

    protected virtual Vector2 ComputeAimDirection()
    {
        if (_ctx.player == null || _ctx.boss == null)
        {
            return Vector2.right;
        }

        Vector2 delta = _ctx.player.position - _ctx.boss.position;
        return delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
    }

    protected virtual void OnTelegraphBegin() { }
    protected abstract void OnFireBegin(Vector2 aimDirection);
    protected virtual void OnFireTick(float deltaTime) { }
    protected virtual bool IsFireFinished() => true;
    protected virtual void OnFireEnd() { }
}
