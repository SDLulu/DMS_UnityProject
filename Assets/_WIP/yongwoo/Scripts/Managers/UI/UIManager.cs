using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 역할:
// - 씬에 배치된 UI 패널을 한 곳에서 등록하고 여닫는 허브입니다.
// - 패널을 새로 생성하지 않고, scene-authored UI를 연결하고 표시 상태만 관리합니다.
//
// 구조 포인트:
// - 개별 HUD/패널은 자기 표시 내용을 알고, 여러 패널의 열기/닫기 책임은 이 객체로 모읍니다.

[DisallowMultipleComponent]
public class UIManager : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("입력 설정 패널 전체 루트입니다.")]
    [SerializeField] private RectTransform inputSettingsPanelRoot;
    [Tooltip("입력 설정 패널을 제어하는 컴포넌트입니다.")]
    [SerializeField] private GameInputSettingsPanel inputSettingsPanel;
    [Tooltip("대화 표시 패널입니다. 필요하면 씬에서 자동으로 찾습니다.")]
    [SerializeField] private DialoguePanel dialoguePanel;

    private bool _hasLoggedMissingEventSystemWarning;

    public bool HasInputSettingsPanel => ResolveInputSettingsPanelRoot() != null && ResolveInputSettingsPanel() != null;
    public bool IsInputSettingsPanelVisible => ResolveInputSettingsPanelRoot() != null && ResolveInputSettingsPanelRoot().gameObject.activeSelf;
    public DialoguePanel DialoguePanel => ResolveDialoguePanel();

    private void Reset()
    {
        TryAutoWire();
    }

    private void Awake()
    {
        TryAutoWire();
    }

    private void OnEnable()
    {
        TryAutoWire();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        TryAutoWire();
    }

    public void BindInputSettingsPanel(RectTransform panelRoot, GameInputSettingsPanel panel)
    {
        inputSettingsPanelRoot = panelRoot;
        inputSettingsPanel = panel;
    }

    public void BindDialoguePanel(DialoguePanel panel)
    {
        dialoguePanel = panel;
    }

    public void ValidateManagedUi(Component context)
    {
        RectTransform resolvedInputSettingsPanelRoot = ResolveInputSettingsPanelRoot();
        GameInputSettingsPanel resolvedInputSettingsPanel = ResolveInputSettingsPanel();
        if (resolvedInputSettingsPanelRoot != null && resolvedInputSettingsPanel == null)
        {
            Debug.LogWarning(
                $"{nameof(UIManager)} on {name} found InputSettingsPanel but it is missing {nameof(GameInputSettingsPanel)}.",
                context != null ? context : this);
        }

        if (!_hasLoggedMissingEventSystemWarning && HasInputSettingsPanel && EventSystem.current == null)
        {
            _hasLoggedMissingEventSystemWarning = true;
            Debug.LogWarning(
                $"{nameof(UIManager)} on {name} could not find an EventSystem in the scene. " +
                "UI buttons will not respond until you place one in the scene.",
                context != null ? context : this);
        }

        ConfigureInputSettingsPanelFromHierarchy();
    }

    public void ToggleInputSettingsPanel()
    {
        if (!HasInputSettingsPanel)
        {
            return;
        }

        SetInputSettingsPanelVisible(!IsInputSettingsPanelVisible);
    }

    public void SetInputSettingsPanelVisible(bool visible)
    {
        RectTransform resolvedInputSettingsPanelRoot = ResolveInputSettingsPanelRoot();
        GameInputSettingsPanel resolvedInputSettingsPanel = ResolveInputSettingsPanel();
        if (resolvedInputSettingsPanelRoot == null)
        {
            return;
        }

        resolvedInputSettingsPanelRoot.gameObject.SetActive(visible);
        if (visible)
        {
            resolvedInputSettingsPanel?.RefreshPanel();
        }
    }

    public void RefreshManagedUi()
    {
        if (IsInputSettingsPanelVisible)
        {
            ResolveInputSettingsPanel()?.RefreshPanel();
        }
    }

    private void TryAutoWire()
    {
        inputSettingsPanel ??= Object.FindFirstObjectByType<GameInputSettingsPanel>();
        if (inputSettingsPanelRoot == null && inputSettingsPanel != null)
        {
            inputSettingsPanelRoot = FindAncestorByName(inputSettingsPanel.transform, "InputSettingsPanel") as RectTransform;
        }

        dialoguePanel ??= Object.FindFirstObjectByType<DialoguePanel>();
    }

    private void ConfigureInputSettingsPanelFromHierarchy()
    {
        RectTransform resolvedInputSettingsPanelRoot = ResolveInputSettingsPanelRoot();
        GameInputSettingsPanel resolvedInputSettingsPanel = ResolveInputSettingsPanel();
        if (resolvedInputSettingsPanelRoot == null || resolvedInputSettingsPanel == null)
        {
            return;
        }

        Button closeButton = resolvedInputSettingsPanelRoot.Find("CloseButton")?.GetComponent<Button>();
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => SetInputSettingsPanelVisible(false));
        }

        Button resetAllButton = resolvedInputSettingsPanelRoot.Find("ResetAllButton")?.GetComponent<Button>();
        Text statusText = resolvedInputSettingsPanelRoot.Find("StatusText")?.GetComponent<Text>();

        if (resetAllButton != null && statusText != null)
        {
            resolvedInputSettingsPanel.SetActionWidgets(resetAllButton, statusText);
        }

        List<GameInputSettingsPanel.BindingRow> rows = new();
        TryAddSettingsRow(rows, "MoveUp", "이동 위", "Player", "Move", GameInput.Instance.FindBindingIndex("Player", "Move", "up", "Keyboard&Mouse"));
        TryAddSettingsRow(rows, "MoveDown", "이동 아래", "Player", "Move", GameInput.Instance.FindBindingIndex("Player", "Move", "down", "Keyboard&Mouse"));
        TryAddSettingsRow(rows, "MoveLeft", "이동 왼쪽", "Player", "Move", GameInput.Instance.FindBindingIndex("Player", "Move", "left", "Keyboard&Mouse"));
        TryAddSettingsRow(rows, "MoveRight", "이동 오른쪽", "Player", "Move", GameInput.Instance.FindBindingIndex("Player", "Move", "right", "Keyboard&Mouse"));
        TryAddSettingsRow(rows, "Jump", "점프", "Player", "Jump", GameInput.Instance.FindBindingIndex("Player", "Jump", groupContains: "Keyboard&Mouse"));
        TryAddSettingsRow(rows, "Crouch", "앉기", "Player", "Crouch", GameInput.Instance.FindBindingIndex("Player", "Crouch", groupContains: "Keyboard&Mouse"));
        TryAddSettingsRow(rows, "Sprint", "대시", "Player", "Sprint", GameInput.Instance.FindBindingIndex("Player", "Sprint", groupContains: "Keyboard&Mouse"));
        TryAddSettingsRow(rows, "Attack", "공격", "Player", "Attack", GameInput.Instance.FindBindingIndex("Player", "Attack", groupContains: "Keyboard&Mouse"));
        TryAddSettingsRow(rows, "Interact", "상호작용", "Player", "Interact", GameInput.Instance.FindBindingIndex("Player", "Interact", groupContains: "Keyboard&Mouse"));
        TryAddSettingsRow(rows, "DialogueAdvance", "대화 진행", "Dialogue", "Advance", GameInput.Instance.FindBindingIndex("Dialogue", "Advance", groupContains: "Keyboard&Mouse"));
        TryAddSettingsRow(rows, "DialogueSkip", "대화 스킵", "Dialogue", "Skip", GameInput.Instance.FindBindingIndex("Dialogue", "Skip", groupContains: "Keyboard&Mouse"));

        if (rows.Count > 0)
        {
            resolvedInputSettingsPanel.SetBindingRows(rows);
        }
    }

    private void TryAddSettingsRow(
        List<GameInputSettingsPanel.BindingRow> rows,
        string rowName,
        string label,
        string mapName,
        string actionName,
        int bindingIndex)
    {
        RectTransform resolvedInputSettingsPanelRoot = ResolveInputSettingsPanelRoot();
        if (resolvedInputSettingsPanelRoot == null)
        {
            return;
        }

        Transform rowRoot = resolvedInputSettingsPanelRoot.Find($"Bindings/{rowName}");
        if (rowRoot == null)
        {
            return;
        }

        Text labelText = rowRoot.Find("Label")?.GetComponent<Text>();
        Text valueText = rowRoot.Find("Value")?.GetComponent<Text>();
        Button rebindButton = rowRoot.Find("RebindButton")?.GetComponent<Button>();
        Button resetButton = rowRoot.Find("ResetButton")?.GetComponent<Button>();
        if (labelText == null || valueText == null || rebindButton == null || resetButton == null)
        {
            return;
        }

        rows.Add(new GameInputSettingsPanel.BindingRow
        {
            label = label,
            mapName = mapName,
            actionName = actionName,
            bindingIndex = Mathf.Max(0, bindingIndex),
            labelText = labelText,
            bindingValueText = valueText,
            rebindButton = rebindButton,
            resetButton = resetButton
        });
    }

    private RectTransform ResolveInputSettingsPanelRoot()
    {
        if (inputSettingsPanelRoot != null)
        {
            return inputSettingsPanelRoot;
        }

        if (inputSettingsPanel != null)
        {
            inputSettingsPanelRoot = FindAncestorByName(inputSettingsPanel.transform, "InputSettingsPanel") as RectTransform;
        }

        return inputSettingsPanelRoot;
    }

    private GameInputSettingsPanel ResolveInputSettingsPanel()
    {
        if (inputSettingsPanel != null)
        {
            return inputSettingsPanel;
        }

        RectTransform resolvedInputSettingsPanelRoot = ResolveInputSettingsPanelRoot();
        inputSettingsPanel = resolvedInputSettingsPanelRoot != null
            ? resolvedInputSettingsPanelRoot.GetComponent<GameInputSettingsPanel>()
            : Object.FindFirstObjectByType<GameInputSettingsPanel>();
        return inputSettingsPanel;
    }

    private DialoguePanel ResolveDialoguePanel()
    {
        dialoguePanel ??= Object.FindFirstObjectByType<DialoguePanel>();
        return dialoguePanel;
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
