using System;
using System.Collections.Generic;
using UnityEngine;

public enum YongwooSfxId
{
    TypingTick,
    SystemLogIn,
    CommsIn,
    CommsOut,
    UiPromptIn,
    UiConfirm,
    UiClick,
    PauseOpen,
    PauseClose,
    TitleStart,
    TitleQuit,
    FadeOut,
    FadeIn,
    GlitchPulse,
    TimeFreeze,
    TimeUnfreeze,
    SlowmoOn,
    SlowmoOff,
    Jump,
    Dash,
    Roll,
    WeaponSwap,
    SwordSwing,
    GunFire,
    HitLight,
    PlayerHurt,
    PlayerDeath,
    PlayerRespawn,
    EnemyHurt,
    EnemyDeath,
    BossArenaEnter,
    BossTelegraph,
    BossFire,
    BossTeleportOut,
    BossTeleportIn,
    BossProjectileImpact,
    BossBlastArm,
    BossBlastExplode,
    BossHurt,
    BossDeath,
    BossPhaseShift
}

[DisallowMultipleComponent]
public sealed class YongwooAudioManager : MonoBehaviour
{
    private const string ResourceRoot = "Yongwoo/SFX/";
    private const int InitialSourceCount = 8;
    private const float DefaultVolume = 0.75f;

    private static readonly Dictionary<YongwooSfxId, string> ResourceNames = new()
    {
        { YongwooSfxId.TypingTick, "타이핑_글자틱" },
        { YongwooSfxId.SystemLogIn, "시스템로그_표시" },
        { YongwooSfxId.CommsIn, "브로커통신_표시" },
        { YongwooSfxId.CommsOut, "브로커통신_닫힘" },
        { YongwooSfxId.UiPromptIn, "상호작용안내_표시" },
        { YongwooSfxId.UiConfirm, "상호작용_확인" },
        { YongwooSfxId.UiClick, "버튼_클릭" },
        { YongwooSfxId.PauseOpen, "일시정지_열기" },
        { YongwooSfxId.PauseClose, "일시정지_닫기" },
        { YongwooSfxId.TitleStart, "타이틀_게임시작" },
        { YongwooSfxId.TitleQuit, "타이틀_게임종료" },
        { YongwooSfxId.FadeOut, "화면_암전" },
        { YongwooSfxId.FadeIn, "화면_복귀" },
        { YongwooSfxId.GlitchPulse, "화면_글리치펄스" },
        { YongwooSfxId.TimeFreeze, "시간_정지" },
        { YongwooSfxId.TimeUnfreeze, "시간_정지해제" },
        { YongwooSfxId.SlowmoOn, "슬로우모션_켜짐" },
        { YongwooSfxId.SlowmoOff, "슬로우모션_꺼짐" },
        { YongwooSfxId.Jump, "플레이어_점프" },
        { YongwooSfxId.Dash, "플레이어_대시" },
        { YongwooSfxId.Roll, "플레이어_구르기" },
        { YongwooSfxId.WeaponSwap, "플레이어_무기전환" },
        { YongwooSfxId.SwordSwing, "플레이어_칼휘두르기" },
        { YongwooSfxId.GunFire, "플레이어_총발사" },
        { YongwooSfxId.HitLight, "전투_공통타격적중" },
        { YongwooSfxId.PlayerHurt, "플레이어_피격" },
        { YongwooSfxId.PlayerDeath, "플레이어_사망" },
        { YongwooSfxId.PlayerRespawn, "플레이어_리스폰" },
        { YongwooSfxId.EnemyHurt, "적_피격" },
        { YongwooSfxId.EnemyDeath, "적_사망" },
        { YongwooSfxId.BossArenaEnter, "보스전_아레나입장" },
        { YongwooSfxId.BossTelegraph, "보스_패턴예고" },
        { YongwooSfxId.BossFire, "보스_패턴발동" },
        { YongwooSfxId.BossTeleportOut, "보스_텔레포트_사라짐" },
        { YongwooSfxId.BossTeleportIn, "보스_텔레포트_등장" },
        { YongwooSfxId.BossProjectileImpact, "보스_투사체_충돌" },
        { YongwooSfxId.BossBlastArm, "보스_장판_경고" },
        { YongwooSfxId.BossBlastExplode, "보스_장판_폭발" },
        { YongwooSfxId.BossHurt, "보스_피격" },
        { YongwooSfxId.BossDeath, "보스_처치" },
        { YongwooSfxId.BossPhaseShift, "보스_페이즈전환" },
    };

    private static YongwooAudioManager _instance;

    [Header("Runtime Mix")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Min(1)] private int sourceCount = InitialSourceCount;

    private readonly Dictionary<YongwooSfxId, AudioClip> _clipCache = new();
    private readonly List<AudioSource> _sources = new();
    private int _nextSourceIndex;

    public static void Play(YongwooSfxId id, float volume = DefaultVolume, float pitchJitter = 0f)
    {
        Instance.PlayInternal(id, volume, pitchJitter);
    }

    public static void PlayAt(Vector3 position, YongwooSfxId id, float volume = DefaultVolume, float pitchJitter = 0f)
    {
        AudioClip clip = Instance.GetClip(id);
        if (clip == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume) * Instance.masterVolume);
    }

    private static YongwooAudioManager Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

            YongwooAudioManager existing = FindFirstObjectByType<YongwooAudioManager>();
            if (existing != null)
            {
                _instance = existing;
                return _instance;
            }

            GameObject managerObject = new GameObject("YongwooAudioManager");
            DontDestroyOnLoad(managerObject);
            _instance = managerObject.AddComponent<YongwooAudioManager>();
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSources();
    }

    private void PlayInternal(YongwooSfxId id, float volume, float pitchJitter)
    {
        AudioClip clip = GetClip(id);
        if (clip == null)
        {
            return;
        }

        AudioSource source = NextSource();
        source.pitch = Mathf.Clamp(1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter), 0.5f, 1.6f);
        source.PlayOneShot(clip, Mathf.Clamp01(volume) * masterVolume);
    }

    private AudioClip GetClip(YongwooSfxId id)
    {
        if (_clipCache.TryGetValue(id, out AudioClip cached))
        {
            return cached;
        }

        if (!ResourceNames.TryGetValue(id, out string resourceName))
        {
            return null;
        }

        AudioClip clip = Resources.Load<AudioClip>(ResourceRoot + resourceName);
        _clipCache[id] = clip;
        return clip;
    }

    private AudioSource NextSource()
    {
        EnsureSources();

        AudioSource source = _sources[_nextSourceIndex];
        _nextSourceIndex = (_nextSourceIndex + 1) % _sources.Count;
        return source;
    }

    private void EnsureSources()
    {
        int count = Mathf.Max(1, sourceCount);
        while (_sources.Count < count)
        {
            GameObject sourceObject = new GameObject($"SFX_{_sources.Count:00}");
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            _sources.Add(source);
        }
    }
}
