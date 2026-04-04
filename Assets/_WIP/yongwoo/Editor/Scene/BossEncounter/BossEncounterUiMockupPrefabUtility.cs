using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 역할:
// - 보스 조우에 필요한 HUD와 대사 UI 목업 프리팹을 빠르게 생성합니다.
// - 씬에 직접 배치할 UI 골격을 준비하는 제작 출발점입니다.
//
// 구조 포인트:
// - 실제 아트가 들어오기 전에도 구조 실험을 할 수 있게 하는 프리팹 생성기입니다.

public static class BossEncounterUiMockupPrefabUtility
{
    private const string PrefabFolderPath = "Assets/_WIP/yongwoo/Prefabs/UI/BossEncounter";
    private const string DialoguePrefabPath = PrefabFolderPath + "/EncounterDialogueLayout.prefab";
    private const string HudPrefabPath = PrefabFolderPath + "/BossEncounterHudLayout.prefab";

    [MenuItem("Tools/Boss Encounter/Generate UI Mockup Prefabs")]
    public static void GenerateAllMockupPrefabs()
    {
        EnsureFolder(PrefabFolderPath);
        GenerateDialogueMockupPrefab();
        GenerateHudMockupPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Boss Encounter UI: HUD/대사 목업 프리팹을 생성했습니다.");
    }

