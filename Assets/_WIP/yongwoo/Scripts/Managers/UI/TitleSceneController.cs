using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

// 역할:
// - 목업 타이틀 씬의 버튼 입력을 처리합니다.

[DisallowMultipleComponent]
public class TitleSceneController : MonoBehaviour
{
    [SerializeField] private string stageSceneName = "Yongwoo_Stage";
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private bool playTitleVideoBackground = true;
    [SerializeField] private VideoClip titleVideoClip;
    [SerializeField] private VideoClip[] titleVideoPlaylist;
    [SerializeField] private RawImage titleVideoImage;
    [SerializeField] private VideoPlayer titleVideoPlayer;
    [SerializeField] private AudioSource titleVideoAudioSource;
    [SerializeField] private YongwooVideoLayoutMode titleVideoLayoutMode = YongwooVideoLayoutMode.ManualRect;
    [SerializeField] private bool titleVideoPreserveAspect = true;
    [SerializeField, Min(0.1f)] private float titleVideoManualAspect = 1.7778f;
    [SerializeField, Range(0.25f, 2f)] private float titleVideoScale = 1f;
    [SerializeField] private bool titleVideoMuted = true;
    [SerializeField, Range(0f, 1f)] private float titleVideoVolume = 0.65f;
    [SerializeField] private bool playIntroVideoBeforeStage = true;
    [SerializeField] private VideoClip introVideoClip;
    [SerializeField] private VideoClip[] introVideoPlaylist;
    [SerializeField] private bool introVideoMuted;
    [SerializeField, Range(0f, 1f)] private float introVideoVolume = 0.65f;
    [SerializeField] private CutsceneVideoPanel cutsceneVideoPanel;
    [SerializeField] private bool introVideoSkippable = true;

    private RenderTexture _titleVideoTexture;
    private Coroutine _titleVideoBackgroundRoutine;
    private bool _isLoadingStage;

    private void Reset()
    {
        TryAutoWire();
    }

    private void Awake()
    {
        TryAutoWire();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        TryAutoWire();
        if (playTitleVideoBackground)
        {
            EnsureTitleVideoObjects();
        }
    }

    private void Start()
    {
        if (playTitleVideoBackground)
        {
            _titleVideoBackgroundRoutine = StartCoroutine(PlayTitleVideoBackgroundRoutine());
        }
    }

    private void Update()
    {
        if (titleVideoPlayer != null && titleVideoPlayer.isPlaying)
        {
            YongwooVideoLayoutUtility.ApplyAudioLive(titleVideoPlayer, titleVideoAudioSource, titleVideoMuted, titleVideoVolume);
        }
    }

    private void OnDestroy()
    {
        ReleaseTitleVideoTexture();
    }

    private void OnEnable()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(LoadStageScene);
            startButton.onClick.AddListener(LoadStageScene);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private IEnumerator PlayTitleVideoBackgroundRoutine()
    {
        VideoClip[] clips = ResolveTitleVideoPlaylist();

        EnsureTitleVideoObjects();
        if (titleVideoPlayer == null || titleVideoImage == null)
        {
            _titleVideoBackgroundRoutine = null;
            yield break;
        }

        ConfigureTitleRenderTexture();
        titleVideoImage.texture = _titleVideoTexture;
        titleVideoImage.color = Color.white;

        if (clips.Length == 0)
        {
            _titleVideoBackgroundRoutine = null;
            yield break;
        }

        int index = 0;
        while (!_isLoadingStage)
        {
            VideoClip clip = clips[index];
            bool loopSingleClip = clips.Length == 1;
            yield return PlayTitleVideoClipRoutine(clip, loopSingleClip);

            if (loopSingleClip)
            {
                break;
            }

            index = (index + 1) % clips.Length;
        }

        _titleVideoBackgroundRoutine = null;
    }

