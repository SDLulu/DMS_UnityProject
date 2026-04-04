using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[Serializable]
public enum EncounterPortraitSide
{
    Left,
    Right
}

[Serializable]
public class EncounterDialogueLine
{
    public string speakerName;
    [TextArea(2, 5)] public string text;
    public Sprite portraitSprite;
    public EncounterPortraitSide portraitSide = EncounterPortraitSide.Left;
}

[DisallowMultipleComponent]
public class EncounterDialoguePanel : MonoBehaviour
{
    [SerializeField] private float charactersPerSecond = 42f;

    private readonly List<EncounterDialogueLine> _lines = new();

    private RectTransform _panelRoot;
    private Image _leftPortrait;
    private Image _rightPortrait;
    private Text _nameText;
    private Text _bodyText;
    private Text _hintText;
    private Action _onComplete;
    private string _currentText = string.Empty;
    private int _currentLineIndex;
    private int _visibleCharacterCount;
    private float _typingProgress;
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;

    private void Awake()
    {
        BuildUi();
        SetPanelVisible(false);
    }

    private void Update()
    {
        if (!_isPlaying)
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
                _bodyText.text = _currentText.Substring(0, _visibleCharacterCount);
            }
        }

        if (ReadAdvancePressed())
        {
            if (_visibleCharacterCount < _currentText.Length)
            {
                RevealLineInstantly();
            }
            else
            {
                AdvanceLine();
            }
        }
    }

    public void Play(IList<EncounterDialogueLine> lines, Action onComplete)
    {
        BuildUi();
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

        if (_lines.Count == 0)
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
        EncounterDialogueLine line = _lines[lineIndex];
        _nameText.text = string.IsNullOrWhiteSpace(line.speakerName) ? "???" : line.speakerName;
        _currentText = line.text ?? string.Empty;
        _typingProgress = 0f;
        _visibleCharacterCount = 0;
        _bodyText.text = string.Empty;

        bool useLeftPortrait = line.portraitSide == EncounterPortraitSide.Left;
        ApplyPortrait(_leftPortrait, useLeftPortrait ? line.portraitSprite : null);
        ApplyPortrait(_rightPortrait, useLeftPortrait ? null : line.portraitSprite);
        _hintText.text = "Space/Enter: Next   Tab: Skip";
    }

    private void RevealLineInstantly()
    {
        _visibleCharacterCount = _currentText.Length;
        _typingProgress = _visibleCharacterCount;
        _bodyText.text = _currentText;
    }

    private void FinishDialogue()
    {
        _isPlaying = false;
        SetPanelVisible(false);
        Action callback = _onComplete;
        _onComplete = null;
        callback?.Invoke();
    }

    private void BuildUi()
    {
        if (_panelRoot != null)
        {
            return;
        }

        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1200;

        if (gameObject.GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect != null)
        {
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
        }

        _panelRoot = CreateRect("DialogueRoot", transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 32f), new Vector2(1520f, 260f));
        Image panelBackground = _panelRoot.gameObject.AddComponent<Image>();
        panelBackground.color = new Color(0.06f, 0.08f, 0.12f, 0.94f);

        RectTransform namePlate = CreateRect("NamePlate", _panelRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -20f), new Vector2(260f, 42f));
        Image nameBackground = namePlate.gameObject.AddComponent<Image>();
        nameBackground.color = new Color(0.21f, 0.15f, 0.2f, 0.98f);

        _nameText = CreateText("Name", namePlate, font, 22, TextAnchor.MiddleCenter);
        _nameText.color = new Color(1f, 0.92f, 0.92f, 1f);

        RectTransform bodyRegion = CreateRect("Body", _panelRoot, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(250f, 0f), new Vector2(-500f, -88f));
        _bodyText = CreateText("BodyText", bodyRegion, font, 28, TextAnchor.UpperLeft);
        _bodyText.color = new Color(0.95f, 0.97f, 1f, 1f);
        _bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        _bodyText.lineSpacing = 1.15f;

        RectTransform hintRegion = CreateRect("Hint", _panelRoot, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-26f, 16f), new Vector2(420f, 24f));
        _hintText = CreateText("HintText", hintRegion, font, 16, TextAnchor.MiddleRight);
        _hintText.color = new Color(0.78f, 0.84f, 0.95f, 0.9f);

        _leftPortrait = CreatePortrait("LeftPortrait", _panelRoot, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 24f));
        _rightPortrait = CreatePortrait("RightPortrait", _panelRoot, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f));
    }

    private static RectTransform CreateRect(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject root = new GameObject(objectName, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private static Text CreateText(string objectName, Transform parent, Font font, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(18f, 14f);
        rect.offsetMax = new Vector2(-18f, -14f);

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.text = string.Empty;
        return text;
    }

    private static Image CreatePortrait(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition)
    {
        RectTransform rect = CreateRect(objectName, parent, anchorMin, anchorMax, pivot, anchoredPosition, new Vector2(200f, 200f));
        Image frame = rect.gameObject.AddComponent<Image>();
        frame.color = new Color(0.18f, 0.21f, 0.28f, 0.98f);

        GameObject portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portraitObject.transform.SetParent(rect, false);
        RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
        portraitRect.anchorMin = Vector2.zero;
        portraitRect.anchorMax = Vector2.one;
        portraitRect.offsetMin = new Vector2(10f, 10f);
        portraitRect.offsetMax = new Vector2(-10f, -10f);

        Image portrait = portraitObject.GetComponent<Image>();
        portrait.preserveAspect = true;
        portrait.color = Color.white;
        portraitObject.SetActive(false);
        return portrait;
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

    private void SetPanelVisible(bool visible)
    {
        if (_panelRoot != null)
        {
            _panelRoot.gameObject.SetActive(visible);
        }
    }

    private static bool ReadAdvancePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.spaceKey.wasPressedThisFrame
                || Keyboard.current.enterKey.wasPressedThisFrame
                || Keyboard.current.numpadEnterKey.wasPressedThisFrame
                || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        }
#endif
        return Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetMouseButtonDown(0);
    }

    private static bool ReadSkipPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.tabKey.wasPressedThisFrame
                || Keyboard.current.escapeKey.wasPressedThisFrame;
        }
#endif
        return Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape);
    }
}
