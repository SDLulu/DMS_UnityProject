using System.Collections;
using UnityEngine;

// 역할:
// - 실제 타격이 들어간 순간의 공통 손맛 연출을 담당합니다.
// - 히트스톱은 unscaled time 기준이라 슬로우 모션 중에도 짧고 선명하게 들어갑니다.

public static class CombatHitFeedback
{
    private const float DefaultHitStopDuration = 0.045f;
    private const float DefaultHitStopScale = 0.04f;
    private const float DefaultShakeDuration = 0.12f;
    private const float DefaultShakeStrength = 0.13f;

    private static Runner _runner;

    public static void PlayLightHit()
    {
        EnsureRunner().Play(DefaultHitStopDuration, DefaultHitStopScale, DefaultShakeDuration, DefaultShakeStrength);
        SimpleCameraFollow cameraFollow = Camera.main != null ? Camera.main.GetComponent<SimpleCameraFollow>() : null;
        cameraFollow?.AddShake(DefaultShakeStrength, DefaultShakeDuration);
    }

    private static Runner EnsureRunner()
    {
        if (_runner != null)
        {
            return _runner;
        }

        GameObject runnerObject = new GameObject("CombatHitFeedback");
        Object.DontDestroyOnLoad(runnerObject);
        _runner = runnerObject.AddComponent<Runner>();
        return _runner;
    }

    private sealed class Runner : MonoBehaviour
    {
        private Coroutine _hitStopRoutine;
        private float _hitStopEndTime;
        private float _hitStopScale = 1f;

        public void Play(float hitStopDuration, float hitStopScale, float shakeDuration, float shakeStrength)
        {
            _hitStopEndTime = Mathf.Max(_hitStopEndTime, Time.unscaledTime + Mathf.Max(0f, hitStopDuration));
            _hitStopScale = Mathf.Min(_hitStopScale, Mathf.Clamp(hitStopScale, 0.01f, 1f));

            if (_hitStopRoutine == null)
            {
                _hitStopRoutine = StartCoroutine(HitStopRoutine());
            }
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < _hitStopEndTime)
            {
                Time.timeScale = Mathf.Min(Time.timeScale, _hitStopScale);
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
            }
        }

        private IEnumerator HitStopRoutine()
        {
            float previousTimeScale = Time.timeScale;
            while (Time.unscaledTime < _hitStopEndTime)
            {
                yield return null;
            }

            Time.timeScale = Mathf.Max(previousTimeScale, 1f);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            _hitStopScale = 1f;
            _hitStopRoutine = null;
        }
    }
}
