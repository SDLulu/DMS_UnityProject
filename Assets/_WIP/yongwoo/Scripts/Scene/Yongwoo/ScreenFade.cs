using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 역할:
// - 풀스크린 페이드 인/아웃 전환을 제공합니다.
// - 구역 이동, 접속/복귀 연출처럼 화면 암전이 필요한 곳에서 사용합니다.

[DisallowMultipleComponent]
public class ScreenFade : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private Graphic fadeGraphic;

    private Color _defaultFadeColor = Color.black;

    private void Awake()
    {
        if (fadeGroup == null)
        {
            fadeGroup = GetComponent<CanvasGroup>();
        }

        if (fadeGraphic == null)
        {
            fadeGraphic = GetComponent<Graphic>();
        }

        if (fadeGraphic == null)
        {
            fadeGraphic = GetComponentInChildren<Graphic>(includeInactive: true);
        }

        if (fadeGraphic != null)
        {
            _defaultFadeColor = fadeGraphic.color;
        }

        if (fadeGroup != null)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
        }
    }

    public IEnumerator FadeOut(float duration)
    {
        YongwooAudioManager.Play(YongwooSfxId.FadeOut, 0.48f, 0.01f);
        return Fade(1f, duration);
    }

    public IEnumerator FadeIn(float duration)
    {
        YongwooAudioManager.Play(YongwooSfxId.FadeIn, 0.42f, 0.01f);
        return Fade(0f, duration);
    }

    public IEnumerator Flash(Color color, float fadeInDuration, float holdDuration, float fadeOutDuration)
    {
        if (fadeGroup == null)
        {
            yield break;
        }

        Color previousColor = _defaultFadeColor;
        if (fadeGraphic != null)
        {
            previousColor = fadeGraphic.color;
            fadeGraphic.color = color;
        }

        YongwooAudioManager.Play(YongwooSfxId.GlitchPulse, 0.42f, 0.02f);
        yield return Fade(1f, fadeInDuration);

        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }

        yield return Fade(0f, fadeOutDuration);

        if (fadeGraphic != null)
        {
            fadeGraphic.color = previousColor;
        }
    }

    private IEnumerator Fade(float targetAlpha, float duration)
    {
        if (fadeGroup == null)
        {
            yield break;
        }

        fadeGroup.blocksRaycasts = targetAlpha > 0.5f;
        float startAlpha = fadeGroup.alpha;
        duration = Mathf.Max(0f, duration);

        if (duration <= 0f)
        {
            fadeGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        fadeGroup.alpha = targetAlpha;
    }
}
