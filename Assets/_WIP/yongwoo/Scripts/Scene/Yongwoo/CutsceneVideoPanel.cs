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
    public static bool IsAnyPlaying { get; private set; }

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
    [SerializeField] private bool muteAudio;
    [SerializeField, Range(0f, 1f)] private float videoVolume = 1f;

    [Header("Story Video Audio")]
    [SerializeField] private bool useStoryVideoAudioOverrides = true;
    [SerializeField] private bool memory01Muted;
    [SerializeField, Range(0f, 1f)] private float memory01Volume = 0.5f;
    [SerializeField] private bool memory02Muted;
    [SerializeField, Range(0f, 1f)] private float memory02Volume = 0.5f;
    [SerializeField] private bool bossDefeatMuted;
    [SerializeField, Range(0f, 1f)] private float bossDefeatVolume = 0.5f;

    [SerializeField] private YongwooVideoLayoutMode videoLayoutMode = YongwooVideoLayoutMode.ManualRect;
    [SerializeField] private bool videoPreserveAspect = true;
    [SerializeField, Min(0.1f)] private float videoManualAspect = 1.7778f;
    [SerializeField, Range(0.25f, 2f)] private float videoScale = 1f;
    [SerializeField] private Color backdropColor = Color.black;
    [SerializeField] private string skipPrompt = "SPACE : SKIP";
    [SerializeField, Min(0f)] private float missingClipHoldSeconds = 1.2f;

    private RenderTexture _renderTexture;
    private bool _hasActiveStoryVideoKey;
    private YongwooStoryVideoKey _activeStoryVideoKey;
    private bool _isRegisteredAsPlaying;

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
        MarkNotPlaying();
        ReleaseRenderTexture();
    }

    private void Update()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            GetEffectiveAudioSettings(out bool muted, out float volume);
            YongwooVideoLayoutUtility.ApplyAudioLive(videoPlayer, audioSource, muted, volume);
        }
    }

    public void SetAudioSettings(bool muted, float volume)
    {
        muteAudio = muted;
        videoVolume = Mathf.Clamp01(volume);
        YongwooVideoLayoutUtility.ApplyAudioLive(videoPlayer, audioSource, muteAudio, videoVolume);
    }

    public void SetStoryVideoKey(YongwooStoryVideoKey key)
    {
        _activeStoryVideoKey = key;
        _hasActiveStoryVideoKey = true;
    }

    public void ClearStoryVideoKey()
    {
        _hasActiveStoryVideoKey = false;
    }

    public IEnumerator Play(VideoClip clip, bool skippable = true)
    {
        TryAutoBind(createMissingObjects: true, createMissingComponents: true);

        if (clip == null)
        {
            ClearStoryVideoKey();
            Debug.LogWarning($"[CutsceneVideoPanel] VideoClip이 비어 있습니다. {YongwooStoryVideoClips.GetImportGuide()}", this);
            ShowFallback("[영상 슬롯 비어 있음]");
            MarkPlaying();
            if (missingClipHoldSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(missingClipHoldSeconds);
            }
            MarkNotPlaying();
            Hide();
            yield break;
        }

        if (videoPlayer == null || videoImage == null)
        {
            Debug.LogWarning("[CutsceneVideoPanel] 재생 UI를 만들지 못했습니다.", this);
            yield break;
        }

        Show();
        MarkPlaying();
        ConfigureRenderTexture();

        videoPlayer.Stop();
        videoPlayer.clip = clip;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = _renderTexture;
        videoImage.texture = _renderTexture;

        GetEffectiveAudioSettings(out bool muted, out float volume);
        YongwooVideoLayoutUtility.ConfigureAudio(videoPlayer, audioSource, muted, volume);

        videoPlayer.Prepare();
        float prepareTimeout = 5f;
        while (!videoPlayer.isPrepared && prepareTimeout > 0f)
        {
            prepareTimeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        YongwooVideoLayoutUtility.Apply(
            videoImage.rectTransform,
            clip,
            videoLayoutMode,
            videoScale,
            videoPreserveAspect,
            videoManualAspect);
        GetEffectiveAudioSettings(out muted, out volume);
        YongwooVideoLayoutUtility.ConfigureAudio(videoPlayer, audioSource, muted, volume);
        videoPlayer.Play();

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
        ClearStoryVideoKey();
        MarkNotPlaying();
        Hide();
    }

    private void MarkPlaying()
    {
        if (!_isRegisteredAsPlaying)
        {
            YongwooAudioManager.SuspendBgmForVideo();
        }

        _isRegisteredAsPlaying = true;
        IsAnyPlaying = true;
    }

    private void MarkNotPlaying()
    {
        if (!_isRegisteredAsPlaying)
        {
            return;
        }

        _isRegisteredAsPlaying = false;
        IsAnyPlaying = false;
        YongwooAudioManager.ResumeBgmAfterVideo();
    }

    private void GetEffectiveAudioSettings(out bool muted, out float volume)
    {
        muted = muteAudio;
        volume = videoVolume;

        if (!useStoryVideoAudioOverrides || !_hasActiveStoryVideoKey)
        {
            return;
        }

        switch (_activeStoryVideoKey)
        {
            case YongwooStoryVideoKey.Memory01:
                muted = memory01Muted;
                volume = memory01Volume;
                break;
            case YongwooStoryVideoKey.Memory02:
                muted = memory02Muted;
                volume = memory02Volume;
                break;
            case YongwooStoryVideoKey.BossDefeat:
                muted = bossDefeatMuted;
                volume = bossDefeatVolume;
                break;
        }
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
        MarkNotPlaying();

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
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
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

public enum YongwooVideoLayoutMode
{
    ManualRect,
    FitInside,
    FillScreen,
    Stretch
}

public static class YongwooVideoLayoutUtility
{
    public static void Apply(
        RectTransform rect,
        VideoClip clip,
        YongwooVideoLayoutMode mode,
        float scale,
        bool preserveAspectInManualRect = true,
        float manualAspect = 1.7778f)
    {
        if (rect == null)
        {
            return;
        }

        if (mode == YongwooVideoLayoutMode.ManualRect)
        {
            if (preserveAspectInManualRect)
            {
                ApplyManualAspect(rect, clip, manualAspect);
            }
            return;
        }

        scale = Mathf.Clamp(scale, 0.25f, 2f);
        if (mode == YongwooVideoLayoutMode.Stretch || clip == null || clip.width <= 0 || clip.height <= 0)
        {
            StretchToParent(rect);
            rect.localScale = Vector3.one * scale;
            return;
        }

        RectTransform parent = rect.parent as RectTransform;
        if (parent == null)
        {
            StretchToParent(rect);
            rect.localScale = Vector3.one * scale;
            return;
        }

        float parentWidth = Mathf.Max(1f, parent.rect.width);
        float parentHeight = Mathf.Max(1f, parent.rect.height);
        float videoAspect = Mathf.Max(0.001f, (float)clip.width / clip.height);
        float parentAspect = parentWidth / parentHeight;

        float width;
        float height;
        bool useParentWidth = mode == YongwooVideoLayoutMode.FitInside
            ? videoAspect >= parentAspect
            : videoAspect < parentAspect;

        if (useParentWidth)
        {
            width = parentWidth;
            height = width / videoAspect;
        }
        else
        {
            height = parentHeight;
            width = height * videoAspect;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width * scale, height * scale);
        rect.localScale = Vector3.one;
    }

    public static void ConfigureAudio(VideoPlayer player, AudioSource source, bool muted, float volume)
    {
        if (player == null)
        {
            return;
        }

        volume = Mathf.Clamp01(volume);

        player.audioOutputMode = VideoAudioOutputMode.Direct;
        ConfigureControlledTracks(player, null);
        ApplyDirectAudio(player, muted, volume);

        if (source != null)
        {
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.mute = muted;
            source.volume = muted ? 0f : volume;
        }
    }

    public static void ApplyAudioLive(VideoPlayer player, AudioSource source, bool muted, float volume)
    {
        if (player == null)
        {
            return;
        }

        volume = Mathf.Clamp01(volume);
        if (player.audioOutputMode != VideoAudioOutputMode.Direct)
        {
            player.audioOutputMode = VideoAudioOutputMode.Direct;
        }

        ApplyDirectAudio(player, muted, volume);

        if (source != null)
        {
            source.mute = muted;
            source.volume = muted ? 0f : volume;
        }
    }

    private static void ConfigureControlledTracks(VideoPlayer player, AudioSource source)
    {
        ushort trackCount = player.audioTrackCount > 0 ? player.audioTrackCount : (ushort)1;
        ushort maxTrackCount = VideoPlayer.controlledAudioTrackMaxCount;
        if (maxTrackCount > 0 && trackCount > maxTrackCount)
        {
            trackCount = maxTrackCount;
        }

        player.controlledAudioTrackCount = trackCount;
        for (ushort track = 0; track < trackCount; track++)
        {
            // EnableAudioTrack is only effective before playback. Keep decoding enabled;
            // use mute/volume for live control while the video is playing.
            player.EnableAudioTrack(track, true);
            if (source != null)
            {
                player.SetTargetAudioSource(track, source);
            }
        }
    }

    private static void ApplyDirectAudio(VideoPlayer player, bool muted, float volume)
    {
        ushort trackCount = player.audioTrackCount > 0
            ? player.audioTrackCount
            : player.controlledAudioTrackCount > 0
                ? player.controlledAudioTrackCount
                : (ushort)1;

        ushort maxTrackCount = VideoPlayer.controlledAudioTrackMaxCount;
        if (maxTrackCount > 0 && trackCount > maxTrackCount)
        {
            trackCount = maxTrackCount;
        }

        for (ushort track = 0; track < trackCount; track++)
        {
            player.SetDirectAudioMute(track, muted);
            if (player.canSetDirectAudioVolume)
            {
                player.SetDirectAudioVolume(track, muted ? 0f : volume);
            }
        }
    }

    private static void ApplyManualAspect(RectTransform rect, VideoClip clip, float manualAspect)
    {
        float aspect = clip != null && clip.width > 0 && clip.height > 0
            ? (float)clip.width / clip.height
            : Mathf.Max(0.1f, manualAspect);

        Vector2 size = rect.rect.size;
        if (size.x <= 0f || size.y <= 0f)
        {
            size = rect.sizeDelta;
        }

        float width = Mathf.Max(1f, Mathf.Abs(size.x));
        float height = Mathf.Max(1f, Mathf.Abs(size.y));
        float currentAspect = width / height;

        if (currentAspect > aspect)
        {
            width = height * aspect;
        }
        else
        {
            height = width / aspect;
        }

        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
    }
}

public enum YongwooStoryVideoKey
{
    Title,
    Intro,
    Memory01,
    Memory02,
    BossDefeat
}

public static class YongwooStoryVideoClips
{
    public const string ResourceFolder = "Yongwoo/Videos";

    public static VideoClip Load(YongwooStoryVideoKey key)
    {
        string[] names = GetCandidateNames(key);
        for (int i = 0; i < names.Length; i++)
        {
            VideoClip clip = Resources.Load<VideoClip>($"{ResourceFolder}/{names[i]}");
            if (clip != null)
            {
                return clip;
            }
        }

        return null;
    }

    public static VideoClip[] LoadPlaylist(YongwooStoryVideoKey key)
    {
        string[] names = GetPlaylistCandidateNames(key);
        if (names.Length == 0)
        {
            return new VideoClip[0];
        }

        VideoClip[] clips = new VideoClip[names.Length];
        int count = 0;
        for (int i = 0; i < names.Length; i++)
        {
            VideoClip clip = Resources.Load<VideoClip>($"{ResourceFolder}/{names[i]}");
            if (clip == null)
            {
                continue;
            }

            clips[count] = clip;
            count++;
        }

        if (count == 0)
        {
            return new VideoClip[0];
        }

        VideoClip[] filtered = new VideoClip[count];
        for (int i = 0; i < count; i++)
        {
            filtered[i] = clips[i];
        }

        return filtered;
    }

    public static VideoClip ResolveForSequence(string sequenceName)
    {
        return TryResolveKeyForSequence(sequenceName, out YongwooStoryVideoKey key)
            ? Load(key)
            : null;
    }

    public static bool TryResolveKeyForSequence(string sequenceName, out YongwooStoryVideoKey key)
    {
        if (string.IsNullOrWhiteSpace(sequenceName))
        {
            key = default;
            return false;
        }

        if (sequenceName.Contains("기억조각_01") || sequenceName.Contains("기억조각1"))
        {
            key = YongwooStoryVideoKey.Memory01;
            return true;
        }

        if (sequenceName.Contains("기억조각_02") || sequenceName.Contains("기억조각2"))
        {
            key = YongwooStoryVideoKey.Memory02;
            return true;
        }

        if (sequenceName.Contains("처치후") || sequenceName.Contains("HOME회수") || sequenceName.Contains("보스_처치"))
        {
            key = YongwooStoryVideoKey.BossDefeat;
            return true;
        }

        key = default;
        return false;
    }

    public static string GetImportGuide()
    {
        return "Assets/_WIP/yongwoo/Resources/Yongwoo/Videos/ 안에 title_01/title 또는 intro_01/intro, memory_01, memory_02, boss_defeat 이름으로 넣으면 자동 재생됩니다.";
    }

    private static string[] GetCandidateNames(YongwooStoryVideoKey key)
    {
        switch (key)
        {
            case YongwooStoryVideoKey.Title:
                return new[] { "title", "타이틀", "title_loop", "title_background" };
            case YongwooStoryVideoKey.Intro:
                return new[] { "intro", "인트로", "opening", "opening_intro" };
            case YongwooStoryVideoKey.Memory01:
                return new[] { "memory_01", "memory01", "기억조각_01", "기억조각1" };
            case YongwooStoryVideoKey.Memory02:
                return new[] { "memory_02", "memory02", "기억조각_02", "기억조각2" };
            case YongwooStoryVideoKey.BossDefeat:
                return new[] { "boss_defeat", "boss_after_defeat", "ending", "보스처치후", "엔딩" };
            default:
                return new string[0];
        }
    }

    private static string[] GetPlaylistCandidateNames(YongwooStoryVideoKey key)
    {
        switch (key)
        {
            case YongwooStoryVideoKey.Title:
                return BuildNumberedNames("title", "title_loop", "타이틀");
            case YongwooStoryVideoKey.Intro:
                return BuildNumberedNames("intro", "opening", "인트로");
            default:
                return new string[0];
        }
    }

    private static string[] BuildNumberedNames(params string[] prefixes)
    {
        const int maxPlaylistIndex = 9;
        string[] names = new string[prefixes.Length * maxPlaylistIndex * 2];
        int write = 0;
        for (int prefixIndex = 0; prefixIndex < prefixes.Length; prefixIndex++)
        {
            string prefix = prefixes[prefixIndex];
            for (int i = 1; i <= maxPlaylistIndex; i++)
            {
                names[write] = $"{prefix}_{i:00}";
                write++;
                names[write] = $"{prefix}_{i}";
                write++;
            }
        }

        return names;
    }
}
