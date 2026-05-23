using UnityEngine;

// 역할:
// - 보스 패턴 4단계(텔레그래프 → 선딜 → 액티브 → 후딜) 공통 시간 흐름을 베이스로 제공합니다.
// - 구체 패턴(단발/연사/확산 등)은 OnFireBegin/OnFireTick/IsFireFinished만 채우면 됩니다.
// - 텔레그래프 중에는 플레이어를 계속 조준하고, 종료 시점 방향을 락해서 OnFireBegin에 전달합니다.

public abstract class BossPatternBase : MonoBehaviour, IBossPattern
{
    [Header("Timing")]
    [SerializeField, Min(0f)] protected float telegraphDuration = 0.6f;
    [SerializeField, Min(0f)] protected float prefireDelay = 0.2f;
    [SerializeField, Min(0f)] protected float recoveryDelay = 0.5f;

    [Header("Visuals (optional)")]
    [Tooltip("텔레그래프 동안 켜두는 GameObject. 보스→플레이어 방향선 등.")]
    [SerializeField] protected GameObject telegraphVisual;
    [SerializeField, Min(0f)] private float telegraphPulseSpeed = 18f;
    [SerializeField, Range(0f, 0.6f)] private float telegraphPulseAlpha = 0.22f;
    [SerializeField, Range(0f, 0.35f)] private float telegraphPulseWidth = 0.12f;

    protected enum Phase { Idle, Telegraph, Prefire, Active, Recovery }

    protected Phase _phase = Phase.Idle;
    protected float _phaseTimer;
    protected Vector2 _lockedAim = Vector2.right;
    protected BossPatternContext _ctx;
    protected bool _isActive;

    private SpriteRenderer _telegraphRenderer;
    private Color _telegraphBaseColor;
    private Vector3 _telegraphBaseScale;

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
                UpdateTelegraphVisual(ComputeAimDirection());
                ApplyTelegraphPulse();
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
                UpdateTelegraphVisual(ComputeAimDirection());
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
            CacheTelegraphVisual();
            if (!visible)
            {
                ResetTelegraphPulse();
            }
        }
    }

    protected virtual Vector2 ComputeAimDirection()
    {
        if (_ctx.player == null || _ctx.boss == null)
        {
            return Vector2.right;
        }

        Vector2 delta = _ctx.player.position - GetAimOriginPosition();
        return delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
    }

    protected virtual Vector3 GetAimOriginPosition()
    {
        return _ctx.boss != null ? _ctx.boss.position : transform.position;
    }

    protected virtual void UpdateTelegraphVisual(Vector2 aimDirection)
    {
        if (telegraphVisual == null)
        {
            return;
        }

        Vector2 direction = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Vector2.right;
        Transform visualTransform = telegraphVisual.transform;
        float length = Mathf.Max(0.1f, Mathf.Abs(visualTransform.localScale.x));
        Vector3 origin = GetAimOriginPosition();
        visualTransform.position = origin + (Vector3)(direction * (length * 0.5f));
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        visualTransform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void CacheTelegraphVisual()
    {
        if (telegraphVisual == null)
        {
            return;
        }

        SpriteRenderer renderer = telegraphVisual.GetComponent<SpriteRenderer>();
        if (_telegraphRenderer == renderer && _telegraphBaseScale != Vector3.zero)
        {
            return;
        }

        _telegraphRenderer = renderer;
        _telegraphBaseScale = telegraphVisual.transform.localScale;
        _telegraphBaseColor = _telegraphRenderer != null ? _telegraphRenderer.color : Color.white;
    }

    private void ApplyTelegraphPulse()
    {
        if (telegraphVisual == null)
        {
            return;
        }

        CacheTelegraphVisual();
        float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * telegraphPulseSpeed) * 0.5f;

        if (_telegraphRenderer != null)
        {
            Color color = _telegraphBaseColor;
            color.a = Mathf.Clamp01(_telegraphBaseColor.a + telegraphPulseAlpha * pulse);
            _telegraphRenderer.color = color;
        }

        Vector3 scale = _telegraphBaseScale;
        scale.y = _telegraphBaseScale.y * (1f + telegraphPulseWidth * pulse);
        telegraphVisual.transform.localScale = scale;
    }

    private void ResetTelegraphPulse()
    {
        if (telegraphVisual == null)
        {
            return;
        }

        if (_telegraphBaseScale != Vector3.zero)
        {
            telegraphVisual.transform.localScale = _telegraphBaseScale;
        }

        if (_telegraphRenderer != null)
        {
            _telegraphRenderer.color = _telegraphBaseColor;
        }
    }

    protected virtual void OnTelegraphBegin() { }
    protected abstract void OnFireBegin(Vector2 aimDirection);
    protected virtual void OnFireTick(float deltaTime) { }
    protected virtual bool IsFireFinished() => true;
    protected virtual void OnFireEnd() { }
}
