using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 역할:
// - 타이틀·버튼: 평소 아주 살짝 흔들림, 호버 시 밝아짐.

[DisallowMultipleComponent]
public class TitleUiMotion : MonoBehaviour
{
    [Serializable]
    private class MotionTarget
    {
        public RectTransform rect;
        public Graphic[] graphics;
        public Vector2 idleAmplitude = new Vector2(0.8f, 1.1f);
        public float idleSpeed = 0.55f;
        public float phaseOffset;
        public float hoverBrighten = 0.38f;
        public bool useUnscaledTime = true;
    }

    [Header("Typography")]
    [SerializeField] private Font displayFont;

    [Header("Targets")]
    [SerializeField] private List<MotionTarget> targets = new();

    [Header("Hover")]
    [SerializeField] private float hoverSmoothing = 12f;

    private readonly Dictionary<RectTransform, AnchorState> _anchorStates = new();
    private readonly Dictionary<RectTransform, Color[]> _baseColors = new();
    private readonly Dictionary<RectTransform, float> _hoverAmounts = new();
    private Camera _uiCamera;

    private struct AnchorState
    {
        public Vector2 anchoredPosition;
    }

    private void Reset()
    {
        TryLoadDisplayFont();
        AutoWireTargets();
    }

    private void Awake()
    {
        CacheAnchorStates();
        CacheBaseColors();
        _uiCamera = GetComponentInParent<Canvas>()?.worldCamera;
    }

#if UNITY_EDITOR
    public void ApplyEditorSceneSetup()
    {
        TryLoadDisplayFont();
        ApplyTypography();
        AutoWireTargets();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private void OnEnable()
    {
        CacheAnchorStates();
        CacheBaseColors();
    }

    private void Update()
    {
        if (targets.Count == 0)
        {
            return;
        }

        Vector2 screenMouse = Input.mousePosition;

        for (int i = 0; i < targets.Count; i++)
        {
            MotionTarget target = targets[i];
            if (target.rect == null || !_anchorStates.TryGetValue(target.rect, out AnchorState anchor))
            {
                continue;
            }

            float time = target.useUnscaledTime ? Time.unscaledTime : Time.time;
            float phase = time * target.idleSpeed + target.phaseOffset;
            Vector2 idleOffset = new Vector2(
                Mathf.Sin(phase) * target.idleAmplitude.x,
                Mathf.Cos(phase * 0.93f) * target.idleAmplitude.y);
            target.rect.anchoredPosition = anchor.anchoredPosition + idleOffset;

            bool hovered = IsHovered(target.rect, screenMouse);
            float hoverTarget = hovered ? 1f : 0f;
            if (!_hoverAmounts.TryGetValue(target.rect, out float hoverAmount))
            {
                hoverAmount = 0f;
            }

            hoverAmount = Mathf.Lerp(
                hoverAmount,
                hoverTarget,
                1f - Mathf.Exp(-hoverSmoothing * Time.unscaledDeltaTime));
            _hoverAmounts[target.rect] = hoverAmount;

            ApplyHoverColor(target, hoverAmount);
        }
    }

    private void ApplyTypography()
    {
        if (displayFont == null)
        {
            return;
        }

        ApplyTextStyle("TitleGlowPink", "DEEP DIVE", 92, new Color(1f, 0.16f, 0.70f, 0.50f), TextAnchor.MiddleCenter);
        ApplyTextStyle("TitleGlowCyan", "DEEP DIVE", 92, new Color(0.10f, 0.95f, 1f, 0.42f), TextAnchor.MiddleCenter);
        ApplyTextStyle("TitleText", "DEEP DIVE", 88, new Color(1f, 0.96f, 1f, 1f), TextAnchor.MiddleCenter);
        ApplyTextStyle("SubtitleText", "HOME RECOVERY PROTOCOL // DEBTOR 047", 20, new Color(0.96f, 0.74f, 0.38f, 0.92f), TextAnchor.MiddleCenter);
        ApplyTextStyle("CoreText", "HOME", 24, new Color(0.82f, 1f, 0.98f, 0.96f), TextAnchor.MiddleCenter);
        ApplyTextStyle("MenuLabel", "CONNECT", 17, new Color(0.50f, 0.96f, 0.88f, 0.80f), TextAnchor.MiddleCenter);
        ApplyTextStyle("BuildTag", "DMS // HOME ARCHIVE", 16, new Color(0.60f, 0.75f, 0.77f, 0.72f), TextAnchor.MiddleLeft);
        ApplyTextStyle("TopStatus", "ACCESS NODE 047", 16, new Color(0.60f, 0.88f, 0.84f, 0.74f), TextAnchor.MiddleRight);
        ApplyButtonTextStyle("StartButton/Text", "START", 30, new Color(0.95f, 1f, 0.99f, 1f));
        ApplyButtonTextStyle("QuitButton/Text", "QUIT", 24, new Color(0.82f, 0.90f, 0.92f, 0.92f));
    }

    private void ApplyTextStyle(string path, string value, int fontSize, Color color, TextAnchor alignment)
    {
        Text text = transform.Find(path)?.GetComponent<Text>();
        if (text == null)
        {
            return;
        }

        text.font = displayFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Normal;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.lineSpacing = 0.86f;
    }

    private void ApplyButtonTextStyle(string path, string value, int fontSize, Color color)
    {
        Text text = transform.Find(path)?.GetComponent<Text>();
        if (text == null)
        {
            return;
        }

        text.font = displayFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Normal;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private void ApplyHoverColor(MotionTarget target, float hoverAmount)
    {
        if (target.graphics == null || target.graphics.Length == 0)
        {
            return;
        }

        if (!_baseColors.TryGetValue(target.rect, out Color[] baseColors))
        {
            return;
        }

        for (int i = 0; i < target.graphics.Length; i++)
        {
            Graphic graphic = target.graphics[i];
            if (graphic == null || i >= baseColors.Length)
            {
                continue;
            }

            Color baseColor = baseColors[i];
            Color bright = baseColor;
            bright.r = Mathf.Min(1f, baseColor.r + target.hoverBrighten);
            bright.g = Mathf.Min(1f, baseColor.g + target.hoverBrighten);
            bright.b = Mathf.Min(1f, baseColor.b + target.hoverBrighten);
            if (baseColor.a < 0.99f)
            {
                bright.a = Mathf.Min(1f, baseColor.a + 0.2f);
            }

            graphic.color = Color.Lerp(baseColor, bright, hoverAmount);
        }
    }

    private bool IsHovered(RectTransform rect, Vector2 screenMouse)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenMouse, _uiCamera);
    }

