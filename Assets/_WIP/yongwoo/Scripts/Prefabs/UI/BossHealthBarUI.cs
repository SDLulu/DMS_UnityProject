using UnityEngine;
using UnityEngine.UI;

// 역할:
// - 현재 페이즈 분신 HP 합계를 상단 중앙 fill bar로 표시합니다.
// - 루트 BossPhaseController.AggregateHealthChanged + 매 프레임 동기화.

[DisallowMultipleComponent]
public class BossHealthBarUI : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField, Min(80f)] private float barWidth = 840f;
    [SerializeField, Min(4f)] private float barHeight = 42f;
    [SerializeField] private Vector2 topOffset = new Vector2(0f, -56f);
    [SerializeField, Min(0f)] private float fillPadding = 4f;

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.1f, 0.14f, 0.72f);
    [SerializeField] private Color fillColor = new Color(1f, 0.22f, 0.28f, 0.92f);
    [SerializeField] private Color borderColor = new Color(0f, 0.9f, 1f, 0.55f);

    [Header("References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform barRoot;
    [SerializeField] private Image fillImage;
    [SerializeField] private CanvasGroup canvasGroup;

    private BossPhaseController _boundRoot;
    private RectTransform _fillRect;
    private bool _uiBuilt;
    private int _lastCurrent = -1;
    private int _lastMax = -1;
    private static Sprite _uiWhiteSprite;

    public void Bind(BossPhaseController rootController)
    {
        Unbind();

        if (rootController == null || !rootController.IsRootController)
        {
            return;
        }

        _boundRoot = rootController;
        _boundRoot.AggregateHealthChanged += HandleAggregateHealthChanged;
        EnsureUiBuilt();
        _boundRoot.RefreshAggregateHealth();
        ForceRefreshFill();
        SetVisible(true);
    }

    public void Unbind()
    {
        if (_boundRoot != null)
        {
            _boundRoot.AggregateHealthChanged -= HandleAggregateHealthChanged;
            _boundRoot = null;
        }

        _lastCurrent = -1;
        _lastMax = -1;
    }

    public void SetVisible(bool visible)
    {
        EnsureUiBuilt();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        else if (barRoot != null)
        {
            barRoot.gameObject.SetActive(visible);
        }
    }

    private void LateUpdate()
    {
        if (_boundRoot == null)
        {
            return;
        }

        ForceRefreshFill();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void HandleAggregateHealthChanged(int current, int max)
    {
        RefreshFill(current, max);
    }

    private void ForceRefreshFill()
    {
        if (_boundRoot == null)
        {
            return;
        }

        RefreshFill(_boundRoot.AggregateCurrentHealth, _boundRoot.AggregateMaxHealth);
    }

    private void RefreshFill(int current, int max)
    {
        if (_fillRect == null)
        {
            return;
        }

        if (current == _lastCurrent && max == _lastMax)
        {
            return;
        }

        _lastCurrent = current;
        _lastMax = max;

        float normalized = max > 0 ? Mathf.Clamp01(current / (float)max) : 0f;
        _fillRect.anchorMin = new Vector2(0f, 0f);
        _fillRect.anchorMax = new Vector2(normalized, 1f);
        _fillRect.pivot = new Vector2(0f, 0.5f);
        _fillRect.offsetMin = new Vector2(fillPadding, fillPadding);
        _fillRect.offsetMax = new Vector2(-fillPadding, -fillPadding);
    }

    private void EnsureUiBuilt()
    {
        targetCanvas ??= FindHudCanvas();
        if (targetCanvas == null)
        {
            Debug.LogWarning("[BossHealthBarUI] HUD Canvas를 찾지 못했습니다.");
            return;
        }

        if (barRoot == null)
        {
            Transform existing = targetCanvas.transform.Find("BossHealthBar");
            if (existing != null)
            {
                barRoot = existing as RectTransform;
            }
            else
            {
                GameObject rootGo = new GameObject("BossHealthBar", typeof(RectTransform));
                rootGo.transform.SetParent(targetCanvas.transform, false);
                barRoot = rootGo.GetComponent<RectTransform>();
            }
        }

        ApplyLayout();

        canvasGroup ??= barRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = barRoot.gameObject.AddComponent<CanvasGroup>();
        }

        Image background = EnsureChildImage("Background", barRoot, 0);
        background.color = backgroundColor;
        StretchRect(background.rectTransform);

        Image border = EnsureChildImage("Border", barRoot, 1);
        border.color = borderColor;
        StretchRect(border.rectTransform);
        border.rectTransform.offsetMin = new Vector2(-3f, -3f);
        border.rectTransform.offsetMax = new Vector2(3f, 3f);

        fillImage = EnsureChildImage("Fill", barRoot, 2);
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Simple;
        _fillRect = fillImage.rectTransform;
        RefreshFill(_lastCurrent >= 0 ? _lastCurrent : 1, _lastMax > 0 ? _lastMax : 1);

        _uiBuilt = true;
    }

    private void ApplyLayout()
    {
        if (barRoot == null)
        {
            return;
        }

        barRoot.anchorMin = new Vector2(0.5f, 1f);
        barRoot.anchorMax = new Vector2(0.5f, 1f);
        barRoot.pivot = new Vector2(0.5f, 1f);
        barRoot.anchoredPosition = topOffset;
        barRoot.sizeDelta = new Vector2(barWidth, barHeight);
    }

    private static Canvas FindHudCanvas()
    {
        GameObject hud = GameObject.Find("HUD");
        if (hud != null && hud.TryGetComponent(out Canvas canvas))
        {
            return canvas;
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas candidate = canvases[i];
            if (candidate != null && candidate.isRootCanvas)
            {
                return candidate;
            }
        }

        return null;
    }

    private static Image EnsureChildImage(string objectName, RectTransform parent, int siblingIndex)
    {
        Transform child = parent.Find(objectName);
        if (child == null)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            child = go.transform;
        }

        child.SetSiblingIndex(siblingIndex);
        Image image = child.GetComponent<Image>();
        image.raycastTarget = false;
        image.sprite = GetUiWhiteSprite();
        image.type = Image.Type.Simple;
        return image;
    }

    private static Sprite GetUiWhiteSprite()
    {
        if (_uiWhiteSprite != null)
        {
            return _uiWhiteSprite;
        }

        Texture2D texture = Texture2D.whiteTexture;
        _uiWhiteSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        return _uiWhiteSprite;
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
