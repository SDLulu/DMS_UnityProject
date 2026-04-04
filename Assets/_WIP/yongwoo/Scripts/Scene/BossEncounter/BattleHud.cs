using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 역할:
// - 씬에 배치된 HUD 레이아웃을 찾아 플레이어/보스 체력과 설정 패널을 갱신합니다.
// - UI를 새로 생성하지 않고 scene-authored 레이아웃을 제어하는 데 집중합니다.
//
// 구조 포인트:
// - 전투 상태의 표현 계층이며, 실제 흐름 제어는 BossEncounterDirector가 담당합니다.

[DisallowMultipleComponent]
public class BattleHud : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("플레이어 체력바 기준이 될 플레이어 상호작용 컴포넌트입니다. 비어 있으면 씬의 Player에서 자동으로 찾습니다.")]
    [SerializeField] private PlayerInteraction playerInteraction;

    [Header("Scene Layout")]
    [Tooltip("플레이어 체력바 전체 루트입니다.")]
    [SerializeField] private GameObject playerBarRoot;
    [Tooltip("플레이어 체력바 Fill 이미지입니다.")]
    [SerializeField] private Image playerFill;
    [Tooltip("플레이어 체력 수치 텍스트입니다.")]
    [SerializeField] private Text playerText;
    [Tooltip("보스 체력바 전체 루트입니다.")]
    [SerializeField] private GameObject bossBarRoot;
    [Tooltip("보스 체력바 Fill 이미지입니다.")]
    [SerializeField] private Image bossFill;
    [Tooltip("보스 체력 수치 텍스트입니다.")]
    [SerializeField] private Text bossText;
    [Tooltip("설정 패널을 여닫는 HUD 버튼입니다.")]
    [SerializeField] private Button settingsButton;
    [Tooltip("설정 버튼 라벨 텍스트입니다.")]
    [SerializeField] private Text settingsButtonText;
    [Tooltip("입력 설정 패널 전체 루트입니다.")]
    [SerializeField] private RectTransform settingsPanelRoot;
    [Tooltip("입력 설정 패널을 제어하는 컴포넌트입니다.")]
    [SerializeField] private GameInputSettingsPanel settingsPanel;
    [Tooltip("HUD와 대화 패널 같은 여러 UI를 함께 관리하는 허브입니다.")]
    [SerializeField] private UIManager uiManager;

    private readonly Color _playerHighColor = new(0.24f, 0.88f, 0.58f, 1f);
    private readonly Color _playerMidColor = new(1f, 0.82f, 0.24f, 1f);
    private readonly Color _playerLowColor = new(1f, 0.32f, 0.32f, 1f);
    private readonly Color _bossHighColor = new(1f, 0.38f, 0.42f, 1f);
    private readonly Color _bossMidColor = new(1f, 0.2f, 0.28f, 1f);
    private readonly Color _bossLowColor = new(0.72f, 0.06f, 0.1f, 1f);

    private BossInteraction _bossInteraction;
    private bool _hasLoggedMissingUiWarning;
    private bool _hasLoggedMissingEventSystemWarning;

    public bool HasSettingsPanel => uiManager != null
        ? uiManager.HasInputSettingsPanel
        : ResolveSettingsPanelRoot() != null && ResolveSettingsPanel() != null;
    public bool IsSettingsPanelVisible => uiManager != null
        ? uiManager.IsInputSettingsPanelVisible
        : ResolveSettingsPanelRoot() != null && ResolveSettingsPanelRoot().gameObject.activeSelf;

    private void Reset()
    {
        TryAutoWireReferences();
        TryAutoBindSceneLayout();
    }

    private void Awake()
    {
        NormalizeCanvasRootLayout();
        TryAutoWireReferences();
        TryAutoBindSceneLayout();
        ValidateSceneLayout();
        WireButtons();
        SetSettingsPanelVisible(false);
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        RefreshPlayer();
        RefreshBoss();
    }

    private void OnEnable()
    {
        NormalizeCanvasRootLayout();
        TryAutoWireReferences();
        TryAutoBindSceneLayout();
        ValidateSceneLayout();
        WireButtons();
        SetSettingsPanelVisible(false);
        UnhookEvents();
        HookEvents();
        RefreshAll();
    }

    private void OnDisable()
    {
        UnhookEvents();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        TryAutoWireReferences();
        TryAutoBindSceneLayout();
    }

    public void Initialize(PlayerInteraction newPlayerInteraction, BossInteraction bossInteraction)
    {
        UnhookEvents();
        playerInteraction = newPlayerInteraction;
        _bossInteraction = bossInteraction;

        TryAutoBindSceneLayout();
        ValidateSceneLayout();
        HookEvents();
        RefreshAll();
    }

    public void SetBossHealth(BossInteraction bossInteraction)
    {
        if (_bossInteraction != null)
        {
            _bossInteraction.HealthChanged -= RefreshBoss;
        }

        _bossInteraction = bossInteraction;
        if (_bossInteraction != null)
        {
            _bossInteraction.HealthChanged += RefreshBoss;
        }

        RefreshBoss();
    }

    public void ToggleSettingsPanelVisibility()
    {
        if (!HasSettingsPanel)
        {
            return;
        }

        SetSettingsPanelVisible(!IsSettingsPanelVisible);
    }

    public void SetSettingsPanelVisibility(bool visible)
    {
        SetSettingsPanelVisible(visible);
    }

    private void TryAutoWireReferences()
    {
        uiManager ??= GetComponent<UIManager>();
        uiManager ??= Object.FindFirstObjectByType<UIManager>();

        if (playerInteraction != null)
        {
            return;
        }

        SimplePlayerController playerController = Object.FindFirstObjectByType<SimplePlayerController>();
        if (playerController != null)
        {
            playerInteraction = playerController.GetComponent<PlayerInteraction>();
        }
    }

    private void TryAutoBindSceneLayout()
    {
        Transform playerBarTransform = playerBarRoot != null ? playerBarRoot.transform : FindDescendantByName(transform, "PlayerBar");
        playerBarRoot ??= playerBarTransform != null ? playerBarTransform.gameObject : null;
        playerFill ??= FindDescendantByName(playerBarTransform, "Fill")?.GetComponent<Image>();
        playerText ??= FindDescendantByName(playerBarTransform, "Label")?.GetComponent<Text>();

        Transform bossBarTransform = bossBarRoot != null ? bossBarRoot.transform : FindDescendantByName(transform, "BossBar");
        bossBarRoot ??= bossBarTransform != null ? bossBarTransform.gameObject : null;
        bossFill ??= FindDescendantByName(bossBarTransform, "Fill")?.GetComponent<Image>();
        bossText ??= FindDescendantByName(bossBarTransform, "Label")?.GetComponent<Text>();

        Transform settingsButtonTransform = settingsButton != null ? settingsButton.transform : FindDescendantByName(transform, "SettingsButton");
        settingsButton ??= settingsButtonTransform != null ? settingsButtonTransform.GetComponent<Button>() : null;
        settingsButtonText ??= FindDescendantByName(settingsButtonTransform, "Label")?.GetComponent<Text>();

        settingsPanelRoot ??= FindDescendantByName(transform, "InputSettingsPanel") as RectTransform;
        settingsPanel ??= settingsPanelRoot != null ? settingsPanelRoot.GetComponent<GameInputSettingsPanel>() : null;
        uiManager?.BindInputSettingsPanel(settingsPanelRoot, settingsPanel);
    }

    private void ValidateSceneLayout()
    {
        if (!HasValidHudBindings() && !_hasLoggedMissingUiWarning)
        {
            _hasLoggedMissingUiWarning = true;
            Debug.LogWarning(
                $"{nameof(BattleHud)} on {name} could not find a usable scene-authored HUD layout. " +
                "Place PlayerBar, BossBar, InputSettingsPanel under this object and bind their widgets.",
                this);
        }

        if (uiManager != null)
        {
            uiManager.BindInputSettingsPanel(ResolveSettingsPanelRoot(), ResolveSettingsPanel());
            uiManager.ValidateManagedUi(this);
        }
        else
        {
            RectTransform resolvedSettingsPanelRoot = ResolveSettingsPanelRoot();
            GameInputSettingsPanel resolvedSettingsPanel = ResolveSettingsPanel();
            if (resolvedSettingsPanelRoot != null && resolvedSettingsPanel == null)
            {
                Debug.LogWarning(
                    $"{nameof(BattleHud)} on {name} found InputSettingsPanel but it is missing {nameof(GameInputSettingsPanel)}.",
                    this);
            }

            if (!_hasLoggedMissingEventSystemWarning && HasSettingsPanel && EventSystem.current == null)
            {
                _hasLoggedMissingEventSystemWarning = true;
                Debug.LogWarning(
                    $"{nameof(BattleHud)} on {name} could not find an EventSystem in the scene. " +
                    "UI buttons will not respond until you place one in the scene.",
                    this);
            }

            ConfigureSettingsPanelFromHierarchy();
        }

        RefreshSettingsButtonState();
    }

    private bool HasValidHudBindings()
    {
        return ResolvePlayerBarRoot() != null
            && playerFill != null
            && playerText != null
            && ResolveBossBarRoot() != null
            && bossFill != null
            && bossText != null;
    }

    private void HookEvents()
    {
        if (playerInteraction != null)
        {
            playerInteraction.HealthChanged += RefreshPlayer;
        }

        if (_bossInteraction != null)
        {
            _bossInteraction.HealthChanged += RefreshBoss;
        }
    }

    private void UnhookEvents()
    {
        if (playerInteraction != null)
        {
            playerInteraction.HealthChanged -= RefreshPlayer;
        }

        if (_bossInteraction != null)
        {
            _bossInteraction.HealthChanged -= RefreshBoss;
        }
    }

    private void RefreshAll()
    {
        RefreshPlayer();
        RefreshBoss();
        RefreshSettingsButtonState();
    }

    private void RefreshPlayer()
    {
        SetRootVisible(ResolvePlayerBarRoot(), playerInteraction != null);
        RefreshBar(playerInteraction, playerFill, playerText, "PLAYER", _playerHighColor, _playerMidColor, _playerLowColor);
    }

    private void RefreshBoss()
    {
        bool showBossBar = _bossInteraction != null && (_bossInteraction.CurrentHealth > 0f || _bossInteraction.MaxHealth <= 0f);
        SetRootVisible(ResolveBossBarRoot(), showBossBar);
        RefreshBar(_bossInteraction, bossFill, bossText, "BOSS", _bossHighColor, _bossMidColor, _bossLowColor);
    }

    private static void RefreshBar(
        object interaction,
        Image fill,
        Text text,
        string label,
        Color highColor,
        Color midColor,
        Color lowColor)
    {
        if (fill == null || text == null)
        {
            return;
        }

        ConfigureFillImage(fill);

        if (interaction == null)
        {
            fill.fillAmount = 0f;
            text.text = string.Empty;
            return;
        }

        float normalized = interaction switch
        {
            PlayerInteraction player => player.HealthNormalized,
            BossInteraction boss => boss.HealthNormalized,
            _ => 0f
        };
        fill.fillAmount = normalized;
        fill.color = EvaluateBarColor(normalized, highColor, midColor, lowColor);
        int current = Mathf.CeilToInt(interaction switch
        {
            PlayerInteraction player => player.CurrentHealth,
            BossInteraction boss => boss.CurrentHealth,
            _ => 0f
        });
        int max = Mathf.CeilToInt(interaction switch
        {
            PlayerInteraction player => player.MaxHealth,
            BossInteraction boss => boss.MaxHealth,
            _ => 0f
        });
        string displayLabel = label switch
        {
            "PLAYER" => "플레이어 HP",
            "BOSS" => "보스 HP",
            _ => label
        };

        text.text = $"{displayLabel} {current} / {max}";
        text.color = normalized <= 0.25f ? new Color(1f, 0.88f, 0.88f, 1f) : Color.white;
    }

    private static void ConfigureFillImage(Image fill)
    {
        if (fill.type != Image.Type.Filled)
        {
            fill.type = Image.Type.Filled;
        }

        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillClockwise = true;
    }

    private static Color EvaluateBarColor(float normalized, Color highColor, Color midColor, Color lowColor)
    {
        if (normalized >= 0.5f)
        {
            return Color.Lerp(midColor, highColor, Mathf.InverseLerp(0.5f, 1f, normalized));
        }

        return Color.Lerp(lowColor, midColor, Mathf.InverseLerp(0f, 0.5f, normalized));
    }

    private void SetSettingsPanelVisible(bool visible)
    {
        if (uiManager != null)
        {
            uiManager.SetInputSettingsPanelVisible(visible);
            RefreshSettingsButtonState();
            return;
        }

        RectTransform resolvedSettingsPanelRoot = ResolveSettingsPanelRoot();
        GameInputSettingsPanel resolvedSettingsPanel = ResolveSettingsPanel();
        if (resolvedSettingsPanelRoot == null)
        {
            RefreshSettingsButtonState();
            return;
        }

        resolvedSettingsPanelRoot.gameObject.SetActive(visible);
        if (visible)
        {
            resolvedSettingsPanel?.RefreshPanel();
        }

        RefreshSettingsButtonState();
    }

    private void WireButtons()
    {
        if (settingsButton == null)
        {
            return;
        }

        settingsButton.onClick.RemoveListener(HandleSettingsButtonPressed);
        settingsButton.onClick.AddListener(HandleSettingsButtonPressed);
    }

    private void HandleSettingsButtonPressed()
    {
        ToggleSettingsPanelVisibility();
    }

    private void ConfigureSettingsPanelFromHierarchy()
    {
        RectTransform resolvedSettingsPanelRoot = ResolveSettingsPanelRoot();
        GameInputSettingsPanel resolvedSettingsPanel = ResolveSettingsPanel();
        if (resolvedSettingsPanelRoot == null || resolvedSettingsPanel == null)
        {
            return;
        }

        Button closeButton = resolvedSettingsPanelRoot.Find("CloseButton")?.GetComponent<Button>();
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => SetSettingsPanelVisible(false));
        }

        Slider sensitivitySlider = resolvedSettingsPanelRoot.Find("SensitivitySlider")?.GetComponent<Slider>();
        Text sensitivityValueText = resolvedSettingsPanelRoot.Find("SensitivityValue")?.GetComponent<Text>();
        Button resetAllButton = resolvedSettingsPanelRoot.Find("ResetAllButton")?.GetComponent<Button>();
        Text statusText = resolvedSettingsPanelRoot.Find("StatusText")?.GetComponent<Text>();

        if (sensitivitySlider != null && sensitivityValueText != null)
        {
            resolvedSettingsPanel.SetLookSensitivityWidgets(sensitivitySlider, sensitivityValueText);
        }

        if (resetAllButton != null && statusText != null)
        {
            resolvedSettingsPanel.SetActionWidgets(resetAllButton, statusText);
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
            settingsPanel.SetBindingRows(rows);
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
        RectTransform resolvedSettingsPanelRoot = ResolveSettingsPanelRoot();
        if (resolvedSettingsPanelRoot == null)
        {
            return;
        }

        Transform rowRoot = resolvedSettingsPanelRoot.Find($"Bindings/{rowName}");
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

    private static void SetRootVisible(GameObject target, bool visible)
    {
        if (target != null)
        {
            target.SetActive(visible);
        }
    }

    private void RefreshSettingsButtonState()
    {
        if (settingsButtonText != null)
        {
            settingsButtonText.text = IsSettingsPanelVisible ? "설정 닫기" : "설정";
        }

        if (settingsButton != null)
        {
            settingsButton.interactable = HasSettingsPanel;
        }
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

    private GameObject ResolvePlayerBarRoot()
    {
        if (playerBarRoot != null)
        {
            return playerBarRoot;
        }

        Transform found = FindAncestorByName(playerFill != null ? playerFill.transform : playerText != null ? playerText.transform : null, "PlayerBar")
            ?? FindDescendantByName(transform, "PlayerBar");
        playerBarRoot = found != null ? found.gameObject : null;
        return playerBarRoot;
    }

    private GameObject ResolveBossBarRoot()
    {
        if (bossBarRoot != null)
        {
            return bossBarRoot;
        }

        Transform found = FindAncestorByName(bossFill != null ? bossFill.transform : bossText != null ? bossText.transform : null, "BossBar")
            ?? FindDescendantByName(transform, "BossBar");
        bossBarRoot = found != null ? found.gameObject : null;
        return bossBarRoot;
    }

    private RectTransform ResolveSettingsPanelRoot()
    {
        if (settingsPanelRoot != null)
        {
            return settingsPanelRoot;
        }

        Transform found = FindAncestorByName(settingsPanel != null ? settingsPanel.transform : null, "InputSettingsPanel")
            ?? FindDescendantByName(transform, "InputSettingsPanel");
        settingsPanelRoot = found as RectTransform;
        return settingsPanelRoot;
    }

    private GameInputSettingsPanel ResolveSettingsPanel()
    {
        if (settingsPanel != null)
        {
            return settingsPanel;
        }

        RectTransform resolvedSettingsPanelRoot = ResolveSettingsPanelRoot();
        settingsPanel = resolvedSettingsPanelRoot != null
            ? resolvedSettingsPanelRoot.GetComponent<GameInputSettingsPanel>()
            : GetComponentInChildren<GameInputSettingsPanel>(true);
        return settingsPanel;
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
            Transform child = root.GetChild(i);
            Transform match = FindDescendantByName(child, targetName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
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
