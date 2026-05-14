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
    [SerializeField] private Color backdropColor = Color.black;

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
        TryAutoBind();

        if (panelRoot != null)
        {
            EnsureHierarchyActive(panelRoot);
            panelRoot.gameObject.SetActive(true);
        }

        if (logText != null)
        {
            logText.text = message ?? string.Empty;
        }

        if (backdropImage != null)
        {
            backdropImage.color = backdropColor;
        }
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
            backdropImage.color = backdropColor;
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
