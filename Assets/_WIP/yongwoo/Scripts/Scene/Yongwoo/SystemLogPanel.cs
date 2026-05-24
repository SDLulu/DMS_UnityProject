using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 역할:
// - 대화창과 별도로 짧은 시스템 로그를 화면 중앙에 표시합니다.
// - 튜토리얼 시작, 접속/복귀, 목표 안내처럼 "대사"가 아닌 텍스트에 사용합니다.

[DisallowMultipleComponent]
public class SystemLogPanel : MonoBehaviour
{
    [Header("Scene Layout")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private Image backdropImage;
    [SerializeField] private Text logText;

    [Header("Visual")]
    [SerializeField] private bool useBackdrop;
    [SerializeField] private Color backdropColor = Color.black;

    [Header("Typing")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField, Min(1f)] private float charactersPerSecond = 42f;
    [SerializeField, Range(0.1f, 1f)] private float maxTypingDurationRatio = 0.55f;
    [SerializeField, Min(1)] private int typingSoundEveryCharacters = 2;

    private Coroutine _typingRoutine;

    private void Reset()
    {
        TryAutoBind();
    }

    private void Awake()
    {
        TryAutoBind();
        Hide();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        TryAutoBind();
    }

    public void Show(string message)
    {
        Show(message, 0f);
    }

    public void Show(string message, float maxDuration)
    {
        TryAutoBind();

        if (panelRoot != null)
        {
            EnsureHierarchyActive(panelRoot);
            panelRoot.gameObject.SetActive(true);
        }

        // 이전 FadeTo(0)로 알파가 0이 됐을 수 있으니, 새 메시지가 보이도록 알파를 복원
        if (panelGroup != null)
        {
            panelGroup.alpha = 1f;
        }

        if (logText != null)
        {
            StartTypewriter(message ?? string.Empty, maxDuration);
        }

        if (backdropImage != null)
        {
            ApplyBackdrop();
        }

        YongwooAudioManager.Play(YongwooSfxId.SystemLogIn, 0.58f, 0.02f);
    }

    public void SetAlpha(float alpha)
    {
        TryAutoBind();

        if (panelRoot != null)
        {
            EnsureHierarchyActive(panelRoot);
            panelRoot.gameObject.SetActive(true);
        }

        if (panelGroup != null)
        {
            panelGroup.alpha = Mathf.Clamp01(alpha);
        }

        if (backdropImage != null)
        {
            ApplyBackdrop();
        }
    }

    public System.Collections.IEnumerator FadeTo(float targetAlpha, float duration)
    {
        TryAutoBind();

        if (panelRoot != null)
        {
            EnsureHierarchyActive(panelRoot);
            panelRoot.gameObject.SetActive(true);
        }

        if (panelGroup == null)
        {
            yield break;
        }

        float startAlpha = panelGroup.alpha;
        targetAlpha = Mathf.Clamp01(targetAlpha);
        duration = Mathf.Max(0f, duration);

        if (duration <= 0f)
        {
            panelGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            panelGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        panelGroup.alpha = targetAlpha;
    }

    public void Hide()
    {
        TryAutoBind();

        if (logText != null)
        {
            logText.text = string.Empty;
        }

        StopTypewriter();

        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(false);
        }
    }

    private void TryAutoBind()
    {
        if (panelRoot == null)
        {
            panelRoot = FindDescendantByName(transform, "SystemLogRoot") as RectTransform;
        }

        if (panelGroup == null && panelRoot != null)
        {
            panelGroup = panelRoot.GetComponent<CanvasGroup>();
            if (panelGroup == null)
            {
                panelGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (backdropImage == null)
        {
            backdropImage = FindDescendantByName(transform, "Backdrop")?.GetComponent<Image>();
        }

        if (logText == null)
        {
            logText = FindDescendantByName(transform, "LogText")?.GetComponent<Text>();
        }

        ApplyBackdrop();
    }

    private void ApplyBackdrop()
    {
        if (backdropImage == null)
        {
            return;
        }

        backdropImage.gameObject.SetActive(useBackdrop);
        backdropImage.color = useBackdrop ? backdropColor : Color.clear;
    }

    private void StartTypewriter(string message, float maxDuration)
    {
        StopTypewriter();

        if (logText == null)
        {
            return;
        }

        if (!useTypewriter || maxDuration <= 0f || string.IsNullOrEmpty(message))
        {
            logText.text = message;
            return;
        }

        _typingRoutine = StartCoroutine(TypewriterRoutine(message, maxDuration));
    }

    private IEnumerator TypewriterRoutine(string message, float maxDuration)
    {
        logText.text = string.Empty;

        int characterCount = message.Length;
        float naturalDuration = characterCount / Mathf.Max(1f, charactersPerSecond);
        float typingDuration = Mathf.Min(naturalDuration, maxDuration * maxTypingDurationRatio);
        float interval = characterCount > 0 ? typingDuration / characterCount : 0f;

        for (int i = 0; i < characterCount; i++)
        {
            logText.text = message.Substring(0, i + 1);
            if (ShouldPlayTypingSound(message[i], i))
            {
                YongwooAudioManager.Play(YongwooSfxId.TypingTick, 0.34f, 0.04f);
            }

            if (interval > 0f)
            {
                yield return new WaitForSecondsRealtime(interval);
            }
        }

        logText.text = message;
        _typingRoutine = null;
    }

    private bool ShouldPlayTypingSound(char character, int index)
    {
        return !char.IsWhiteSpace(character)
            && typingSoundEveryCharacters > 0
            && index % typingSoundEveryCharacters == 0;
    }

    private void StopTypewriter()
    {
        if (_typingRoutine == null)
        {
            return;
        }

        StopCoroutine(_typingRoutine);
        _typingRoutine = null;
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

    private static void EnsureHierarchyActive(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }

            current = current.parent;
        }
    }
}
