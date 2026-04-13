using UnityEngine;

// 역할:
// - 슬로우 키를 누르는 동안 Time.timeScale을 낮춰 세상을 느리게 합니다.
// - 플레이어 자신은 unscaledDeltaTime을 쓰는 다른 스크립트에서 정상 속도를 유지합니다.

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

    public bool IsSlowMotionActive => isSlowMotionActive;

    private void Update()
    {
        isSlowMotionActive = GameInput.Instance.SlowMotionHeld;
        _targetTimeScale = isSlowMotionActive ? slowTimeScale : 1f;

        Time.timeScale = Mathf.MoveTowards(Time.timeScale, _targetTimeScale, Time.unscaledDeltaTime * transitionSpeed);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
