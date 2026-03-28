using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[DisallowMultipleComponent]
public class PrototypeBattleHud : MonoBehaviour
{
    private readonly Color _playerHighColor = new Color(0.24f, 0.88f, 0.58f, 1f);
    private readonly Color _playerMidColor = new Color(1f, 0.82f, 0.24f, 1f);
    private readonly Color _playerLowColor = new Color(1f, 0.32f, 0.32f, 1f);
    private readonly Color _bossHighColor = new Color(1f, 0.38f, 0.42f, 1f);
    private readonly Color _bossMidColor = new Color(1f, 0.2f, 0.28f, 1f);
    private readonly Color _bossLowColor = new Color(0.72f, 0.06f, 0.1f, 1f);

    private PrototypeHealth _playerHealth;
    private PrototypeHealth _bossHealth;

    private Image _playerFill;
    private RectTransform _playerFillRect;
    private float _playerFillWidth;
    private Text _playerText;
    private Image _bossFill;
    private RectTransform _bossFillRect;
    private float _bossFillWidth;
    private Text _bossText;
    private Button _bossSpawnButton;
    private Text _bossSpawnButtonText;
    private PrototypeBossDebugDirector _director;

    public void Initialize(PrototypeHealth playerHealth, PrototypeHealth bossHealth)
    {
        UnhookEvents();
        _playerHealth = playerHealth;
        _bossHealth = bossHealth;

        BuildUi();
        HookEvents();
        RefreshAll();
    }

    public void BindDirector(PrototypeBossDebugDirector director)
    {
        _director = director;
        if (_bossSpawnButton != null)
        {
            _bossSpawnButton.onClick.RemoveAllListeners();
            if (_director != null)
            {
                _bossSpawnButton.onClick.AddListener(_director.SpawnOrResetBoss);
            }
        }

        UpdateBossButtonLabel();
    }

    public void SetBossHealth(PrototypeHealth bossHealth)
    {
        if (_bossHealth != null)
        {
            _bossHealth.HealthChanged -= RefreshBoss;
        }

        _bossHealth = bossHealth;
        if (_bossHealth != null)
        {
            _bossHealth.HealthChanged += RefreshBoss;
        }

        RefreshBoss();
        UpdateBossButtonLabel();
    }

    private void OnDestroy()
    {
        UnhookEvents();
    }

    private void HookEvents()
    {
        if (_playerHealth != null)
        {
            _playerHealth.HealthChanged += RefreshPlayer;
        }

        if (_bossHealth != null)
        {
            _bossHealth.HealthChanged += RefreshBoss;
        }
    }

    private void UnhookEvents()
    {
        if (_playerHealth != null)
        {
            _playerHealth.HealthChanged -= RefreshPlayer;
        }

        if (_bossHealth != null)
        {
            _bossHealth.HealthChanged -= RefreshBoss;
        }
    }

    private void RefreshAll()
    {
        RefreshPlayer();
        RefreshBoss();
    }

    private void RefreshPlayer()
    {
        RefreshBar(_playerHealth, _playerFill, _playerFillRect, _playerFillWidth, _playerText, "PLAYER", _playerHighColor, _playerMidColor, _playerLowColor);
    }

    private void RefreshBoss()
    {
        RefreshBar(_bossHealth, _bossFill, _bossFillRect, _bossFillWidth, _bossText, "BOSS", _bossHighColor, _bossMidColor, _bossLowColor);
        UpdateBossButtonLabel();
    }

