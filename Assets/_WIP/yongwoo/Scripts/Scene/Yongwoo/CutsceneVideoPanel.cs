using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

// 역할:
// - 사용자가 나중에 만든 인트로/기억조각/엔딩 영상을 SceneEventSequence에서 재생할 수 있게 하는 화면 레이어입니다.
// - 영상 파일 자체는 이 컴포넌트의 VideoClip 슬롯에만 꽂고, 재생/스킵/검은 배경 UI는 런타임에서 처리합니다.

[DisallowMultipleComponent]
public class CutsceneVideoPanel : MonoBehaviour
{
    [Header("Scene Layout")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private RawImage videoImage;
    [SerializeField] private Image fallbackBackdrop;
    [SerializeField] private Text skipText;

    [Header("Playback")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Color backdropColor = Color.black;
    [SerializeField] private string skipPrompt = "Space / Esc : 건너뛰기";
    [SerializeField, Min(0f)] private float missingClipHoldSeconds = 1.2f;

    private RenderTexture _renderTexture;

    private void Reset()
    {
        TryAutoBind(createMissingObjects: true, createMissingComponents: true);
    }

    private void Awake()
    {
        TryAutoBind(createMissingObjects: true, createMissingComponents: true);
        Hide();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        TryAutoBind(createMissingObjects: false, createMissingComponents: false);
    }

    private void OnDestroy()
    {
        ReleaseRenderTexture();
    }

    public IEnumerator Play(VideoClip clip, bool skippable = true)
    {
        TryAutoBind(createMissingObjects: true, createMissingComponents: true);

        if (clip == null)
        {
            Debug.LogWarning("[CutsceneVideoPanel] VideoClip이 비어 있습니다. 영상 파일을 꽂으면 이 step에서 재생됩니다.", this);
            ShowFallback("[영상 슬롯 비어 있음]");
            if (missingClipHoldSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(missingClipHoldSeconds);
            }
            Hide();
            yield break;
        }

        if (videoPlayer == null || videoImage == null)
        {
            Debug.LogWarning("[CutsceneVideoPanel] 재생 UI를 만들지 못했습니다.", this);
            yield break;
        }

        Show();
        ConfigureRenderTexture();

        videoPlayer.Stop();
        videoPlayer.clip = clip;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = _renderTexture;
        videoImage.texture = _renderTexture;

        if (audioSource != null)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, audioSource);
        }
        else
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        }

        videoPlayer.Prepare();
        float prepareTimeout = 5f;
        while (!videoPlayer.isPrepared && prepareTimeout > 0f)
        {
            prepareTimeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        videoPlayer.Play();
        audioSource?.Play();

        while (videoPlayer != null && videoPlayer.isPlaying)
        {
            if (skippable && WasSkipPressed())
            {
                break;
            }

            yield return null;
        }

        videoPlayer.Stop();
        audioSource?.Stop();
        Hide();
    }

    private void Show()
    {
        if (panelRoot != null)
        {
            EnsureHierarchyActive(panelRoot);
            panelRoot.gameObject.SetActive(true);
        }

        if (panelGroup != null)
        {
            panelGroup.alpha = 1f;
            panelGroup.blocksRaycasts = true;
        }

        if (fallbackBackdrop != null)
        {
            fallbackBackdrop.color = backdropColor;
            fallbackBackdrop.gameObject.SetActive(true);
        }

        if (videoImage != null)
        {
            videoImage.gameObject.SetActive(true);
        }

        if (skipText != null)
        {
            skipText.text = skipPrompt;
            skipText.gameObject.SetActive(true);
        }
    }

    private void ShowFallback(string message)
    {
        Show();
        if (videoImage != null)
        {
            videoImage.gameObject.SetActive(false);
        }

        if (skipText != null)
        {
            skipText.text = message;
            skipText.gameObject.SetActive(true);
        }
    }