    private void CacheAnchorStates()
    {
        _anchorStates.Clear();
        for (int i = 0; i < targets.Count; i++)
        {
            RectTransform rect = targets[i].rect;
            if (rect == null)
            {
                continue;
            }

            _anchorStates[rect] = new AnchorState { anchoredPosition = rect.anchoredPosition };
        }
    }

    private void CacheBaseColors()
    {
        _baseColors.Clear();
        for (int i = 0; i < targets.Count; i++)
        {
            MotionTarget target = targets[i];
            if (target.rect == null || target.graphics == null)
            {
                continue;
            }

            Color[] colors = new Color[target.graphics.Length];
            for (int g = 0; g < target.graphics.Length; g++)
            {
                colors[g] = target.graphics[g] != null ? target.graphics[g].color : Color.white;
            }

            _baseColors[target.rect] = colors;
        }
    }

    private void AutoWireTargets()
    {
        targets.Clear();
        TryAddTextTarget("TitleText", new Vector2(0.45f, 0.55f), 0f, 0.24f);
        TryAddTextTarget("SubtitleText", new Vector2(0.26f, 0.32f), 1.2f, 0.16f);
        TryAddTextTarget("CoreText", new Vector2(0.22f, 0.28f), 1.7f, 0.20f);
        TryAddTextTarget("MenuLabel", new Vector2(0.25f, 0.35f), 2.1f, 0.2f);
        TryAddButtonTarget("StartButton", new Vector2(0.45f, 0.55f), 2.4f, 0.32f);
        TryAddButtonTarget("QuitButton", new Vector2(0.35f, 0.48f), 3.6f, 0.28f);
    }

    private void TryAddTextTarget(string childName, Vector2 idleAmplitude, float phaseOffset, float hoverBrighten)
    {
        Transform child = transform.Find(childName);
        if (child == null || !child.gameObject.activeInHierarchy)
        {
            return;
        }

        RectTransform rect = child as RectTransform;
        Text text = child.GetComponent<Text>();
        if (rect == null || text == null)
        {
            return;
        }

        text.raycastTarget = true;
        targets.Add(new MotionTarget
        {
            rect = rect,
            graphics = new[] { text },
            idleAmplitude = idleAmplitude,
            phaseOffset = phaseOffset,
            hoverBrighten = hoverBrighten
        });
    }

    private void TryAddButtonTarget(string childName, Vector2 idleAmplitude, float phaseOffset, float hoverBrighten)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            return;
        }

        RectTransform rect = child as RectTransform;
        Image image = child.GetComponent<Image>();
        Text text = child.Find("Text")?.GetComponent<Text>();
        if (rect == null)
        {
            return;
        }

        if (text != null)
        {
            text.raycastTarget = false;
        }

        targets.Add(new MotionTarget
        {
            rect = rect,
            graphics = image != null && text != null
                ? new Graphic[] { image, text }
                : image != null
                    ? new Graphic[] { image }
                    : text != null
                        ? new Graphic[] { text }
                        : Array.Empty<Graphic>(),
            idleAmplitude = idleAmplitude,
            phaseOffset = phaseOffset,
            hoverBrighten = hoverBrighten
        });
    }

    private void TryLoadDisplayFont()
    {
        if (displayFont != null)
        {
            return;
        }

#if UNITY_EDITOR
        displayFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/BoldPixels.ttf");
#endif
    }
}