    private static void RefreshBar(
        PrototypeHealth health,
        Image fill,
        RectTransform fillRect,
        float fullWidth,
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

        if (health == null)
        {
            if (fillRect != null)
            {
                fillRect.sizeDelta = new Vector2(0f, fillRect.sizeDelta.y);
            }
            text.text = $"{label} 0 / 0";
            return;
        }

        float normalized = health.HealthNormalized;
        if (fillRect != null)
        {
            fillRect.sizeDelta = new Vector2(fullWidth * normalized, fillRect.sizeDelta.y);
        }
        fill.color = EvaluateBarColor(normalized, highColor, midColor, lowColor);
        int current = Mathf.CeilToInt(health.CurrentHealth);
        int max = Mathf.CeilToInt(health.MaxHealth);
        text.text = $"{label} {current} / {max}";
        text.color = normalized <= 0.25f ? new Color(1f, 0.88f, 0.88f, 1f) : Color.white;
    }

    private static Color EvaluateBarColor(float normalized, Color highColor, Color midColor, Color lowColor)
    {
        if (normalized >= 0.5f)
        {
            return Color.Lerp(midColor, highColor, Mathf.InverseLerp(0.5f, 1f, normalized));
        }

        return Color.Lerp(lowColor, midColor, Mathf.InverseLerp(0f, 0.5f, normalized));
    }

    private void BuildUi()
    {
        if (_playerFill != null && _bossFill != null)
        {
            return;
        }

        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

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

        EnsureEventSystem();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }

        _playerFill = CreateBar(
            "PlayerBar",
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(32f, -32f),
            new Vector2(360f, 34f),
            new Color(0.16f, 0.18f, 0.24f, 0.95f),
            _playerHighColor,
            out _playerFillRect,
            out _playerFillWidth,
            out _playerText,
            font);

        _bossFill = CreateBar(
            "BossBar",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -32f),
            new Vector2(640f, 38f),
            new Color(0.18f, 0.08f, 0.1f, 0.96f),
            _bossHighColor,
            out _bossFillRect,
            out _bossFillWidth,
            out _bossText,
            font);

        _bossSpawnButton = CreateButton(
            "BossSpawnButton",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-32f, -32f),
            new Vector2(220f, 42f),
            out _bossSpawnButtonText,
            font);
    }

    private Image CreateBar(
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size,
        Color backgroundColor,
        Color fillColor,
        out RectTransform fillRect,
        out float fillWidth,
        out Text label,
        Font font)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image));
        root.transform.SetParent(transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = anchorMin;
        rootRect.anchorMax = anchorMax;
        rootRect.pivot = new Vector2(anchorMin.x == 0.5f ? 0.5f : 0f, 1f);
        rootRect.anchoredPosition = anchoredPosition;
        rootRect.sizeDelta = size;

        Image background = root.GetComponent<Image>();
        background.color = backgroundColor;

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(root.transform, false);

        fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = new Vector2(4f, 0f);
        fillWidth = size.x - 8f;
        fillRect.sizeDelta = new Vector2(fillWidth, size.y - 8f);

        Image fill = fillObject.GetComponent<Image>();
        fill.color = fillColor;

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(root.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        label = textObject.GetComponent<Text>();
        label.font = font;
        label.fontSize = 20;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = name.ToUpperInvariant();

        return fill;
    }

    private Button CreateButton(
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size,
        out Text label,
        Font font)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = anchorMin;
        rootRect.anchorMax = anchorMax;
        rootRect.pivot = new Vector2(1f, 1f);
        rootRect.anchoredPosition = anchoredPosition;
        rootRect.sizeDelta = size;

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.14f, 0.16f, 0.2f, 0.96f);

        Button button = root.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = background.color;
        colors.highlightedColor = new Color(0.2f, 0.22f, 0.28f, 1f);
        colors.pressedColor = new Color(0.1f, 0.12f, 0.16f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
        button.colors = colors;

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(root.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        label = textObject.GetComponent<Text>();
        label.font = font;
        label.fontSize = 18;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = "보스 소환";

        return button;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        Object.DontDestroyOnLoad(eventSystemObject);
    }

    private void UpdateBossButtonLabel()
    {
        if (_bossSpawnButtonText == null)
        {
            return;
        }

        _bossSpawnButtonText.text = _bossHealth == null ? "보스 소환" : "보스 리셋";
    }
}