    private IEnumerator PlayTitleVideoClipRoutine(VideoClip clip, bool loop)
    {
        if (clip == null || titleVideoPlayer == null || titleVideoImage == null)
        {
            yield break;
        }

        titleVideoPlayer.Stop();
        titleVideoPlayer.clip = clip;
        titleVideoPlayer.isLooping = loop;
        titleVideoPlayer.playOnAwake = false;
        titleVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
        titleVideoPlayer.targetTexture = _titleVideoTexture;
        YongwooVideoLayoutUtility.ConfigureAudio(titleVideoPlayer, titleVideoAudioSource, titleVideoMuted, titleVideoVolume);

        titleVideoPlayer.Prepare();
        float prepareTimeout = 5f;
        while (!_isLoadingStage && !titleVideoPlayer.isPrepared && prepareTimeout > 0f)
        {
            prepareTimeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (_isLoadingStage)
        {
            yield break;
        }

        YongwooVideoLayoutUtility.Apply(
            titleVideoImage.rectTransform,
            clip,
            titleVideoLayoutMode,
            titleVideoScale,
            titleVideoPreserveAspect,
            titleVideoManualAspect);
        YongwooVideoLayoutUtility.ConfigureAudio(titleVideoPlayer, titleVideoAudioSource, titleVideoMuted, titleVideoVolume);
        titleVideoPlayer.Play();
        yield return null;

        while (!_isLoadingStage && titleVideoPlayer != null && titleVideoPlayer.isPlaying)
        {
            yield return null;
        }
    }

    public void LoadStageScene()
    {
        if (_isLoadingStage)
        {
            return;
        }

        StartCoroutine(LoadStageSceneRoutine());
    }

    private IEnumerator LoadStageSceneRoutine()
    {
        _isLoadingStage = true;
        SetButtonsInteractable(false);
        YongwooAudioManager.Play(YongwooSfxId.TitleStart, 0.65f, 0.02f);
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        StopTitleVideoBackground();

        if (playIntroVideoBeforeStage)
        {
            VideoClip[] clips = ResolveIntroVideoPlaylist();
            if (clips.Length > 0)
            {
                CutsceneVideoPanel panel = EnsureCutsceneVideoPanel();
                if (panel != null)
                {
                    panel.SetAudioSettings(introVideoMuted, introVideoVolume);
                    for (int i = 0; i < clips.Length; i++)
                    {
                        if (clips[i] == null)
                        {
                            continue;
                        }

                        yield return panel.Play(clips[i], introVideoSkippable);
                    }
                }
            }
        }

        SceneManager.LoadScene(stageSceneName);
    }

    private void StopTitleVideoBackground()
    {
        if (_titleVideoBackgroundRoutine != null)
        {
            StopCoroutine(_titleVideoBackgroundRoutine);
            _titleVideoBackgroundRoutine = null;
        }

        if (titleVideoPlayer != null)
        {
            YongwooVideoLayoutUtility.ApplyAudioLive(titleVideoPlayer, titleVideoAudioSource, muted: true, volume: 0f);
            titleVideoPlayer.Stop();
        }

        if (titleVideoAudioSource != null)
        {
            titleVideoAudioSource.mute = true;
            titleVideoAudioSource.volume = 0f;
            titleVideoAudioSource.Stop();
        }
    }

    public void QuitGame()
    {
        YongwooAudioManager.Play(YongwooSfxId.TitleQuit, 0.5f, 0.02f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void TryAutoWire()
    {
        startButton ??= GameObject.Find("StartButton")?.GetComponent<Button>();
        quitButton ??= GameObject.Find("QuitButton")?.GetComponent<Button>();
        cutsceneVideoPanel ??= FindFirstObjectByType<CutsceneVideoPanel>(FindObjectsInactive.Include);
        titleVideoImage ??= transform.Find("TitleVideoBackground")?.GetComponent<RawImage>();
        titleVideoPlayer ??= titleVideoImage != null ? titleVideoImage.GetComponent<VideoPlayer>() : null;
        titleVideoAudioSource ??= titleVideoImage != null ? titleVideoImage.GetComponent<AudioSource>() : null;
    }

    private CutsceneVideoPanel EnsureCutsceneVideoPanel()
    {
        if (cutsceneVideoPanel != null)
        {
            return cutsceneVideoPanel;
        }

        cutsceneVideoPanel = FindFirstObjectByType<CutsceneVideoPanel>(FindObjectsInactive.Include);
        if (cutsceneVideoPanel != null)
        {
            return cutsceneVideoPanel;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        }

        if (canvas == null)
        {
            return null;
        }

        cutsceneVideoPanel = canvas.gameObject.AddComponent<CutsceneVideoPanel>();
        return cutsceneVideoPanel;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (startButton != null)
        {
            startButton.interactable = interactable;
        }

        if (quitButton != null)
        {
            quitButton.interactable = interactable;
        }
    }

    private VideoClip[] ResolveTitleVideoPlaylist()
    {
        VideoClip[] clips = FilterClips(titleVideoPlaylist);
        if (clips.Length > 0)
        {
            return clips;
        }

        clips = YongwooStoryVideoClips.LoadPlaylist(YongwooStoryVideoKey.Title);
        if (clips.Length > 0)
        {
            return clips;
        }

        VideoClip fallback = titleVideoClip != null
            ? titleVideoClip
            : YongwooStoryVideoClips.Load(YongwooStoryVideoKey.Title);
        return fallback != null ? new[] { fallback } : new VideoClip[0];
    }

    private VideoClip[] ResolveIntroVideoPlaylist()
    {
        VideoClip[] clips = FilterClips(introVideoPlaylist);
        if (clips.Length > 0)
        {
            return clips;
        }

        clips = YongwooStoryVideoClips.LoadPlaylist(YongwooStoryVideoKey.Intro);
        if (clips.Length > 0)
        {
            return clips;
        }

        VideoClip fallback = introVideoClip != null
            ? introVideoClip
            : YongwooStoryVideoClips.Load(YongwooStoryVideoKey.Intro);
        return fallback != null ? new[] { fallback } : new VideoClip[0];
    }

    private static VideoClip[] FilterClips(VideoClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return new VideoClip[0];
        }

        int count = 0;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
            {
                count++;
            }
        }

        if (count == 0)
        {
            return new VideoClip[0];
        }

        VideoClip[] filtered = new VideoClip[count];
        int write = 0;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
            {
                filtered[write] = clips[i];
                write++;
            }
        }

        return filtered;
    }

