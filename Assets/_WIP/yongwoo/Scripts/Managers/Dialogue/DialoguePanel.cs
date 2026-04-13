using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 역할:
// - 화면에 대사 줄, 화자명, 초상, 힌트를 출력하는 뷰 계층입니다.
// - DialogueManager가 넘긴 데이터를 scene-authored UI에 반영합니다.

[DisallowMultipleComponent]
public class DialoguePanel : MonoBehaviour
{
    [Header("Scene Layout")]
    [Tooltip("대사 전체를 보여주고 숨길 루트입니다. 씬에 배치한 DialogueRoot를 직접 연결하는 것이 기준입니다.")]
    [SerializeField] private RectTransform panelRoot;
    [Tooltip("왼쪽 초상 이미지입니다.")]
    [SerializeField] private Image leftPortrait;
    [Tooltip("오른쪽 초상 이미지입니다.")]
    [SerializeField] private Image rightPortrait;
    [Tooltip("화자 이름 텍스트입니다.")]
    [SerializeField] private Text nameText;
    [Tooltip("본문 텍스트입니다.")]
    [SerializeField] private Text bodyText;
    [Tooltip("진행/스킵 힌트 텍스트입니다.")]
    [SerializeField] private Text hintText;

    [Header("Typing")]
    [Tooltip("초당 몇 글자씩 타이핑할지 정합니다.")]
    [SerializeField] private float charactersPerSecond = 42f;

    private readonly List<DialogueLineData> _lines = new();

    private Action _onComplete;
    private string _currentText = string.Empty;
    private int _currentLineIndex;
    private int _visibleCharacterCount;
    private float _typingProgress;
    private bool _isPlaying;
    private bool _hasLoggedMissingUiWarning;

    public bool IsPlaying => _isPlaying;

    private void Reset()
    {
        TryAutoBindSceneReferences();
    }

    private void Awake()
    {
        NormalizeCanvasRootLayout();
        TryAutoBindSceneReferences();
        ValidateSceneLayout();
        SetPanelVisible(false);
    }