    private void Hide()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.blocksRaycasts = false;
        }

        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(false);
        }
    }

    private void TryAutoBind(bool createMissingObjects = true, bool createMissingComponents = true)
    {
        if (targetCanvas == null)
        {
            targetCanvas = FindHudCanvas();
        }

        if (panelRoot == null)
        {
            Transform existing = targetCanvas != null ? targetCanvas.transform.Find("CutsceneVideoRoot") : null;
            panelRoot = existing as RectTransform;
        }

        if (panelRoot == null && targetCanvas != null && createMissingObjects)
        {
            panelRoot = CreatePanelRoot(targetCanvas.transform);
        }

        if (panelRoot == null)
        {
            return;
        }

        if (panelGroup == null)
        {
            panelGroup = panelRoot.GetComponent<CanvasGroup>();
            if (panelGroup == null)
            {
                panelGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (fallbackBackdrop == null)
        {
            fallbackBackdrop = FindChild<Image>(panelRoot, "Backdrop");
        }

        if (videoImage == null)
        {
            videoImage = FindChild<RawImage>(panelRoot, "VideoImage");
        }

        if (skipText == null)
        {
            skipText = FindChild<Text>(panelRoot, "SkipText");
        }

        videoPlayer ??= panelRoot.GetComponent<VideoPlayer>();
        if (videoPlayer == null && createMissingComponents)
        {
            videoPlayer = panelRoot.gameObject.AddComponent<VideoPlayer>();
        }

        audioSource ??= panelRoot.GetComponent<AudioSource>();
        if (audioSource == null && createMissingComponents)
        {
            audioSource = panelRoot.gameObject.AddComponent<AudioSource>();
        }

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
        }

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
        }
    }

    private RectTransform CreatePanelRoot(Transform parent)
    {
        GameObject root = new GameObject("CutsceneVideoRoot", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(parent, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        StretchToParent(rootRect);

        GameObject backdropGo = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backdropGo.transform.SetParent(root.transform, false);
        RectTransform backdropRect = backdropGo.GetComponent<RectTransform>();
        StretchToParent(backdropRect);
        Image backdrop = backdropGo.GetComponent<Image>();
        backdrop.color = backdropColor;

        GameObject imageGo = new GameObject("VideoImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        imageGo.transform.SetParent(root.transform, false);
        RectTransform imageRect = imageGo.GetComponent<RectTransform>();
        StretchToParent(imageRect);
        RawImage rawImage = imageGo.GetComponent<RawImage>();
        rawImage.color = Color.white;

        GameObject skipGo = new GameObject("SkipText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        skipGo.transform.SetParent(root.transform, false);
        RectTransform skipRect = skipGo.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(1f, 0f);
        skipRect.anchorMax = new Vector2(1f, 0f);
        skipRect.pivot = new Vector2(1f, 0f);
        skipRect.anchoredPosition = new Vector2(-28f, 24f);
        skipRect.sizeDelta = new Vector2(360f, 36f);
        Text text = skipGo.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleRight;
        text.fontSize = 18;
        text.color = new Color(0.78f, 0.92f, 1f, 0.76f);
        text.text = skipPrompt;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        fallbackBackdrop = backdrop;
        videoImage = rawImage;
        skipText = text;
        return rootRect;
    }

    private void ConfigureRenderTexture()
    {
        int width = Mathf.Max(16, Screen.width);
        int height = Mathf.Max(16, Screen.height);
        if (_renderTexture != null && _renderTexture.width == width && _renderTexture.height == height)
        {
            return;
        }

        ReleaseRenderTexture();
        _renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            name = "CutsceneVideo_RenderTexture"
        };
        _renderTexture.Create();
    }

    private void ReleaseRenderTexture()
    {
        if (_renderTexture == null)
        {
            return;
        }

        _renderTexture.Release();
        if (Application.isPlaying)
        {
            Destroy(_renderTexture);
        }
        else
        {
            DestroyImmediate(_renderTexture);
        }
        _renderTexture = null;
    }

    private static bool WasSkipPressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null
            && (keyboard.escapeKey.wasPressedThisFrame
                || keyboard.spaceKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame))
        {
            return true;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        return GameInput.Instance.UiClickPressed;
    }

    private static Canvas FindHudCanvas()
    {
        GameObject hud = GameObject.Find("HUD");
        if (hud != null && hud.TryGetComponent(out Canvas hudCanvas))
        {
            return hudCanvas;
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.isRootCanvas)
            {
                return canvas;
            }
        }

        return null;
    }

    private static T FindChild<T>(Transform root, string name) where T : Component
    {
        Transform child = root.Find(name);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void EnsureHierarchyActive(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }

            current = current.parent;
        }
    }
}
