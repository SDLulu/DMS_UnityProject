using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

// 역할:
// - Yongwoo_Title 씬에 타이틀 UI 연출을 전부 bake (Play 없이 인스펙터에서 조절).

public static class TitleUiEffectSetup
{
    private const string TitleScenePath = "Assets/_WIP/yongwoo/Scenes/Yongwoo_Title.unity";
    private const string DisplayFontPath = "Assets/Fonts/BoldPixels.ttf";
    private const string NeonShaderPath = "Assets/_WIP/yongwoo/Art/UI/TitleNeonOverlay.shader";
    private const string MaterialFolderPath = "Assets/_WIP/yongwoo/Art/UI/TitleMaterials";

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
        RemoveBottomPulseLine(titleUi.transform);
        EnsurePremiumObjects(titleUi.transform);
        ApplyLayout(titleUi.transform);
        ApplyShaderMaterials(titleUi.transform);
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
        ApplyFullScreenImage(titleUiRoot.Find("Background"),
            new Color(0.00f, 0.01f, 0.014f, 0.28f));

        ApplyFullScreenImage(titleUiRoot.Find("Atmosphere"),
            new Color(0.00f, 0.04f, 0.05f, 0.08f));

        ApplyRect(titleUiRoot.Find("TitleVideoBackground") as RectTransform,
            Vector2.zero, Vector2.one);

        Transform video = titleUiRoot.Find("TitleVideoBackground");
        Transform background = titleUiRoot.Find("Background");
        Transform atmosphere = titleUiRoot.Find("Atmosphere");
        video?.SetSiblingIndex(0);
        background?.SetSiblingIndex(1);
        atmosphere?.SetSiblingIndex(2);

        ShowSubtitle(titleUiRoot);

        ApplyRect(titleUiRoot.Find("TitleGlowPink") as RectTransform,
            new Vector2(0.18f, 0.81f), new Vector2(0.82f, 0.94f),
            new Vector2(0.5f, 0.5f), new Vector2(5f, -4f), Vector2.zero);

        ApplyRect(titleUiRoot.Find("TitleGlowCyan") as RectTransform,
            new Vector2(0.18f, 0.81f), new Vector2(0.82f, 0.94f),
            new Vector2(0.5f, 0.5f), new Vector2(-4f, 4f), Vector2.zero);

        ApplyRect(titleUiRoot.Find("TitleText") as RectTransform,
            new Vector2(0.18f, 0.81f), new Vector2(0.82f, 0.94f));

        ApplyRect(titleUiRoot.Find("SubtitleText") as RectTransform,
            new Vector2(0.25f, 0.745f), new Vector2(0.75f, 0.79f));

        ApplyRect(titleUiRoot.Find("CoreText") as RectTransform,
            new Vector2(0.43f, 0.61f), new Vector2(0.57f, 0.68f));

        ApplyRect(titleUiRoot.Find("MenuLabel") as RectTransform,
            new Vector2(0.38f, 0.39f), new Vector2(0.62f, 0.43f));

        ApplyRect(titleUiRoot.Find("BuildTag") as RectTransform,
            new Vector2(0.055f, 0.045f), new Vector2(0.35f, 0.09f));

        ApplyRect(titleUiRoot.Find("TopStatus") as RectTransform,
            new Vector2(0.64f, 0.91f), new Vector2(0.94f, 0.955f));

