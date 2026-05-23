using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 역할:
// - Yongwoo_Title 씬에 타이틀 UI 연출을 전부 bake (Play 없이 인스펙터에서 조절).

public static class TitleUiEffectSetup
{
    private const string TitleScenePath = "Assets/_WIP/yongwoo/Scenes/Yongwoo_Title.unity";
    private const string DisplayFontPath = "Assets/Fonts/BoldPixels.ttf";

    [MenuItem("DMS/Yongwoo/Setup Title UI Effects")]
    public static void SetupTitleUiEffects()
    {
        Scene scene = OpenTitleScene();
        if (!scene.IsValid())
        {
            return;
        }

        GameObject titleUi = GameObject.Find("TitleUI");
        if (titleUi == null)
        {
            Debug.LogError("[TitleUiEffectSetup] TitleUI 오브젝트를 찾을 수 없습니다.");
            return;
        }

        ApplyToTitleUi(titleUi);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[TitleUiEffectSetup] 씬 bake 완료. Ctrl+S 또는 저장 메뉴로 저장하세요.");
    }

    public static void ApplyToTitleUi(GameObject titleUi)
    {
        RemoveLegacyEcgLine(titleUi.transform);
        ApplyLayout(titleUi.transform);
        ApplyScanLine(titleUi.transform);
        ApplyTextMotion(titleUi);
    }

    private static Scene OpenTitleScene()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.IsValid() && scene.path == TitleScenePath)
        {
            return scene;
        }

        if (!System.IO.File.Exists(TitleScenePath))
        {
            Debug.LogError($"[TitleUiEffectSetup] 씬을 찾을 수 없습니다: {TitleScenePath}");
            return default;
        }

        return EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
    }

    private static void ApplyLayout(Transform titleUiRoot)
    {
        HideSubtitle(titleUiRoot);

        ApplyRect(titleUiRoot.Find("TitleText") as RectTransform,
            new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.84f));

        ApplyRect(titleUiRoot.Find("StartButton") as RectTransform,
            new Vector2(0.5f, 0.36f), new Vector2(0.5f, 0.36f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 68f));

        ApplyRect(titleUiRoot.Find("QuitButton") as RectTransform,
            new Vector2(0.5f, 0.26f), new Vector2(0.5f, 0.26f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 60f));

        ApplyMinimalButtonStyle(titleUiRoot.Find("StartButton"),
            new Color(0.10f, 0.40f, 0.36f, 0.28f),
            new Color(0.94f, 1f, 0.99f, 1f));

        ApplyMinimalButtonStyle(titleUiRoot.Find("QuitButton"),
            new Color(0.12f, 0.14f, 0.16f, 0.22f),
            new Color(0.82f, 0.90f, 0.92f, 0.92f));
    }

    private static void HideSubtitle(Transform titleUiRoot)
    {
        Transform subtitle = titleUiRoot.Find("SubtitleText");
        if (subtitle == null)
        {
            return;
        }

        Text text = subtitle.GetComponent<Text>();
        if (text != null)
        {
            text.text = string.Empty;
            EditorUtility.SetDirty(text);
        }

        subtitle.gameObject.SetActive(false);
        EditorUtility.SetDirty(subtitle.gameObject);
    }

    private static void ApplyScanLine(Transform titleUiRoot)
    {
        Transform scanLine = titleUiRoot.Find("Background/ScanLine");
        if (scanLine == null)
        {
            return;
        }

        RectTransform rt = scanLine.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.10f, 0.51f);
            rt.anchorMax = new Vector2(0.90f, 0.57f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
            EditorUtility.SetDirty(rt);
        }

        Image image = scanLine.GetComponent<Image>();
        if (image != null)
        {
            Object.DestroyImmediate(image);
        }

        TitleEcgLineGraphic graphic = scanLine.GetComponent<TitleEcgLineGraphic>();
        if (graphic == null)
        {
            graphic = scanLine.gameObject.AddComponent<TitleEcgLineGraphic>();
        }

        graphic.raycastTarget = false;
        ApplyDefaultEcgSettings(graphic);
        EditorUtility.SetDirty(graphic);
    }

    private static void ApplyDefaultEcgSettings(TitleEcgLineGraphic graphic)
    {
        SerializedObject so = new SerializedObject(graphic);
        so.FindProperty("waveStyle").enumValueIndex = (int)TitleWaveStyle.Random;
        so.FindProperty("scrollSpeed").floatValue = 110f;
        so.FindProperty("cycleSeconds").floatValue = 1.35f;
        so.FindProperty("waveAmplitude").floatValue = 0.36f;
        so.FindProperty("sampleCount").intValue = 180;
        so.FindProperty("smoothPasses").intValue = 3;
        so.FindProperty("controlPointsPerCycle").intValue = 9;
        so.FindProperty("patternSeed").intValue = 20260523;
        so.FindProperty("randomMin").floatValue = -0.12f;
        so.FindProperty("randomMax").floatValue = 1f;
        so.FindProperty("lineThickness").floatValue = 5f;
        so.FindProperty("glowThickness").floatValue = 14f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplyTextMotion(GameObject titleUiRoot)
    {
        TitleUiMotion motion = titleUiRoot.GetComponent<TitleUiMotion>();
        if (motion == null)
        {
            motion = titleUiRoot.AddComponent<TitleUiMotion>();
        }

        Font font = AssetDatabase.LoadAssetAtPath<Font>(DisplayFontPath);
        SerializedObject so = new SerializedObject(motion);
        if (font != null)
        {
            so.FindProperty("displayFont").objectReferenceValue = font;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        motion.ApplyEditorSceneSetup();
    }

    private static void ApplyMinimalButtonStyle(Transform button, Color bg, Color label)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = bg;
            EditorUtility.SetDirty(image);
        }

        Text text = button.Find("Text")?.GetComponent<Text>();
        if (text != null)
        {
            text.color = label;
            EditorUtility.SetDirty(text);
        }

        Button uiButton = button.GetComponent<Button>();
        if (uiButton == null)
        {
            return;
        }

        ColorBlock colors = uiButton.colors;
        colors.normalColor = bg;
        colors.highlightedColor = new Color(
            Mathf.Min(bg.r + 0.14f, 1f),
            Mathf.Min(bg.g + 0.14f, 1f),
            Mathf.Min(bg.b + 0.14f, 1f),
            Mathf.Min(bg.a + 0.18f, 0.55f));
        colors.pressedColor = new Color(bg.r * 0.85f, bg.g * 0.85f, bg.b * 0.85f, bg.a);
        colors.selectedColor = colors.highlightedColor;
        uiButton.colors = colors;
        EditorUtility.SetDirty(uiButton);
    }

    private static void ApplyRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (rt == null)
        {
            return;
        }

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localScale = Vector3.one;
        EditorUtility.SetDirty(rt);
    }

    private static void ApplyRect(
        RectTransform rt,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        if (rt == null)
        {
            return;
        }

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        rt.localScale = Vector3.one;
        EditorUtility.SetDirty(rt);
    }

    private static void RemoveLegacyEcgLine(Transform titleUiRoot)
    {
        Transform legacy = titleUiRoot.Find("EcgLine");
        if (legacy == null)
        {
            return;
        }

        Object.DestroyImmediate(legacy.gameObject);
    }
}