    private void OnEnable()
    {
        NormalizeCanvasRootLayout();
        if (!_isPlaying)
        {
            SetPanelVisible(false);
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        TryAutoBindSceneReferences();
    }

    private void Update()
    {
        if (!_isPlaying || !HasValidBindings())
        {
            return;
        }

        if (ReadSkipPressed())
        {
            SkipAll();
            return;
        }

        if (_visibleCharacterCount < _currentText.Length)
        {
            _typingProgress += Time.unscaledDeltaTime * Mathf.Max(1f, charactersPerSecond);
            int nextCharacterCount = Mathf.Min(_currentText.Length, Mathf.FloorToInt(_typingProgress));
            if (nextCharacterCount != _visibleCharacterCount)
            {
                _visibleCharacterCount = nextCharacterCount;
                bodyText.text = _currentText.Substring(0, _visibleCharacterCount);
            }
        }

        if (!ReadAdvancePressed())
        {
            return;
        }

        if (_visibleCharacterCount < _currentText.Length)
        {
            RevealLineInstantly();
        }
        else
        {
            AdvanceLine();
        }
    }

    public void Play(IList<DialogueLineData> lines, Action onComplete)
    {
        ValidateSceneLayout();
        _onComplete = onComplete;
        _lines.Clear();

        if (lines != null)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] != null)
                {
                    _lines.Add(lines[i]);
                }
            }
        }

        if (_lines.Count == 0 || !HasValidBindings())
        {
            FinishDialogue();
            return;
        }

        _isPlaying = true;
        _currentLineIndex = 0;
        SetPanelVisible(true);
        PresentLine(_currentLineIndex);
    }

    public void SkipAll()
    {
        if (!_isPlaying)
        {
            return;
        }

        FinishDialogue();
    }

    private void AdvanceLine()
    {
        _currentLineIndex++;
        if (_currentLineIndex >= _lines.Count)
        {
            FinishDialogue();
            return;
        }

        PresentLine(_currentLineIndex);
    }

    private void PresentLine(int lineIndex)
    {
        DialogueLineData line = _lines[lineIndex];
        ApplyLineVisuals(line);
        _currentText = line.text ?? string.Empty;
        _typingProgress = 0f;
        _visibleCharacterCount = 0;
        bodyText.text = string.Empty;
        hintText.text = "Space/Enter: 다음   Tab/Esc: 전체 스킵";
    }

    private void RevealLineInstantly()
    {
        _visibleCharacterCount = _currentText.Length;
        _typingProgress = _visibleCharacterCount;
        bodyText.text = _currentText;
    }

    private void FinishDialogue()
    {
        _isPlaying = false;
        SetPanelVisible(false);
        Action callback = _onComplete;
        _onComplete = null;
        callback?.Invoke();
    }

    private void ValidateSceneLayout()
    {
        TryAutoBindSceneReferences();
        if (HasValidBindings() || _hasLoggedMissingUiWarning)
        {
            return;
        }

        _hasLoggedMissingUiWarning = true;
        Debug.LogWarning(
            $"{nameof(DialoguePanel)} on {name} could not find a usable scene-authored dialogue UI. " +
            "Place a DialogueRoot hierarchy under this object and bind its children.",
            this);
    }

    private void TryAutoBindSceneReferences()
    {
        Transform root = FindDescendantByName(transform, "DialogueRoot");
        panelRoot ??= root as RectTransform;
        nameText ??= FindDescendantByName(root, "Name")?.GetComponent<Text>();
        bodyText ??= FindDescendantByName(root, "BodyText")?.GetComponent<Text>();
        hintText ??= FindDescendantByName(root, "HintText")?.GetComponent<Text>();
        leftPortrait ??= FindDescendantByName(FindDescendantByName(root, "LeftPortrait"), "Portrait")?.GetComponent<Image>();
        rightPortrait ??= FindDescendantByName(FindDescendantByName(root, "RightPortrait"), "Portrait")?.GetComponent<Image>();
    }

    private bool HasValidBindings()
    {
        return ResolvePanelRoot() != null
            && leftPortrait != null
            && rightPortrait != null
            && nameText != null
            && bodyText != null
            && hintText != null;
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
            Transform nested = FindDescendantByName(root.GetChild(i), targetName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static void ApplyPortrait(Image portrait, Sprite sprite)
    {
        if (portrait == null)
        {
            return;
        }

        portrait.sprite = sprite;
        if (portrait.transform.parent != null)
        {
            portrait.transform.parent.gameObject.SetActive(sprite != null);
        }
        else
        {
            portrait.gameObject.SetActive(sprite != null);
        }
    }

    private void ApplyLineVisuals(DialogueLineData line)
    {
        nameText.text = string.IsNullOrWhiteSpace(line.speakerName) ? "???" : line.speakerName;

        bool useLeftPortrait = line.portraitSide == DialoguePortraitSide.Left;
        ApplyPortrait(leftPortrait, useLeftPortrait ? line.portraitSprite : null);
        ApplyPortrait(rightPortrait, useLeftPortrait ? null : line.portraitSprite);
    }

    private void SetPanelVisible(bool visible)
    {
        RectTransform resolvedPanelRoot = ResolvePanelRoot();
        if (resolvedPanelRoot != null)
        {
            if (visible)
            {
                EnsureHierarchyActive(resolvedPanelRoot);
            }

            resolvedPanelRoot.gameObject.SetActive(visible);
        }
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

    private static bool ReadAdvancePressed()
    {
        return GameInput.Instance.DialogueAdvancePressed;
    }

    private static bool ReadSkipPressed()
    {
        return GameInput.Instance.DialogueSkipPressed;
    }

    private void NormalizeCanvasRootLayout()
    {
        if (transform is not RectTransform rectTransform)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private RectTransform ResolvePanelRoot()
    {
        if (panelRoot != null)
        {
            return panelRoot;
        }

        Transform found = FindAncestorByName(nameText != null ? nameText.transform : null, "DialogueRoot")
            ?? FindDescendantByName(transform, "DialogueRoot");
        panelRoot = found as RectTransform;
        return panelRoot;
    }

    private static Transform FindAncestorByName(Transform start, string targetName)
    {
        Transform current = start;
        while (current != null)
        {
            if (current.name == targetName)
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }
}