        ApplyRect(titleUiRoot.Find("StartButton") as RectTransform,
            new Vector2(0.5f, 0.30f), new Vector2(0.5f, 0.30f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(430f, 62f));

        ApplyRect(titleUiRoot.Find("QuitButton") as RectTransform,
            new Vector2(0.5f, 0.205f), new Vector2(0.5f, 0.205f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(330f, 52f));

        ApplyMinimalButtonStyle(titleUiRoot.Find("StartButton"),
            new Color(0.04f, 0.28f, 0.28f, 0.36f),
            new Color(0.94f, 1f, 0.99f, 1f));

        ApplyMinimalButtonStyle(titleUiRoot.Find("QuitButton"),
            new Color(0.02f, 0.04f, 0.055f, 0.26f),
            new Color(0.82f, 0.90f, 0.92f, 0.92f));

        ApplyDecorativeRects(titleUiRoot);
        MoveReadableContentToFront(titleUiRoot);
    }

    private static void EnsurePremiumObjects(Transform titleUiRoot)
    {
        EnsureImage(titleUiRoot, "Atmosphere");
        EnsureImage(titleUiRoot, "LeftPanel");
        EnsureImage(titleUiRoot, "MenuPanel");
        EnsureImage(titleUiRoot, "TitleRail");
        EnsureImage(titleUiRoot, "MenuRule");
        EnsureImage(titleUiRoot, "TopRule");
        EnsureImage(titleUiRoot, "BottomRule");
        EnsureImage(titleUiRoot, "CornerTL");
        EnsureImage(titleUiRoot, "CornerBR");
        EnsureImage(titleUiRoot, "DataTick01");
        EnsureImage(titleUiRoot, "DataTick02");
        EnsureImage(titleUiRoot, "DataTick03");
        EnsureImage(titleUiRoot, "CoreFrame");
        EnsureImage(titleUiRoot, "CoreFrameGlow");
        EnsureImage(titleUiRoot, "CoreLineLeft");
        EnsureImage(titleUiRoot, "CoreLineRight");
        EnsureImage(titleUiRoot, "CoreStemTop");
        EnsureImage(titleUiRoot, "CoreStemBottom");
        EnsureImage(titleUiRoot, "CoreCircuitL1");
        EnsureImage(titleUiRoot, "CoreCircuitL2");
        EnsureImage(titleUiRoot, "CoreCircuitR1");
        EnsureImage(titleUiRoot, "CoreCircuitR2");
        EnsureImage(titleUiRoot, "GlitchPink01");
        EnsureImage(titleUiRoot, "GlitchPink02");
        EnsureImage(titleUiRoot, "GlitchPink03");
        EnsureImage(titleUiRoot, "GlitchPink04");
        EnsureImage(titleUiRoot, "GlitchCyan01");
        EnsureImage(titleUiRoot, "GlitchCyan02");
        EnsureText(titleUiRoot, "TitleGlowPink");
        EnsureText(titleUiRoot, "TitleGlowCyan");
        EnsureText(titleUiRoot, "CoreText");
        EnsureText(titleUiRoot, "MenuLabel");
        EnsureText(titleUiRoot, "BuildTag");
        EnsureText(titleUiRoot, "TopStatus");
    }

    private static void ShowSubtitle(Transform titleUiRoot)
    {
        Transform subtitle = titleUiRoot.Find("SubtitleText");
        if (subtitle == null)
        {
            return;
        }

        subtitle.gameObject.SetActive(true);
        EditorUtility.SetDirty(subtitle.gameObject);
    }

    private static void ApplyShaderMaterials(Transform titleUiRoot)
    {
        Material cyan = GetOrCreateTitleMaterial(
            "TitleNeon_Cyan",
            new Color(1f, 1f, 1f, 1f),
            new Color(0.05f, 0.95f, 1f, 1f),
            0.75f,
            0.18f,
            96f,
            0.75f,
            0.55f,
            0.42f,
            0.05f);

        Material pink = GetOrCreateTitleMaterial(
            "TitleNeon_Pink",
            new Color(1f, 1f, 1f, 1f),
            new Color(1f, 0.10f, 0.65f, 1f),
            0.85f,
            0.25f,
            130f,
            1.1f,
            0.72f,
            -0.65f,
            0.10f);

        Material soft = GetOrCreateTitleMaterial(
            "TitleNeon_SoftOverlay",
            new Color(1f, 1f, 1f, 1f),
            new Color(0.10f, 0.80f, 0.95f, 1f),
            0.28f,
            0.10f,
            72f,
            0.35f,
            0.20f,
            0.22f,
            0.035f);

        AssignMaterial(titleUiRoot, soft, "Background", "Atmosphere");
        AssignMaterial(titleUiRoot, cyan,
            "TopRule", "MenuRule", "DataTick01", "DataTick02", "GlitchCyan01", "GlitchCyan02",
            "CoreFrame", "CoreFrameGlow", "CoreLineLeft", "CoreLineRight", "CoreStemTop",
            "CoreStemBottom", "CoreCircuitL1", "CoreCircuitL2", "CoreCircuitR1", "CoreCircuitR2",
            "TitleGlowCyan", "CoreText", "MenuLabel");
        AssignMaterial(titleUiRoot, pink,
            "BottomRule", "CornerTL", "CornerBR", "DataTick03", "GlitchPink01", "GlitchPink02",
            "GlitchPink03", "GlitchPink04", "TitleGlowPink", "TitleText");
    }

    private static Material GetOrCreateTitleMaterial(
        string materialName,
        Color color,
        Color glowColor,
        float glowStrength,
        float scanStrength,
        float scanDensity,
        float scanSpeed,
        float sweepStrength,
        float sweepSpeed,
        float flickerStrength)
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(NeonShaderPath);
        if (shader == null)
        {
            Debug.LogWarning($"[TitleUiEffectSetup] 셰이더를 찾지 못했습니다: {NeonShaderPath}");
            return null;
        }

        if (!AssetDatabase.IsValidFolder(MaterialFolderPath))
        {
            Directory.CreateDirectory(MaterialFolderPath);
            AssetDatabase.Refresh();
        }

        string path = $"{MaterialFolderPath}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        material.SetColor("_Color", color);
        material.SetColor("_GlowColor", glowColor);
        material.SetFloat("_GlowStrength", glowStrength);
        material.SetFloat("_ScanStrength", scanStrength);
        material.SetFloat("_ScanDensity", scanDensity);
        material.SetFloat("_ScanSpeed", scanSpeed);
        material.SetFloat("_SweepStrength", sweepStrength);
        material.SetFloat("_SweepSpeed", sweepSpeed);
        material.SetFloat("_FlickerStrength", flickerStrength);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void AssignMaterial(Transform root, Material material, params string[] names)
    {
        if (material == null)
        {
            return;
        }

        for (int i = 0; i < names.Length; i++)
        {
            Graphic graphic = root.Find(names[i])?.GetComponent<Graphic>();
            if (graphic == null)
            {
                continue;
            }

            graphic.material = material;
            EditorUtility.SetDirty(graphic);
        }
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

    private static void ApplyDecorativeRects(Transform root)
    {
        ApplyPanel(root.Find("LeftPanel"), new Vector2(0.055f, 0.34f), new Vector2(0.58f, 0.83f),
            new Color(0.02f, 0.06f, 0.07f, 0.00f));
        ApplyPanel(root.Find("MenuPanel"), new Vector2(0.60f, 0.22f), new Vector2(0.93f, 0.60f),
            new Color(0.01f, 0.025f, 0.03f, 0.00f));
        ApplyPanel(root.Find("TitleRail"), new Vector2(0.0f, 0.0f), new Vector2(0.0f, 0.0f),
            new Color(0.13f, 0.95f, 0.82f, 0f));
        ApplyPanel(root.Find("MenuRule"), new Vector2(0.38f, 0.375f), new Vector2(0.62f, 0.379f),
            new Color(0.13f, 0.95f, 0.82f, 0.50f));
        ApplyPanel(root.Find("TopRule"), new Vector2(0.34f, 0.802f), new Vector2(0.66f, 0.806f),
            new Color(0.10f, 0.95f, 1f, 0.42f));
        ApplyPanel(root.Find("BottomRule"), new Vector2(0.27f, 0.105f), new Vector2(0.73f, 0.109f),
            new Color(0.96f, 0.20f, 0.62f, 0.42f));
        ApplyPanel(root.Find("CornerTL"), new Vector2(0.18f, 0.855f), new Vector2(0.27f, 0.861f),
            new Color(1f, 0.16f, 0.70f, 0.70f));
        ApplyPanel(root.Find("CornerBR"), new Vector2(0.73f, 0.855f), new Vector2(0.82f, 0.861f),
            new Color(1f, 0.16f, 0.70f, 0.70f));
        ApplyPanel(root.Find("DataTick01"), new Vector2(0.28f, 0.642f), new Vector2(0.40f, 0.647f),
            new Color(0.13f, 0.95f, 0.82f, 0.44f));
        ApplyPanel(root.Find("DataTick02"), new Vector2(0.60f, 0.642f), new Vector2(0.72f, 0.647f),
            new Color(0.13f, 0.95f, 0.82f, 0.44f));
        ApplyPanel(root.Find("DataTick03"), new Vector2(0.45f, 0.585f), new Vector2(0.55f, 0.589f),
            new Color(0.96f, 0.20f, 0.62f, 0.36f));

        ApplyPanel(root.Find("CoreFrameGlow"), new Vector2(0.455f, 0.61f), new Vector2(0.545f, 0.695f),
            new Color(0.08f, 0.95f, 1f, 0.18f));
        ApplyPanel(root.Find("CoreFrame"), new Vector2(0.465f, 0.62f), new Vector2(0.535f, 0.685f),
            new Color(0.08f, 0.95f, 1f, 0.58f));
        ApplyPanel(root.Find("CoreLineLeft"), new Vector2(0.315f, 0.648f), new Vector2(0.455f, 0.654f),
            new Color(0.08f, 0.95f, 1f, 0.74f));
        ApplyPanel(root.Find("CoreLineRight"), new Vector2(0.545f, 0.648f), new Vector2(0.685f, 0.654f),
            new Color(0.08f, 0.95f, 1f, 0.74f));
        ApplyPanel(root.Find("CoreStemTop"), new Vector2(0.497f, 0.695f), new Vector2(0.503f, 0.735f),
            new Color(0.08f, 0.95f, 1f, 0.64f));
        ApplyPanel(root.Find("CoreStemBottom"), new Vector2(0.497f, 0.575f), new Vector2(0.503f, 0.61f),
            new Color(0.08f, 0.95f, 1f, 0.50f));
        ApplyPanel(root.Find("CoreCircuitL1"), new Vector2(0.36f, 0.675f), new Vector2(0.42f, 0.680f),
            new Color(0.08f, 0.95f, 1f, 0.56f));
        ApplyPanel(root.Find("CoreCircuitL2"), new Vector2(0.39f, 0.620f), new Vector2(0.455f, 0.625f),
            new Color(0.08f, 0.95f, 1f, 0.42f));
        ApplyPanel(root.Find("CoreCircuitR1"), new Vector2(0.58f, 0.675f), new Vector2(0.64f, 0.680f),
            new Color(0.08f, 0.95f, 1f, 0.56f));
        ApplyPanel(root.Find("CoreCircuitR2"), new Vector2(0.545f, 0.620f), new Vector2(0.61f, 0.625f),
            new Color(0.08f, 0.95f, 1f, 0.42f));

        ApplyPanel(root.Find("GlitchPink01"), new Vector2(0.21f, 0.895f), new Vector2(0.27f, 0.901f),
            new Color(1f, 0.16f, 0.70f, 0.86f));
        ApplyPanel(root.Find("GlitchPink02"), new Vector2(0.29f, 0.872f), new Vector2(0.34f, 0.878f),
            new Color(1f, 0.16f, 0.70f, 0.72f));
        ApplyPanel(root.Find("GlitchPink03"), new Vector2(0.67f, 0.895f), new Vector2(0.73f, 0.901f),
            new Color(1f, 0.16f, 0.70f, 0.86f));
        ApplyPanel(root.Find("GlitchPink04"), new Vector2(0.75f, 0.872f), new Vector2(0.80f, 0.878f),
            new Color(1f, 0.16f, 0.70f, 0.72f));
        ApplyPanel(root.Find("GlitchCyan01"), new Vector2(0.405f, 0.795f), new Vector2(0.46f, 0.800f),
            new Color(0.08f, 0.95f, 1f, 0.55f));
        ApplyPanel(root.Find("GlitchCyan02"), new Vector2(0.54f, 0.795f), new Vector2(0.595f, 0.800f),
            new Color(0.08f, 0.95f, 1f, 0.55f));
    }

    private static void MoveReadableContentToFront(Transform root)
    {
        string[] order =
        {
            "LeftPanel", "MenuPanel", "TitleRail", "MenuRule", "TopRule", "BottomRule",
            "CornerTL", "CornerBR", "DataTick01", "DataTick02", "DataTick03",
            "CoreFrameGlow", "CoreFrame", "CoreLineLeft", "CoreLineRight", "CoreStemTop", "CoreStemBottom",
            "CoreCircuitL1", "CoreCircuitL2", "CoreCircuitR1", "CoreCircuitR2",
            "GlitchPink01", "GlitchPink02", "GlitchPink03", "GlitchPink04", "GlitchCyan01", "GlitchCyan02",
            "TitleGlowPink", "TitleGlowCyan", "TitleText", "SubtitleText", "CoreText",
            "MenuLabel", "StartButton", "QuitButton", "BuildTag", "TopStatus"
        };

        for (int i = 0; i < order.Length; i++)
        {
            root.Find(order[i])?.SetAsLastSibling();
        }
    }

    private static void ApplyFullScreenImage(Transform target, Color color)
    {
        if (target == null)
        {
            return;
        }

        ApplyRect(target as RectTransform, Vector2.zero, Vector2.one);
        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
            image.raycastTarget = false;
            EditorUtility.SetDirty(image);
        }
    }

    private static void ApplyPanel(Transform target, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        if (target == null)
        {
            return;
        }

        ApplyRect(target as RectTransform, anchorMin, anchorMax);
        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
            image.raycastTarget = false;
            EditorUtility.SetDirty(image);
        }
    }

    private static Image EnsureImage(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Image found = existing.GetComponent<Image>();
            if (found != null)
            {
                return found;
            }
        }

        GameObject go = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        if (existing == null)
        {
            go.transform.SetParent(parent, false);
        }

        Image image = go.GetComponent<Image>();
        if (image == null)
        {
            image = go.AddComponent<Image>();
        }

        image.raycastTarget = false;
        return image;
    }

    private static Text EnsureText(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Text found = existing.GetComponent<Text>();
            if (found != null)
            {
                return found;
            }
        }

        GameObject go = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        if (existing == null)
        {
            go.transform.SetParent(parent, false);
        }

        Text text = go.GetComponent<Text>();
        if (text == null)
        {
            text = go.AddComponent<Text>();
        }

        text.raycastTarget = false;
        return text;
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

    private static void RemoveBottomPulseLine(Transform titleUiRoot)
    {
        Transform scanLine = titleUiRoot.Find("Background/ScanLine");
        if (scanLine == null)
        {
            return;
        }

        Object.DestroyImmediate(scanLine.gameObject);
    }
}