    private void EnsureTitleVideoObjects()
    {
        if (titleVideoImage == null)
        {
            Transform existing = transform.Find("TitleVideoBackground");
            if (existing != null)
            {
                titleVideoImage = existing.GetComponent<RawImage>();
            }
        }

        if (titleVideoImage == null)
        {
            GameObject go = new GameObject("TitleVideoBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            StretchToParent(rect);
            go.transform.SetAsFirstSibling();
            titleVideoImage = go.GetComponent<RawImage>();
            titleVideoImage.raycastTarget = false;
            titleVideoImage.color = new Color(1f, 1f, 1f, 0.45f);
        }

        titleVideoImage.transform.SetAsFirstSibling();

        Image staticBackground = transform.Find("Background")?.GetComponent<Image>();
        if (staticBackground != null)
        {
            Color color = staticBackground.color;
            color.a = 0f;
            staticBackground.color = color;
            staticBackground.raycastTarget = false;
        }

        titleVideoPlayer ??= titleVideoImage.GetComponent<VideoPlayer>();
        if (titleVideoPlayer == null)
        {
            titleVideoPlayer = titleVideoImage.gameObject.AddComponent<VideoPlayer>();
        }

        titleVideoAudioSource ??= titleVideoImage.GetComponent<AudioSource>();
        if (titleVideoAudioSource == null)
        {
            titleVideoAudioSource = titleVideoImage.gameObject.AddComponent<AudioSource>();
        }

        titleVideoAudioSource.playOnAwake = false;
    }

    private void ConfigureTitleRenderTexture()
    {
        int width = Mathf.Max(16, Screen.width);
        int height = Mathf.Max(16, Screen.height);
        if (_titleVideoTexture != null && _titleVideoTexture.width == width && _titleVideoTexture.height == height)
        {
            return;
        }

        ReleaseTitleVideoTexture();
        _titleVideoTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            name = "TitleVideo_RenderTexture"
        };
        _titleVideoTexture.Create();
    }

    private void ReleaseTitleVideoTexture()
    {
        if (_titleVideoTexture == null)
        {
            return;
        }

        _titleVideoTexture.Release();
        Destroy(_titleVideoTexture);
        _titleVideoTexture = null;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
    }
}
