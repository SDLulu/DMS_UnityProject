using System.Collections;
using UnityEngine;

// 역할:
// - 풀스크린 페이드 인/아웃 전환을 제공합니다.
// - 구역 이동, 접속/복귀 연출처럼 화면 암전이 필요한 곳에서 사용합니다.

[DisallowMultipleComponent]
public class ScreenFade : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeGroup;

    private void Awake()
    {
        if (fadeGroup == null)
        {
            fadeGroup = GetComponent<CanvasGroup>();
        }

        if (fadeGroup != null)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
        }
    }

    public IEnumerator FadeOut(float duration)
    {
        return Fade(1f, duration);
    }

    public IEnumerator FadeIn(float duration)
    {
        return Fade(0f, duration);
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
