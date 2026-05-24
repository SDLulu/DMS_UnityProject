using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 역할:
// - 게임플레이 화면 위에 뜨는 통신 UI입니다.
// - 브로커, 시스템 오퍼레이터 같은 조력자/감시자 대사를 짧게 표시합니다.

[DisallowMultipleComponent]
public class CommsPanel : MonoBehaviour
{
    [Header("Scene Layout")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Sprite brokerPortraitSprite;
    [SerializeField] private Sprite merchantPortraitSprite;
    [SerializeField] private Sprite robotGuidePortraitSprite;
    [SerializeField] private Sprite passerbyPortraitSprite;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text bodyText;

    [Header("Typing")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField, Min(1f)] private float charactersPerSecond = 36f;
    [SerializeField, Range(0.1f, 1f)] private float maxTypingDurationRatio = 0.65f;
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

    public void ShowLine(string speaker, string body)
    {
        ShowLine(speaker, body, 0f);
    }

    public void ShowLine(string speaker, string body, float maxDuration)
    {
        TryAutoBind();

        if (panelRoot != null)
        {
            EnsureHierarchyActive(panelRoot);
            panelRoot.gameObject.SetActive(true);
        }

        if (panelGroup != null)
        {
            panelGroup.alpha = 1f;
        }

        if (speakerText != null)
        {
            speakerText.gameObject.SetActive(true);
            speakerText.text = string.IsNullOrWhiteSpace(speaker) ? string.Empty : speaker;
        }

        if (bodyText != null)
        {
            StartTypewriter(body ?? string.Empty, maxDuration);
        }

        UpdatePortrait(speaker);
        YongwooAudioManager.Play(YongwooSfxId.CommsIn, 0.55f, 0.02f);
    }

    public void Hide()
    {
        TryAutoBind();
        bool wasVisible = panelRoot != null && panelRoot.gameObject.activeInHierarchy;

        if (speakerText != null)
        {
            speakerText.text = string.Empty;
            speakerText.gameObject.SetActive(true);
        }

        if (bodyText != null)
        {
            bodyText.text = string.Empty;
        }

        StopTypewriter();

        if (panelGroup != null)
        {
            panelGroup.alpha = 1f;
        }

        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(false);
        }

        if (wasVisible && Application.isPlaying)
        {
            YongwooAudioManager.Play(YongwooSfxId.CommsOut, 0.4f, 0.02f);
        }
    }

    private void TryAutoBind()
    {
        if (panelRoot == null)
        {
            panelRoot = FindDescendantByName(transform, "CommsRoot") as RectTransform;
        }

        if (panelGroup == null && panelRoot != null)
        {
            panelGroup = panelRoot.GetComponent<CanvasGroup>();
            if (panelGroup == null)
            {
                panelGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (speakerText == null)
        {
            speakerText = FindDescendantByName(transform, "SpeakerText")?.GetComponent<Text>();
        }

        if (bodyText == null)
        {
            bodyText = FindDescendantByName(transform, "BodyText")?.GetComponent<Text>();
        }

        if (portraitImage == null)
        {
            portraitImage = FindDescendantByName(transform, "PortraitImage")?.GetComponent<Image>();
        }
    }

    private void UpdatePortrait(string speaker)
    {
        if (portraitImage == null)
        {
            return;
        }

        Sprite portrait = ResolvePortrait(speaker);
        portraitImage.sprite = portrait;
        portraitImage.gameObject.SetActive(portrait != null);
    }

    private Sprite ResolvePortrait(string speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker))
        {
            return null;
        }

        if (speaker.Contains("주인공") || speaker.Contains("Player"))
        {
            return null;
        }

        if (speaker.Contains("브로커") || speaker.Contains("Broker"))
        {
            return brokerPortraitSprite;
        }

        if (speaker.Contains("노점상") || speaker.Contains("상인") || speaker.Contains("Merchant"))
        {
            return merchantPortraitSprite;
        }

        if (speaker.Contains("행인") || speaker.Contains("Passerby"))
        {
            return passerbyPortraitSprite;
        }

        if (speaker.Contains("AI") || speaker.Contains("로봇") || speaker.Contains("Robot"))
        {
            return robotGuidePortraitSprite;
        }

        return null;
    }

    private void StartTypewriter(string message, float maxDuration)
    {
        StopTypewriter();

        if (bodyText == null)
        {
            return;
        }

        if (!useTypewriter || maxDuration <= 0f || string.IsNullOrEmpty(message))
        {
            bodyText.text = message;
            return;
        }

        _typingRoutine = StartCoroutine(TypewriterRoutine(message, maxDuration));
    }

    private IEnumerator TypewriterRoutine(string message, float maxDuration)
    {
        bodyText.text = string.Empty;

        int characterCount = message.Length;
        float naturalDuration = characterCount / Mathf.Max(1f, charactersPerSecond);
        float typingDuration = Mathf.Min(naturalDuration, maxDuration * maxTypingDurationRatio);
        float interval = characterCount > 0 ? typingDuration / characterCount : 0f;

        for (int i = 0; i < characterCount; i++)
        {
            bodyText.text = message.Substring(0, i + 1);
            if (ShouldPlayTypingSound(message[i], i))
            {
                YongwooAudioManager.Play(YongwooSfxId.TypingTick, 0.28f, 0.04f);
            }

            if (interval > 0f)
            {
                yield return new WaitForSecondsRealtime(interval);
            }
        }

        bodyText.text = message;
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