    [MenuItem("Tools/Boss Encounter/Generate Dialogue UI Mockup Prefab")]
    public static void GenerateDialogueMockupPrefab()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject root = new GameObject("EncounterDialogueLayout", typeof(RectTransform));

        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            RectTransform dialogueRoot = CreateRect(
                "DialogueRoot",
                root.transform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 32f),
                new Vector2(1520f, 260f));
            dialogueRoot.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.94f);

            RectTransform namePlate = CreateRect(
                "NamePlate",
                dialogueRoot,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(28f, -20f),
                new Vector2(260f, 42f));
            namePlate.gameObject.AddComponent<Image>().color = new Color(0.21f, 0.15f, 0.2f, 0.98f);
            CreateText("Name", namePlate, font, 22, TextAnchor.MiddleCenter, "이름");

            RectTransform body = CreateRect(
                "Body",
                dialogueRoot,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                new Vector2(250f, 0f),
                new Vector2(-500f, -88f));
            Text bodyText = CreateText("BodyText", body, font, 28, TextAnchor.UpperLeft, "여기에 대사 본문이 들어갑니다.");
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            bodyText.lineSpacing = 1.15f;

            RectTransform hint = CreateRect(
                "Hint",
                dialogueRoot,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-26f, 16f),
                new Vector2(420f, 24f));
            Text hintText = CreateText("HintText", hint, font, 16, TextAnchor.MiddleRight, "Space/Enter: 다음   Tab/Esc: 전체 스킵");
            hintText.color = new Color(0.78f, 0.84f, 0.95f, 0.9f);

            CreatePortraitRoot("LeftPortrait", dialogueRoot, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 24f));
            CreatePortraitRoot("RightPortrait", dialogueRoot, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f));

            SavePrefab(root, DialoguePrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [MenuItem("Tools/Boss Encounter/Generate HUD UI Mockup Prefab")]
    public static void GenerateHudMockupPrefab()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject root = new GameObject("BossEncounterHudLayout", typeof(RectTransform));

        try
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            CreateBar("PlayerBar", root.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -32f), new Vector2(360f, 34f), new Color(0.16f, 0.18f, 0.24f, 0.95f), new Color(0.24f, 0.88f, 0.58f, 1f), font, "PLAYER");
            CreateBar("BossBar", root.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(640f, 38f), new Color(0.18f, 0.08f, 0.1f, 0.96f), new Color(1f, 0.38f, 0.42f, 1f), font, "BOSS");
            CreateButton("BossSpawnButton", root.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -32f), new Vector2(220f, 42f), font, "조우 시작");
            CreateButton("SettingsButton", root.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -84f), new Vector2(220f, 42f), font, "입력 설정");
            CreateSettingsPanel(root.transform, font);

            SavePrefab(root, HudPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CreateSettingsPanel(Transform parent, Font font)
    {
        RectTransform panel = CreateRect(
            "InputSettingsPanel",
            parent,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-32f, -136f),
            new Vector2(560f, 560f));
        panel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.97f);
        panel.gameObject.AddComponent<GameInputSettingsPanel>();

        Text title = CreateAnchoredText("Title", panel, font, 24, TextAnchor.MiddleLeft, new Vector2(20f, -20f), new Vector2(220f, 32f), "입력 설정");
        title.color = new Color(0.96f, 0.97f, 1f, 1f);
        CreateButton("CloseButton", panel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(92f, 32f), font, "닫기", new Vector2(1f, 1f));
        CreateAnchoredText("SensitivityLabel", panel, font, 18, TextAnchor.MiddleLeft, new Vector2(20f, -68f), new Vector2(160f, 24f), "마우스 감도");
        CreateSlider("SensitivitySlider", panel, new Vector2(180f, -68f), new Vector2(250f, 24f));
        CreateAnchoredText("SensitivityValue", panel, font, 18, TextAnchor.MiddleRight, new Vector2(-20f, -68f), new Vector2(90f, 24f), "1.00", new Vector2(1f, 1f));

        RectTransform bindings = CreateRect(
            "Bindings",
            panel,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(20f, -120f),
            new Vector2(500f, 376f));

        string[] rowNames =
        {
            "MoveUp",
            "MoveDown",
            "MoveLeft",
            "MoveRight",
            "Jump",
            "Crouch",
            "Sprint",
            "Attack",
            "Interact",
            "DialogueAdvance",
            "DialogueSkip"
        };

        string[] rowLabels =
        {
            "이동 위",
            "이동 아래",
            "이동 왼쪽",
            "이동 오른쪽",
            "점프",
            "앉기",
            "대시",
            "공격",
            "상호작용",
            "대화 진행",
            "대화 스킵"
        };

        for (int i = 0; i < rowNames.Length; i++)
        {
            CreateBindingRow(rowNames[i], rowLabels[i], bindings, font, i);
        }

        CreateButton("ResetAllButton", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -520f), new Vector2(130f, 34f), font, "전체 기본값", new Vector2(0f, 1f));
        Text statusText = CreateAnchoredText("StatusText", panel, font, 16, TextAnchor.MiddleRight, new Vector2(-20f, -520f), new Vector2(320f, 26f), string.Empty, new Vector2(1f, 1f));
        statusText.color = new Color(0.82f, 0.87f, 0.95f, 0.95f);
    }

    private static void CreateBindingRow(string rowName, string labelText, RectTransform parent, Font font, int index)
    {
        float y = -34f * index;
        RectTransform row = CreateRect(
            rowName,
            parent,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, y),
            new Vector2(500f, 28f));

        CreateAnchoredText("Label", row, font, 16, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(140f, 24f), labelText);

        Text value = CreateAnchoredText("Value", row, font, 16, TextAnchor.MiddleCenter, new Vector2(158f, 0f), new Vector2(120f, 24f), "-");
        value.color = new Color(0.98f, 0.91f, 0.74f, 1f);

        CreateButton("RebindButton", row, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, 0f), new Vector2(70f, 26f), font, "변경", new Vector2(0f, 1f));
        CreateButton("ResetButton", row, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(382f, 0f), new Vector2(70f, 26f), font, "복구", new Vector2(0f, 1f));
    }

    private static void CreateBar(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size,
        Color backgroundColor,
        Color fillColor,
        Font font,
        string labelText)
    {
        RectTransform root = CreateRect(objectName, parent, anchorMin, anchorMax, new Vector2(anchorMin.x == 0.5f ? 0.5f : 0f, 1f), anchoredPosition, size);
        root.gameObject.AddComponent<Image>().color = backgroundColor;

        RectTransform fill = CreateRect("Fill", root, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(size.x - 8f, size.y - 8f));
        fill.gameObject.AddComponent<Image>().color = fillColor;

        CreateText("Label", root, font, 20, TextAnchor.MiddleCenter, labelText);
    }

    private static RectTransform CreatePortraitRoot(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition)
    {
        RectTransform rect = CreateRect(objectName, parent, anchorMin, anchorMax, pivot, anchoredPosition, new Vector2(200f, 200f));
        rect.gameObject.AddComponent<Image>().color = new Color(0.18f, 0.21f, 0.28f, 0.98f);

        RectTransform portrait = CreateRect("Portrait", rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, -20f));
        Image portraitImage = portrait.gameObject.AddComponent<Image>();
        portraitImage.color = new Color(1f, 1f, 1f, 0.85f);
        portraitImage.preserveAspect = true;
        return rect;
    }

    private static Button CreateButton(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size,
        Font font,
        string labelText,
        Vector2? pivotOverride = null)
    {
        RectTransform root = CreateRect(objectName, parent, anchorMin, anchorMax, pivotOverride ?? new Vector2(1f, 1f), anchoredPosition, size);
        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.14f, 0.16f, 0.2f, 0.96f);

        Button button = root.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = background.color;
        colors.highlightedColor = new Color(0.2f, 0.22f, 0.28f, 1f);
        colors.pressedColor = new Color(0.1f, 0.12f, 0.16f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
        button.colors = colors;

        CreateText("Label", root, font, 18, TextAnchor.MiddleCenter, labelText);
        return button;
    }

    private static Slider CreateSlider(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform root = CreateRect(objectName, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, size);
        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;

        RectTransform background = CreateRect("Background", root, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        background.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.22f, 0.28f, 0.98f);

        RectTransform fillArea = CreateRect("Fill Area", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        fillArea.offsetMin = new Vector2(6f, 6f);
        fillArea.offsetMax = new Vector2(-6f, -6f);

        RectTransform fill = CreateRect("Fill", fillArea, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        fill.gameObject.AddComponent<Image>().color = new Color(0.95f, 0.74f, 0.34f, 1f);

        RectTransform handleSlideArea = CreateRect("Handle Slide Area", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        handleSlideArea.offsetMin = new Vector2(10f, 0f);
        handleSlideArea.offsetMax = new Vector2(-10f, 0f);

        RectTransform handle = CreateRect("Handle", handleSlideArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 28f));
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = new Color(0.95f, 0.95f, 0.98f, 1f);

        slider.targetGraphic = handleImage;
        slider.fillRect = fill;
        slider.handleRect = handle;
        return slider;
    }

    private static Text CreateText(string objectName, Transform parent, Font font, int fontSize, TextAnchor alignment, string defaultText)
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
        text.color = Color.white;
        text.text = defaultText;
        return text;
    }

    private static Text CreateAnchoredText(
        string objectName,
        Transform parent,
        Font font,
        int fontSize,
        TextAnchor alignment,
        Vector2 anchoredPosition,
        Vector2 size,
        string defaultText,
        Vector2? pivotOverride = null)
    {
        RectTransform rect = CreateRect(objectName, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), pivotOverride ?? new Vector2(0f, 1f), anchoredPosition, size);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = defaultText;
        return text;
    }

    private static RectTransform CreateRect(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
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

    private static void SavePrefab(GameObject root, string prefabPath)
    {
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), folderPath);
        Directory.CreateDirectory(fullPath);
        AssetDatabase.Refresh();
    }
}
