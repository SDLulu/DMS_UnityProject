using UnityEngine;

// 역할:
// - 슬로우 키를 누르는 동안 Time.timeScale을 낮춰 세상을 느리게 합니다.
// - 플레이어 자신은 unscaledDeltaTime을 쓰는 다른 스크립트에서 정상 속도를 유지합니다.
// - 시네마틱 시퀀스가 시간 정지를 걸 때(PushExternalFreeze)는 timeScale 갱신을 양보합니다.

[DisallowMultipleComponent]
public class PlayerSlowMotion : MonoBehaviour
{
    [Header("Slow Motion")]
    [Tooltip("슬로우 중 timeScale 값입니다. 낮을수록 느립니다.")]
    [SerializeField, Range(0.01f, 0.5f)] private float slowTimeScale = 0.15f;
    [Tooltip("timeScale이 변하는 속도입니다. 높을수록 즉시 전환됩니다.")]
    [SerializeField] private float transitionSpeed = 12f;

    [Header("Debug")]
    [SerializeField] private bool isSlowMotionActive;

    private float _targetTimeScale = 1f;
    private int _externalFreezeRefs;

    public bool IsSlowMotionActive => isSlowMotionActive;
    public bool IsExternallyFrozen => _externalFreezeRefs > 0;

    // 외부 시네마틱이 Time.timeScale을 직접 관리하는 동안 호출. 중첩 가능.
    public void PushExternalFreeze()
    {
        _externalFreezeRefs++;
    }

    public void PopExternalFreeze()
    {
        _externalFreezeRefs = Mathf.Max(0, _externalFreezeRefs - 1);
    }

    private void Update()
    {
        if (PauseMenuController.IsPaused)
        {
            return;
        }

        if (IsExternallyFrozen)
        {
            // 시네마틱 freeze 중에는 timeScale을 건드리지 않는다.
            isSlowMotionActive = false;
            _targetTimeScale = Time.timeScale;
            return;
        }

        isSlowMotionActive = GameInput.Instance.SlowMotionHeld;
        _targetTimeScale = isSlowMotionActive ? slowTimeScale : 1f;

        Time.timeScale = Mathf.MoveTowards(Time.timeScale, _targetTimeScale, Time.unscaledDeltaTime * transitionSpeed);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private void OnDisable()
    {
        _externalFreezeRefs = 0;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
