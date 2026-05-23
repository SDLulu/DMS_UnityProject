using UnityEngine;

// 역할:
// - 슬로우 키를 누르는 동안 Time.timeScale을 낮춰 세상을 느리게 합니다.
// - 슬로우는 자원(charge)으로 제한됩니다. 자원이 0이면 입력을 무시하고, 사용하지 않을 때 자동 회복합니다.
// - 플레이어 자신은 unscaledDeltaTime을 쓰는 다른 스크립트에서 정상 속도를 유지합니다.
// - 시네마틱 시퀀스가 시간 정지를 걸 때(PushExternalFreeze)는 timeScale 갱신과 자원 변화를 양보합니다.

[DisallowMultipleComponent]
public class PlayerSlowMotion : MonoBehaviour
{
    [Header("Slow Motion")]
    [Tooltip("슬로우 중 timeScale 값입니다. 낮을수록 느립니다.")]
    [SerializeField, Range(0.01f, 0.5f)] private float slowTimeScale = 0.15f;
    [Tooltip("timeScale이 변하는 속도입니다. 높을수록 즉시 전환됩니다.")]
    [SerializeField] private float transitionSpeed = 12f;

    [Header("Charges")]
    [Tooltip("슬로우 자원 최대 칸 수입니다.")]
    [SerializeField, Min(1)] private int maxCharges = 5;
    [Tooltip("1칸을 소비하는 데 걸리는 실시간 초입니다. (소비 속도)")]
    [SerializeField, Min(0.05f)] private float chargeConsumeSeconds = 1f;
    [Tooltip("1칸을 회복하는 데 걸리는 실시간 초입니다.")]
    [SerializeField, Min(0.05f)] private float chargeRegenSeconds = 4f;

    [Header("Debug")]
    [SerializeField] private bool isSlowMotionActive;
    [SerializeField] private float debugCurrentCharges;

    private float _targetTimeScale = 1f;
    private int _externalFreezeRefs;
    private float _chargesNormalized; // 0 ~ maxCharges 실수값

    public bool IsSlowMotionActive => isSlowMotionActive;
    public bool IsExternallyFrozen => _externalFreezeRefs > 0;

    // UI/디버그용 노출
    public int MaxCharges => maxCharges;
    public int CurrentCharges => Mathf.FloorToInt(_chargesNormalized);
    public float CurrentChargesRaw => _chargesNormalized;
    public float ChargeFillNormalized => maxCharges > 0 ? Mathf.Clamp01(_chargesNormalized / maxCharges) : 0f;
    public bool HasCharge => _chargesNormalized > 0f;

    private void Awake()
    {
        _chargesNormalized = maxCharges;
    }

    // 외부 시네마틱이 Time.timeScale을 직접 관리하는 동안 호출. 중첩 가능.
    public void PushExternalFreeze()
    {
        _externalFreezeRefs++;
    }

    public void PopExternalFreeze()
    {
        _externalFreezeRefs = Mathf.Max(0, _externalFreezeRefs - 1);
    }

    // 디자인 검증용: 보스전 진입 시 강제 풀충전 등 외부 호출 자리.
    public void RefillCharges()
    {
        _chargesNormalized = maxCharges;
    }

    private void Update()
    {
        if (PauseMenuController.IsPaused)
        {
            return;
        }

        if (IsExternallyFrozen)
        {
            // 시네마틱 freeze 중에는 timeScale과 자원을 건드리지 않는다.
            isSlowMotionActive = false;
            _targetTimeScale = Time.timeScale;
            debugCurrentCharges = _chargesNormalized;
            return;
        }

        bool slowInputHeld = GameInput.Instance.SlowMotionHeld;
        bool canSlow = slowInputHeld && _chargesNormalized > 0f;
        isSlowMotionActive = canSlow;

        if (canSlow)
        {
            _chargesNormalized = Mathf.Max(0f, _chargesNormalized - Time.unscaledDeltaTime / chargeConsumeSeconds);
        }
        else
        {
            _chargesNormalized = Mathf.Min(maxCharges, _chargesNormalized + Time.unscaledDeltaTime / chargeRegenSeconds);
        }

        _targetTimeScale = isSlowMotionActive ? slowTimeScale : 1f;

        Time.timeScale = Mathf.MoveTowards(Time.timeScale, _targetTimeScale, Time.unscaledDeltaTime * transitionSpeed);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        debugCurrentCharges = _chargesNormalized;
    }

    private void OnDisable()
    {
        _externalFreezeRefs = 0;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
