using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class YongwooBgmAreaController : MonoBehaviour
{
    private const string StageSceneName = "Yongwoo_Stage";

    private static YongwooBgmAreaController _instance;

    [SerializeField] private YongwooBgmId defaultStageBgm = YongwooBgmId.AccessArea;
    [SerializeField] private float homeAndPlazaMinY = -2f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.75f;
    [SerializeField] private bool stopBgmOutsideStage = true;
    [SerializeField] private bool logChanges;

    private Transform _player;
    private BossBattleArena _bossArena;
    private YongwooBgmId _lastRequested = YongwooBgmId.None;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureForScene(scene);
    }

    private static void EnsureForScene(Scene scene)
    {
        if (scene.name != StageSceneName)
        {
            if (_instance != null)
            {
                _instance.enabled = false;
                _instance._lastRequested = YongwooBgmId.None;
            }

            if (_instance == null || _instance.stopBgmOutsideStage)
            {
                YongwooAudioManager.StopBgm(0.35f);
            }

            return;
        }

        if (_instance == null)
        {
            GameObject controllerObject = new GameObject("YongwooBgmAreaController");
            DontDestroyOnLoad(controllerObject);
            _instance = controllerObject.AddComponent<YongwooBgmAreaController>();
        }

        _instance.enabled = true;
        _instance.RefreshSceneReferences();
        _instance.RequestBgm(_instance.ResolveCurrentBgm(), immediate: true);
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
    }

    private void OnEnable()
    {
        RefreshSceneReferences();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != StageSceneName)
        {
            return;
        }

        RequestBgm(ResolveCurrentBgm(), immediate: false);
    }

    private void RefreshSceneReferences()
    {
        _player = FindFirstObjectByType<PlayerInteraction>()?.transform;
        _bossArena = FindFirstObjectByType<BossBattleArena>(FindObjectsInactive.Include);
    }

    private YongwooBgmId ResolveCurrentBgm()
    {
        if (_bossArena == null || _player == null)
        {
            RefreshSceneReferences();
        }

        if (_bossArena != null && _bossArena.IsActive)
        {
            return YongwooBgmId.BossBattle;
        }

        if (_player != null && _player.position.y >= homeAndPlazaMinY)
        {
            return YongwooBgmId.HomeAndPlaza;
        }

        return defaultStageBgm;
    }

    private void RequestBgm(YongwooBgmId bgm, bool immediate)
    {
        if (_lastRequested == bgm)
        {
            return;
        }

        _lastRequested = bgm;
        YongwooAudioManager.PlayBgm(bgm, immediate ? 0f : fadeDuration);

        if (logChanges)
        {
            Debug.Log($"[YongwooBGM] {bgm}", this);
        }
    }
}
