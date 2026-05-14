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
    [SerializeField] private Text speakerText;
    [SerializeField] private Text bodyText;

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
            speakerText.text = string.IsNullOrWhiteSpace(speaker) ? "???" : speaker;
        }

        if (bodyText != null)
        {
            bodyText.text = body ?? string.Empty;
        }
    }

    public void Hide()
    {
        TryAutoBind();

        if (speakerText != null)
        {
            speakerText.text = string.Empty;
        }

        if (bodyText != null)
        {
            bodyText.text = string.Empty;
        }

        if (panelGroup != null)
        {
            panelGroup.alpha = 1f;
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
