using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PauseUiInstaller
{
    private const string StagePath = "Assets/_WIP/yongwoo/Scenes/Yongwoo_Stage.unity";
    private const string TitlePath = "Assets/_WIP/yongwoo/Scenes/Yongwoo_Title.unity";
    private const string StageSceneName = "Yongwoo_Stage";
    private const string TitleSceneName = "Yongwoo_Title";

    public static string Install()
    {
        EnsureStagePauseUi();
        EnsureTitleScene();
        EditorSceneManager.OpenScene(StagePath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        return "Pause UI installed and title scene created.";
    }

    private static void EnsureStagePauseUi()
    {
        EditorSceneManager.OpenScene(StagePath, OpenSceneMode.Single);
        Scene scene = EditorSceneManager.GetActiveScene();

        GameObject uiRoot = GameObject.Find("UI") ?? new GameObject("UI");
        GameObject hud = GameObject.Find("HUD");
        if (hud == null)
        {
            hud = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            hud.transform.SetParent(uiRoot.transform, false);
            Canvas canvas = hud.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        Transform hudTransform = hud.transform;
        Button pauseButton = MakeButton(
            hudTransform,
            "PauseButton",
            "II",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-28f, -24f),
            new Vector2(64f, 56f),
            new Color(0.04f, 0.06f, 0.07f, 0.86f),
            new Color(0.75f, 1f, 0.94f, 1f),
            25);

        GameObject panelRoot = FindChildRecursive(hudTransform, "PauseMenuRoot");
        if (panelRoot == null)
        {
            panelRoot = new GameObject("PauseMenuRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        }
        SetRect(panelRoot, hudTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        AddImage(panelRoot, new Color(0.01f, 0.015f, 0.018f, 0.72f));
        CanvasGroup group = panelRoot.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
        panelRoot.SetActive(true);

        GameObject card = panelRoot.transform.Find("Panel")?.gameObject;
        if (card == null)
        {
            card = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        }
        SetRect(card, panelRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 360f));
        AddImage(card, new Color(0.05f, 0.075f, 0.085f, 0.96f));

        GameObject title = card.transform.Find("TitleText")?.gameObject;
        if (title == null)
        {
            title = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        }
        SetRect(title, card.transform, new Vector2(0.08f, 1f), new Vector2(0.92f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(0f, 72f));
        AddText(title, "PAUSED", 42, TextAnchor.MiddleCenter, new Color(0.83f, 1f, 0.96f, 1f));

        GameObject subtitle = card.transform.Find("SubtitleText")?.gameObject;
        if (subtitle == null)
        {
            subtitle = new GameObject("SubtitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        }
        SetRect(subtitle, card.transform, new Vector2(0.1f, 1f), new Vector2(0.9f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(0f, 42f));
        AddText(subtitle, "ESC 또는 게임 재개 버튼으로 복귀", 20, TextAnchor.MiddleCenter, new Color(0.65f, 0.78f, 0.78f, 1f));

        Button resumeButton = MakeButton(
            card.transform,
            "ResumeButton",
            "게임 재개",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -28f),
            new Vector2(300f, 58f),
            new Color(0.08f, 0.42f, 0.38f, 1f),
            Color.white,
            24);

        Button titleButton = MakeButton(
            card.transform,
            "TitleButton",
            "타이틀로",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -104f),
            new Vector2(300f, 54f),
            new Color(0.17f, 0.19f, 0.22f, 1f),
            new Color(0.9f, 0.95f, 0.96f, 1f),
            22);

        PauseMenuController controller = uiRoot.GetComponent<PauseMenuController>() ?? uiRoot.AddComponent<PauseMenuController>();
        SetPrivateObjectReference(controller, "pausePanelRoot", panelRoot);
        SetPrivateObjectReference(controller, "pauseButton", pauseButton);
        SetPrivateObjectReference(controller, "resumeButton", resumeButton);
        SetPrivateObjectReference(controller, "titleButton", titleButton);
        SetPrivateString(controller, "titleSceneName", TitleSceneName);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnsureTitleScene()
    {
        Scene titleScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        Camera cam = camera.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.015f, 0.02f, 0.025f, 1f);
        cam.orthographic = true;
        camera.transform.position = new Vector3(0f, 0f, -10f);

        GameObject canvasGo = new GameObject("TitleUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        SetRect(bg, canvasGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        AddImage(bg, new Color(0.02f, 0.028f, 0.032f, 1f));

        GameObject scanLine = new GameObject("ScanLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        SetRect(scanLine, bg.transform, new Vector2(0.10f, 0.51f), new Vector2(0.90f, 0.57f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        AddImage(scanLine, new Color(0.16f, 0.95f, 0.82f, 0.45f));

        GameObject title = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        SetRect(title, canvasGo.transform, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.84f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        AddText(title, "DEEP DIVE: HOME", 104, TextAnchor.MiddleCenter, new Color(0.90f, 1f, 0.98f, 1f));

        Button startButton = MakeButton(
            canvasGo.transform,
            "StartButton",
            "게임 시작",
            new Vector2(0.5f, 0.36f),
            new Vector2(0.5f, 0.36f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(480f, 68f),
            new Color(0.10f, 0.40f, 0.36f, 0.28f),
            Color.white,
            38);

        Button quitButton = MakeButton(
            canvasGo.transform,
            "QuitButton",
            "종료",
            new Vector2(0.5f, 0.26f),
            new Vector2(0.5f, 0.26f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(420f, 60f),
            new Color(0.12f, 0.14f, 0.16f, 0.22f),
            new Color(0.88f, 0.96f, 0.97f, 0.95f),
            34);

        TitleSceneController titleController = canvasGo.AddComponent<TitleSceneController>();
        SetPrivateObjectReference(titleController, "startButton", startButton);
        SetPrivateObjectReference(titleController, "quitButton", quitButton);
        SetPrivateString(titleController, "stageSceneName", StageSceneName);

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.GetComponent<EventSystem>().SetSelectedGameObject(startButton.gameObject);

        TitleUiEffectSetup.ApplyToTitleUi(canvasGo);
        EditorSceneManager.SaveScene(titleScene, TitlePath);

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        string[] requiredPaths = { TitlePath, StagePath };
        var sceneList = scenes.ToList();
        foreach (string path in requiredPaths)
        {
            if (sceneList.All(s => s.path != path))
            {
                sceneList.Add(new EditorBuildSettingsScene(path, true));
            }
        }
        EditorBuildSettings.scenes = sceneList.ToArray();
    }

    private static Button MakeButton(
        Transform parent,
        string name,
        string labelText,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 pos,
        Vector2 size,
        Color bg,
        Color textColor,
        int fontSize)
    {
        GameObject buttonGo = FindChildRecursive(parent, name);
        if (buttonGo == null)
        {
            buttonGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        }
        SetRect(buttonGo, parent, anchorMin, anchorMax, pivot, pos, size);
        Image image = AddImage(buttonGo, bg);
        Button button = buttonGo.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = bg;
        colors.highlightedColor = new Color(Mathf.Min(bg.r + 0.12f, 1f), Mathf.Min(bg.g + 0.12f, 1f), Mathf.Min(bg.b + 0.12f, 1f), bg.a);
        colors.pressedColor = new Color(Mathf.Max(bg.r - 0.08f, 0f), Mathf.Max(bg.g - 0.08f, 0f), Mathf.Max(bg.b - 0.08f, 0f), bg.a);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        GameObject textGo = buttonGo.transform.Find("Text")?.gameObject;
        if (textGo == null)
        {
            textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        }
        SetRect(textGo, buttonGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        AddText(textGo, labelText, fontSize, TextAnchor.MiddleCenter, textColor);
        return button;
    }

    private static RectTransform SetRect(GameObject go, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
        rt.localScale = Vector3.one;
        return rt;
    }

    private static Text AddText(GameObject go, string text, int fontSize, TextAnchor alignment, Color color)
    {
        Text label = go.GetComponent<Text>() ?? go.AddComponent<Text>();
        label.text = text;
        label.font = LoadFont();
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static Image AddImage(GameObject go, Color color)
    {
        Image image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Font LoadFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        try
        {
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        catch (System.ArgumentException)
        {
            return null;
        }
    }

    private static GameObject FindChildRecursive(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == name)
        {
            return root.gameObject;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            GameObject found = FindChildRecursive(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void SetPrivateObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPrivateString(Object target, string propertyName, string value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            return;
        }

        prop.stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
