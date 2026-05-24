using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 역할:
// - 화면 전체 글리치 연출을 UI 오버레이로 제공합니다.
// - 실제 이미지/배치는 씬에서 교체하고, 시퀀스는 강도와 시간만 제어합니다.

[DisallowMultipleComponent]
public class ScreenGlitchOverlay : MonoBehaviour
{
    [Header("Scene Layout")]
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private RectTransform jitterRoot;
    [SerializeField] private Image noiseImage;
    [SerializeField] private Image[] glitchBars;

    [Header("Visual")]
    [SerializeField, Range(0f, 1f)] private float intensity;
    [SerializeField] private float jitterPixels = 18f;
    [SerializeField] private float barFlickerChance = 0.45f;

    private Coroutine _fadeRoutine;
    private Coroutine _pulseRoutine;
    private Vector2 _baseAnchoredPosition;

    private void Reset()
    {
        TryAutoBind();
    }

    private void Awake()
    {
        TryAutoBind();

        if (jitterRoot != null)
        {
            _baseAnchoredPosition = jitterRoot.anchoredPosition;
        }

        SetIntensity(0f);
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        TryAutoBind();
        ApplyVisual(intensity);
    }

    private void Update()
    {
        ApplyVisual(intensity);
    }

    public IEnumerator PlayTransitionCover(float fadeIn, float hold, float fadeOut, float peakIntensity = 1f)
    {
        StopPulse();
        StopFade();
        peakIntensity = Mathf.Clamp01(peakIntensity);
        fadeIn = Mathf.Max(0f, fadeIn);
        hold = Mathf.Max(0f, hold);
        fadeOut = Mathf.Max(0f, fadeOut);

        if (fadeIn > 0f)
        {
            yield return FadeRoutine(peakIntensity, fadeIn);
        }
        else
        {
            SetIntensity(peakIntensity);
        }

        if (hold > 0f)
        {
            yield return new WaitForSecondsRealtime(hold);
        }

        if (fadeOut > 0f)
        {
            yield return FadeRoutine(0f, fadeOut);
        }
        else
        {
            SetIntensity(0f);
        }
    }

    public IEnumerator Pulse(float targetIntensity, float duration)
    {
        StopFade();
        StopPulse();
        YongwooAudioManager.Play(YongwooSfxId.GlitchPulse, Mathf.Lerp(0.28f, 0.68f, Mathf.Clamp01(targetIntensity)), 0.04f);
        _pulseRoutine = StartCoroutine(PulseRoutine(targetIntensity, duration));
        yield return _pulseRoutine;
    }

    public IEnumerator FadeTo(float targetIntensity, float duration)
    {
        StopPulse();
        StopFade();
        _fadeRoutine = StartCoroutine(FadeRoutine(targetIntensity, duration));
        yield return _fadeRoutine;
    }

    public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp01(value);
        ApplyVisual(intensity);
    }

    public void ResetGlitch()
    {
        StopPulse();
        StopFade();
        SetIntensity(0f);
    }

    private IEnumerator PulseRoutine(float targetIntensity, float duration)
    {
        float previous = intensity;
        SetIntensity(targetIntensity);

        duration = Mathf.Max(0f, duration);
        if (duration > 0f)
        {
            yield return new WaitForSecondsRealtime(duration);
        }

        SetIntensity(previous);
        _pulseRoutine = null;
    }

    private IEnumerator FadeRoutine(float targetIntensity, float duration)
    {
        float start = intensity;
        targetIntensity = Mathf.Clamp01(targetIntensity);
        duration = Mathf.Max(0f, duration);

        if (duration <= 0f)
        {
            SetIntensity(targetIntensity);
            _fadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetIntensity(Mathf.Lerp(start, targetIntensity, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetIntensity(targetIntensity);
        _fadeRoutine = null;
    }

    private void StopFade()
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }
    }

    private void StopPulse()
    {
        if (_pulseRoutine != null)
        {
            StopCoroutine(_pulseRoutine);
            _pulseRoutine = null;
        }
    }

    private void ApplyVisual(float value)
    {
        value = Mathf.Clamp01(value);

        if (overlayGroup != null)
        {
            overlayGroup.alpha = value;
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;
        }

        if (jitterRoot != null)
        {
            Vector2 offset = value <= 0f
                ? Vector2.zero
                : Random.insideUnitCircle * jitterPixels * value;
            jitterRoot.anchoredPosition = _baseAnchoredPosition + offset;
        }

        if (noiseImage != null)
        {
            Color color = noiseImage.color;
            color.a = value;
            noiseImage.color = color;
        }

        if (glitchBars == null)
        {
            return;
        }

        for (int i = 0; i < glitchBars.Length; i++)
        {
            Image bar = glitchBars[i];
            if (bar == null)
            {
                continue;
            }

            bool visible = value > 0f && Random.value < barFlickerChance * value;
            bar.enabled = visible;
            Color color = bar.color;
            color.a = visible ? value : 0f;
            bar.color = color;
        }
    }

    private void TryAutoBind()
    {
        if (overlayGroup == null)
        {
            overlayGroup = GetComponent<CanvasGroup>();
            if (overlayGroup == null)
            {
                overlayGroup = GetComponentInChildren<CanvasGroup>(true);
            }

            if (overlayGroup == null)
            {
                overlayGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (jitterRoot == null)
        {
            jitterRoot = transform as RectTransform;
        }

        if (noiseImage == null)
        {
            noiseImage = FindDescendantByName(transform, "Noise")?.GetComponent<Image>();
        }
    }

    private static Transform FindDescendantByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendantByName(root.GetChild(i), targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
