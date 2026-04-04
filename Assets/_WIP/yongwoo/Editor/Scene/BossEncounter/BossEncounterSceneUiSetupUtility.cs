using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// 역할:
// - 보스 조우 씬 UI 루트와 컴포넌트 배치를 자동으로 정리하고 참조를 연결합니다.
// - HUD, 대사 패널, 디렉터 사이의 배선을 반복 작업 없이 맞추기 위한 도구입니다.
//
// 구조 포인트:
// - scene-authored UI 구조를 코드와 맞춰주는 셋업 유틸리티입니다.

public static class BossEncounterSceneUiSetupUtility
{
    private const string HudLayoutPrefabPath = "Assets/_WIP/yongwoo/Prefabs/UI/BossEncounter/BossEncounterHudLayout.prefab";
    private const string DialogueLayoutPrefabPath = "Assets/_WIP/yongwoo/Prefabs/UI/BossEncounter/EncounterDialogueLayout.prefab";
    private const string UiRootName = "UI";
    private const string SystemsRootName = "Systems";

    [MenuItem("Tools/Boss Encounter/Apply Scene UI Layouts")]
    public static void ApplySceneUiLayouts()
    {
        BattleHud battleHud = Object.FindFirstObjectByType<BattleHud>();
        EncounterDialoguePanel dialoguePanel = Object.FindFirstObjectByType<EncounterDialoguePanel>();
        BossEncounterDirector encounterDirector = Object.FindFirstObjectByType<BossEncounterDirector>();

        if (battleHud == null || dialoguePanel == null || encounterDirector == null)
        {
            Debug.LogWarning("Boss Encounter UI Setup: BattleHud, EncounterDialoguePanel, BossEncounterDirector가 모두 씬에 있어야 합니다.");
            return;
        }

        GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudLayoutPrefabPath);
        GameObject dialoguePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueLayoutPrefabPath);
        if (hudPrefab == null || dialoguePrefab == null)
        {
            Debug.LogWarning("Boss Encounter UI Setup: 목업 프리팹을 찾지 못했습니다. 먼저 UI Mockup Prefab 생성 메뉴를 실행하세요.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(battleHud.gameObject, "Apply Boss Encounter HUD Layout");
        Undo.RegisterFullObjectHierarchyUndo(dialoguePanel.gameObject, "Apply Encounter Dialogue Layout");

        ClearChildren(battleHud.transform);
        ClearChildren(dialoguePanel.transform);

        battleHud.gameObject.name = "HUD";
        dialoguePanel.gameObject.name = "DialogueUI";

        PrefabUtility.InstantiatePrefab(hudPrefab, battleHud.transform);
        PrefabUtility.InstantiatePrefab(dialoguePrefab, dialoguePanel.transform);

        RemoveChildIfExists(battleHud.transform, "BossSpawnButton");
        ConfigureHudComponent(battleHud);
        ConfigureDialogueComponent(dialoguePanel);
        OrganizeHierarchy(battleHud, dialoguePanel, encounterDirector);
        ConfigureCanvasForScreenUi(battleHud);
        ConfigureCanvasForScreenUi(dialoguePanel);
        ApplyInitialUiVisibility(battleHud, dialoguePanel);
        EnsureDebugPanel(battleHud.transform, encounterDirector, battleHud);
        EnsureEventSystem();
        EnsureDirectorReferences(encounterDirector, battleHud, dialoguePanel);
        DisableTimelinePlayOnAwake(encounterDirector);

        EditorUtility.SetDirty(battleHud);
        EditorUtility.SetDirty(dialoguePanel);
        EditorUtility.SetDirty(encounterDirector);
        EditorSceneManager.MarkSceneDirty(encounterDirector.gameObject.scene);
        AssetDatabase.SaveAssets();

        Debug.Log("Boss Encounter UI Setup: 씬 HUD/대사 UI를 프리팹 배치 구조로 교체하고 디버그 패널을 준비했습니다.");
    }

    private static void ConfigureHudComponent(BattleHud battleHud)
    {
        SerializedObject serializedObject = new SerializedObject(battleHud);
        serializedObject.FindProperty("playerBarRoot").objectReferenceValue = FindChildRecursive(battleHud.transform, "PlayerBar")?.gameObject;
        serializedObject.FindProperty("playerFill").objectReferenceValue = FindChildRecursive(battleHud.transform, "PlayerBar/Fill")?.GetComponent<Image>();
        serializedObject.FindProperty("playerText").objectReferenceValue = FindChildRecursive(battleHud.transform, "PlayerBar/Label")?.GetComponent<Text>();
        serializedObject.FindProperty("bossBarRoot").objectReferenceValue = FindChildRecursive(battleHud.transform, "BossBar")?.gameObject;
        serializedObject.FindProperty("bossFill").objectReferenceValue = FindChildRecursive(battleHud.transform, "BossBar/Fill")?.GetComponent<Image>();
        serializedObject.FindProperty("bossText").objectReferenceValue = FindChildRecursive(battleHud.transform, "BossBar/Label")?.GetComponent<Text>();
        serializedObject.FindProperty("settingsButton").objectReferenceValue = FindChildRecursive(battleHud.transform, "SettingsButton")?.GetComponent<Button>();
        serializedObject.FindProperty("settingsButtonText").objectReferenceValue = FindChildRecursive(battleHud.transform, "SettingsButton/Label")?.GetComponent<Text>();
        serializedObject.FindProperty("settingsPanelRoot").objectReferenceValue = FindChildRecursive(battleHud.transform, "InputSettingsPanel") as RectTransform;
        serializedObject.FindProperty("settingsPanel").objectReferenceValue = FindChildRecursive(battleHud.transform, "InputSettingsPanel")?.GetComponent<GameInputSettingsPanel>();
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(battleHud);
    }

    private static void ConfigureDialogueComponent(EncounterDialoguePanel dialoguePanel)
    {
        SerializedObject serializedObject = new SerializedObject(dialoguePanel);
        serializedObject.FindProperty("panelRoot").objectReferenceValue = FindChildRecursive(dialoguePanel.transform, "DialogueRoot") as RectTransform;
        serializedObject.FindProperty("leftPortrait").objectReferenceValue = FindChildRecursive(dialoguePanel.transform, "DialogueRoot/LeftPortrait/Portrait")?.GetComponent<Image>();
        serializedObject.FindProperty("rightPortrait").objectReferenceValue = FindChildRecursive(dialoguePanel.transform, "DialogueRoot/RightPortrait/Portrait")?.GetComponent<Image>();
        serializedObject.FindProperty("nameText").objectReferenceValue = FindChildRecursive(dialoguePanel.transform, "DialogueRoot/NamePlate/Name")?.GetComponent<Text>();
        serializedObject.FindProperty("bodyText").objectReferenceValue = FindChildRecursive(dialoguePanel.transform, "DialogueRoot/Body/BodyText")?.GetComponent<Text>();
        serializedObject.FindProperty("hintText").objectReferenceValue = FindChildRecursive(dialoguePanel.transform, "DialogueRoot/Hint/HintText")?.GetComponent<Text>();
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dialoguePanel);
    }

    private static void EnsureDirectorReferences(BossEncounterDirector encounterDirector, BattleHud battleHud, EncounterDialoguePanel dialoguePanel)
    {
        SerializedObject serializedObject = new SerializedObject(encounterDirector);
        serializedObject.FindProperty("battleHud").objectReferenceValue = battleHud;
        serializedObject.FindProperty("dialoguePanel").objectReferenceValue = dialoguePanel;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplyInitialUiVisibility(BattleHud battleHud, EncounterDialoguePanel dialoguePanel)
    {
        SetChildActive(battleHud.transform, "PlayerBar", true);
        SetChildActive(battleHud.transform, "BossBar", false);
        SetChildActive(battleHud.transform, "InputSettingsPanel", false);
        SetChildActive(dialoguePanel.transform, "DialogueRoot", false);
    }

    private static void EnsureDebugPanel(Transform parent, BossEncounterDirector encounterDirector, BattleHud battleHud)
    {
        RemoveAllChildrenByName("BossEncounterDebugPanel");

        if (parent == null)
        {
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform panel = CreateRect(
            "BossEncounterDebugPanel",
            parent,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(32f, -84f),
            new Vector2(260f, 148f));
        panel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.94f);

        BossEncounterDebugPanel debugPanel = panel.gameObject.AddComponent<BossEncounterDebugPanel>();
        SerializedObject panelSerializedObject = new SerializedObject(debugPanel);
        panelSerializedObject.FindProperty("encounterDirector").objectReferenceValue = encounterDirector;
        panelSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        Text title = CreateAnchoredText("Title", panel, font, 18, TextAnchor.MiddleLeft, new Vector2(16f, -14f), new Vector2(160f, 24f), "디버그 패널");
        title.color = new Color(0.95f, 0.97f, 1f, 1f);
        CreateAnchoredText("StateText", panel, font, 15, TextAnchor.MiddleLeft, new Vector2(16f, -42f), new Vector2(220f, 22f), "상태: 대기");
        CreateButton("EncounterActionButton", panel, font, "조우 시작", new Vector2(16f, -78f), new Vector2(228f, 36f));
    }

    private static void EnsureEventSystem()
    {
        EventSystem existing = Object.FindFirstObjectByType<EventSystem>();
        if (existing != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
        Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create EventSystem");
#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private static void OrganizeHierarchy(BattleHud battleHud, EncounterDialoguePanel dialoguePanel, BossEncounterDirector encounterDirector)
    {
        GameObject uiRoot = GetOrCreateRoot(UiRootName);
        GameObject systemsRoot = GetOrCreateRoot(SystemsRootName);

        Undo.SetTransformParent(battleHud.transform, uiRoot.transform, "Parent HUD Under UI");
        Undo.SetTransformParent(dialoguePanel.transform, uiRoot.transform, "Parent Dialogue Under UI");
        dialoguePanel.transform.SetSiblingIndex(1);

        DialogueManager dialogueManager = Object.FindFirstObjectByType<DialogueManager>();
        if (dialogueManager != null)
        {
            Undo.SetTransformParent(dialogueManager.transform, systemsRoot.transform, "Parent DialogueManager Under Systems");
        }

        Undo.SetTransformParent(encounterDirector.transform, systemsRoot.transform, "Parent BossEncounterDirector Under Systems");

        StretchRectTransform(battleHud.transform as RectTransform);
        StretchRectTransform(dialoguePanel.transform as RectTransform);
    }

    private static void DisableTimelinePlayOnAwake(BossEncounterDirector encounterDirector)
    {
        if (encounterDirector == null)
        {
            return;
        }

        SerializedObject directorSerializedObject = new SerializedObject(encounterDirector);
        Object introDirectorObject = directorSerializedObject.FindProperty("introTimeline").objectReferenceValue;
        Object victoryDirectorObject = directorSerializedObject.FindProperty("victoryTimeline").objectReferenceValue;

        SetDirectorInitialState(introDirectorObject as PlayableDirector);
        SetDirectorInitialState(victoryDirectorObject as PlayableDirector);
    }

    private static void SetDirectorInitialState(PlayableDirector director)
    {
        if (director == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(director);
        SerializedProperty initialState = serializedObject.FindProperty("m_InitialState");
        if (initialState != null)
        {
            initialState.intValue = 0;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ConfigureCanvasForScreenUi(Component component)
    {
        if (component == null)
        {
            return;
        }

        Canvas canvas = component.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = component is EncounterDialoguePanel ? 1200 : 1000;
        }

        CanvasScaler scaler = component.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        StretchRectTransform(component.transform as RectTransform);
    }

    private static GameObject GetOrCreateRoot(string objectName)
    {
        GameObject root = GameObject.Find(objectName);
        if (root != null)
        {
            return root;
        }

        root = new GameObject(objectName);
        Undo.RegisterCreatedObjectUndo(root, $"Create {objectName}");
        return root;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
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
        Undo.RegisterCreatedObjectUndo(root, $"Create {objectName}");
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private static Text CreateAnchoredText(
        string objectName,
        Transform parent,
        Font font,
        int fontSize,
        TextAnchor alignment,
        Vector2 anchoredPosition,
        Vector2 size,
        string defaultText)
    {
        RectTransform rect = CreateRect(objectName, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, size);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = defaultText;
        return text;
    }

    private static Button CreateButton(string objectName, Transform parent, Font font, string labelText, Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform root = CreateRect(objectName, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, size);
        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.18f, 0.2f, 0.27f, 0.98f);

        Button button = root.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = background.color;
        colors.highlightedColor = new Color(0.24f, 0.27f, 0.35f, 1f);
        colors.pressedColor = new Color(0.12f, 0.14f, 0.18f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.16f, 0.16f, 0.16f, 0.75f);
        button.colors = colors;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        Undo.RegisterCreatedObjectUndo(labelObject, $"Create {objectName} Label");
        labelObject.transform.SetParent(root, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        label.font = font;
        label.fontSize = 16;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = labelText;
        return button;
    }

    private static void StretchRectTransform(RectTransform rectTransform)
    {
        if (rectTransform == null)
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

    private static void SetChildActive(Transform parent, string path, bool active)
    {
        if (parent == null)
        {
            return;
        }

        Transform child = FindChildRecursive(parent, path);
        if (child != null)
        {
            child.gameObject.SetActive(active);
        }
    }

    private static void RemoveChildIfExists(Transform parent, string path)
    {
        if (parent == null)
        {
            return;
        }

        Transform child = FindChildRecursive(parent, path);
        if (child != null)
        {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static Transform FindChildRecursive(Transform parent, string path)
    {
        if (parent == null)
        {
            return null;
        }

        Transform direct = parent.Find(path);
        if (direct != null)
        {
            return direct;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform nested = FindChildRecursive(parent.GetChild(i), path);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static void RemoveAllChildrenByName(string objectName)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate.name != objectName)
            {
                continue;
            }

            Object.DestroyImmediate(candidate.gameObject);
        }
    }
}
