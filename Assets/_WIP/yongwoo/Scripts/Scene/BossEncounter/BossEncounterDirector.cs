using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
#if UNITY_EDITOR
using UnityEditor;
#endif
// 역할:
// - 보스 조우의 컷신, 대사, 전투 시작, 실패 리셋, 승리 흐름을 순서대로 조율합니다.
// - 플레이어, 카메라, HUD, 대화 뷰, 보스 프리팹을 연결하는 씬 허브입니다.
//
// 구조 포인트:
// - 프리팹 내부 규칙은 건드리지 않고 씬 단위 오케스트레이션만 맡도록 보는 파일입니다.

[DisallowMultipleComponent]
public class BossEncounterDirector : MonoBehaviour
{
    private const string DefaultDialogueObjectName = "DialogueUI";
#if UNITY_EDITOR
    private const string DefaultBossPrefabAssetPath = "Assets/_WIP/yongwoo/Prefabs/Prototype/Boss.prefab";
#endif

    public enum EncounterState
    {
        Idle,
        IntroTimeline,
        IntroDialogue,
        Combat,
        FailureReset,
        VictoryTimeline,
        VictoryDialogue,
        Completed
    }

    [Header("Scene References")]
    [Tooltip("조우 시작/전투 복귀 시 제어를 잠그거나 다시 돌려줄 플레이어 이동 컴포넌트입니다.")]
    [SerializeField] private SimplePlayerController playerController;
    [Tooltip("플레이어 사망/리스폰 감지와 이동/공격 제어 잠금을 같이 맡는 플레이어 상호작용 허브입니다.")]
    [SerializeField] private PlayerInteraction playerInteraction;
    [Tooltip("평상시 플레이 카메라를 따라가는 컴포넌트입니다. 컷씬 동안에는 꺼지고, 전투 시작 시 다시 켜집니다.")]
    [SerializeField] private SimpleCameraFollow cameraFollow;
    [Tooltip("체력 HUD와 입력 설정 UI를 표시하는 HUD입니다. 조우 액션 표시 상태도 이 디렉터의 상태를 따라갑니다.")]
    [SerializeField] private BattleHud battleHud;
    [Tooltip("인트로/승리 대화를 화면에 표시하는 패널입니다. 씬에 직접 배치한 UI를 연결해 쓰는 것이 기준입니다.")]
    [SerializeField] private EncounterDialoguePanel dialoguePanel;
    [Tooltip("보스 조우와 NPC 대화를 공통 흐름으로 재생하는 대화 러너입니다. 씬에 직접 배치한 오브젝트를 연결하는 것이 기준입니다.")]
    [SerializeField] private DialogueManager dialogueManager;
    [Tooltip("조우 시작 시 소환할 보스 프리팹입니다.")]
    [SerializeField] private BossController bossPrefab;
    [Tooltip("보스를 조우 시작 시 소환할 위치 마커입니다.")]
    [SerializeField] private Transform bossSpawnPoint;
    [Tooltip("소환한 보스를 정리해서 붙일 부모입니다. 비워두면 루트에 생성합니다.")]
    [SerializeField] private Transform bossParent;
    [Tooltip("플레이어가 보스전 중 죽었을 때 다시 세워둘 위치입니다. 비어 있으면 전투 시작 순간의 플레이어 위치를 사용합니다.")]
    [SerializeField] private Transform combatCheckpoint;

    [Header("Optional Timelines")]
    [Tooltip("연결하면 인트로를 Timeline으로 재생합니다. 비어 있으면 아래 Fallback Cutscene 값으로 코드 컷씬을 사용합니다.")]
    [SerializeField] private PlayableDirector introTimeline;
    [Tooltip("연결하면 승리 연출을 Timeline으로 재생합니다. 비어 있으면 아래 Fallback Cutscene 값으로 코드 컷씬을 사용합니다.")]
    [SerializeField] private PlayableDirector victoryTimeline;

    [Header("Fallback Cutscene")]
    [Tooltip("코드 기반 인트로 컷씬에서 넓은 샷으로 이동하는 시간입니다.")]
    [SerializeField] private float introWideDuration = 0.6f;
    [Tooltip("코드 기반 인트로 컷씬에서 보스를 강조하는 샷으로 이동하는 시간입니다.")]
    [SerializeField] private float introBossDuration = 0.7f;
    [Tooltip("코드 기반 인트로 컷씬에서 보스 샷을 유지하는 시간입니다.")]
    [SerializeField] private float introHoldDuration = 0.4f;
    [Tooltip("코드 기반 승리 연출에서 보스 쪽으로 카메라를 이동하는 시간입니다.")]
    [SerializeField] private float victoryPanDuration = 0.65f;
    [Tooltip("코드 기반 승리 연출에서 카메라를 유지하는 시간입니다.")]
    [SerializeField] private float victoryHoldDuration = 0.85f;

    [Header("Dialogue")]
    [Tooltip("인트로 컷씬 직후 재생할 대사 목록입니다.")]
    [SerializeField] private List<EncounterDialogueLine> introLines = new();
    [Tooltip("보스 처치 후 재생할 승리 대사 목록입니다.")]
    [SerializeField] private List<EncounterDialogueLine> victoryLines = new();

    private Rigidbody2D _playerBody;
    private BossController _currentBossController;
    private BossInteraction _currentBossInteraction;
    private GameObject _currentBossObject;
    private Coroutine _sequenceRoutine;
    private bool _skipRequested;
    private Vector3 _encounterStartPosition;
    private Vector3 _combatCheckpointPosition;
    private Vector3 _bossSpawnPosition;
    private Quaternion _bossSpawnRotation = Quaternion.identity;
    private EncounterState _state;

    public EncounterState CurrentState => _state;
    public BossInteraction CurrentBossInteraction => _currentBossInteraction;
    public BossController BossPrefab => bossPrefab;
    public Transform BossSpawnPoint => bossSpawnPoint;
    public IReadOnlyList<EncounterDialogueLine> IntroLines => introLines;
    public IReadOnlyList<EncounterDialogueLine> VictoryLines => victoryLines;
    public float IntroWideDuration => introWideDuration;
    public float IntroBossDuration => introBossDuration;
    public float IntroHoldDuration => introHoldDuration;
    public float VictoryPanDuration => victoryPanDuration;
    public float VictoryHoldDuration => victoryHoldDuration;
    public bool IsEncounterActionInteractable => _state != EncounterState.FailureReset;
    public string CurrentEncounterActionLabel
    {
        get
        {
            return _state switch
            {
                EncounterState.Idle => "조우 시작",
                EncounterState.IntroTimeline => "건너뛰기",
                EncounterState.IntroDialogue => "건너뛰기",
                EncounterState.Combat => "전투 리셋",
                EncounterState.FailureReset => "리셋 중...",
                EncounterState.VictoryTimeline => "건너뛰기",
                EncounterState.VictoryDialogue => "건너뛰기",
                EncounterState.Completed => "다시 시작",
                _ => "조우 시작"
            };
        }
    }

    private void Reset()
    {
        RefreshReferencesAndDefaults(preparePreview: true);
    }

    private void Awake()
    {
        RefreshReferencesAndDefaults(preparePreview: false);
    }

    private void OnEnable()
    {
        RefreshReferencesAndDefaults(preparePreview: true);
        HookPlayerEvents();
        ConfigureHud();
        RefreshHudState();
    }

    private void OnDisable()
    {
        UnhookPlayerEvents();
        UnhookBossEvents();
        if (Application.isPlaying)
        {
            DespawnCurrentBoss();
        }
    }

    private void Start()
    {
        RefreshReferencesAndDefaults(preparePreview: true);
        HookPlayerEvents();
        ConfigureHud();
        RefreshHudState();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        RefreshReferencesAndDefaults(preparePreview: false);
    }

    private void Update()
    {
        if (!IsSkippableState(_state))
        {
            return;
        }

        if (ReadSkipPressed())
        {
            RequestSkip();
        }
    }

    public void Initialize(
        SimplePlayerController newPlayerController,
        PlayerInteraction newPlayerInteraction,
        SimpleCameraFollow newCameraFollow,
        BattleHud newBattleHud)
    {
        playerController = newPlayerController;
        playerInteraction = newPlayerInteraction;
        cameraFollow = newCameraFollow;
        battleHud = newBattleHud;

        RefreshReferencesAndDefaults(preparePreview: true);
        HookPlayerEvents();
        ConfigureHud();
        SetState(EncounterState.Idle);
    }

    public void HandleEncounterActionRequested()
    {
        switch (_state)
        {
            case EncounterState.Idle:
            case EncounterState.Completed:
                BeginEncounter();
                break;
            case EncounterState.IntroTimeline:
            case EncounterState.IntroDialogue:
            case EncounterState.VictoryTimeline:
            case EncounterState.VictoryDialogue:
                RequestSkip();
                break;
            case EncounterState.Combat:
                ResetCombat();
                break;
        }
    }

    public void BeginEncounter()
    {
        if (_state != EncounterState.Idle && _state != EncounterState.Completed)
        {
            return;
        }

        TryAutoWire();
        if (playerController == null || playerInteraction == null || cameraFollow == null || bossPrefab == null || bossSpawnPoint == null)
        {
            Debug.LogWarning("BossEncounterDirector is missing required references.");
            return;
        }

        StartSequence(BeginEncounterRoutine());
    }

    public void ResetCombat()
    {
        if (_state != EncounterState.Combat && _state != EncounterState.Completed)
        {
            return;
        }

        StartSequence(ResetCombatRoutine());
    }

    public void RequestSkip()
    {
        if (!IsSkippableState(_state))
        {
            return;
        }

        _skipRequested = true;
        if (_state == EncounterState.IntroDialogue || _state == EncounterState.VictoryDialogue)
        {
            dialoguePanel?.SkipAll();
        }
    }

    private IEnumerator BeginEncounterRoutine()
    {
        // 조우 시작은 플레이어를 컷신 진입 상태로 고정하고 보스 액터를 시네마틱 모드로 준비합니다.
        _skipRequested = false;
        SetState(EncounterState.IntroTimeline);
        LockPlayerControl();
        MovePlayerToPosition(_encounterStartPosition);

        if (playerInteraction != null)
        {
            RestorePlayerForEncounter(_encounterStartPosition);
        }

        if (!PrepareBossActor(cinematicMode: true))
        {
            AbortEncounterToIdle();
            yield break;
        }

        yield return PlayIntroSequence();
        if (_state == EncounterState.Combat)
        {
            _sequenceRoutine = null;
            yield break;
        }

        if (_state != EncounterState.IntroTimeline)
        {
            _sequenceRoutine = null;
            yield break;
        }

        // 인트로 연출이 끝나면 필요할 때만 대화를 재생하고 바로 전투 상태로 넘깁니다.
        yield return PlayDialogueIfNeeded(introTimeline, introLines, EncounterState.IntroDialogue);

        StartCombat();
        _sequenceRoutine = null;
    }

    private IEnumerator ResetCombatRoutine()
    {
        // 실패 리셋은 체크포인트 복귀, 보스 재소환, 카메라 재정렬을 한 번에 처리합니다.
        SetState(EncounterState.FailureReset);
        LockPlayerControl();
        DisableCameraFollow();
        yield return null;

        _combatCheckpointPosition = ResolveCombatCheckpointPosition();
        if (playerInteraction != null)
        {
            RestorePlayerForEncounter(_combatCheckpointPosition);
        }
        else
        {
            MovePlayerToPosition(_combatCheckpointPosition);
        }

        if (!PrepareBossActor(cinematicMode: false))
        {
            AbortEncounterToIdle();
            yield break;
        }

        SnapCameraToCombatFrame();
        StartCombat();
        _sequenceRoutine = null;
    }

    private IEnumerator HandleVictoryRoutine()
    {
        // 승리 연출은 전투 입력을 끊고 컷신과 마무리 대사를 순서대로 진행합니다.
        _skipRequested = false;
        LockPlayerControl();
        DisableCameraFollow();
        SetState(EncounterState.VictoryTimeline);

        yield return PlayVictorySequence();
        if (_state != EncounterState.VictoryTimeline)
        {
            yield break;
        }

        yield return PlayDialogueIfNeeded(victoryTimeline, victoryLines, EncounterState.VictoryDialogue);

        SetState(EncounterState.Completed);
        DespawnCurrentBoss();
        _sequenceRoutine = null;
    }

    private IEnumerator PlayIntroSequence()
    {
        // Timeline이 있으면 자산 기준으로, 없으면 코드 기반 카메라 샷으로 인트로를 재생합니다.
        if (HasPlayableTimeline(introTimeline))
        {
            yield return PlayPlayableDirector(introTimeline, SnapCameraToCombatFrame);
            yield break;
        }

        Transform cameraTransform = ResolveCutsceneCameraTransform();
        if (cameraTransform == null || playerController == null || _currentBossObject == null)
        {
            yield break;
        }

        Vector3 wideView = BuildWideFrame(cameraTransform.position.z);
        Vector3 bossView = BuildBossFrame(cameraTransform.position.z);
        Vector3 combatView = BuildCombatFrame(cameraTransform.position.z);

        yield return AnimateCamera(cameraTransform, wideView, introWideDuration);
        if (_skipRequested) { yield break; }
        yield return AnimateCamera(cameraTransform, bossView, introBossDuration);
        if (_skipRequested) { yield break; }
        yield return HoldCutscene(introHoldDuration);
        if (_skipRequested) { yield break; }
        yield return AnimateCamera(cameraTransform, combatView, introWideDuration);
    }

    private IEnumerator PlayVictorySequence()
    {
        // 승리 연출도 Timeline 우선, 없으면 코드 기반 패닝으로 대체합니다.
        if (HasPlayableTimeline(victoryTimeline))
        {
            yield return PlayPlayableDirector(victoryTimeline, SnapCameraToVictoryFrame);
            yield break;
        }

        Transform cameraTransform = ResolveCutsceneCameraTransform();
        if (cameraTransform == null)
        {
            yield break;
        }

        yield return AnimateCamera(cameraTransform, BuildBossFrame(cameraTransform.position.z), victoryPanDuration);
        if (_skipRequested) { yield break; }
        yield return HoldCutscene(victoryHoldDuration);
    }

    private IEnumerator PlayPlayableDirector(PlayableDirector director, System.Action finalizePose)
    {
        // 재생 전 바인딩을 맞추고, 스킵이 들어오면 즉시 정지한 뒤 최종 포즈만 남깁니다.
        _skipRequested = false;
        PrepareTimelineBindings(director);
        director.time = 0d;
        director.Evaluate();
        director.Play();

        while (director.state == PlayState.Playing)
        {
            if (_skipRequested)
            {
                director.Stop();
                break;
            }

            yield return null;
        }

        finalizePose?.Invoke();
        dialoguePanel?.ClearTimelinePreview();
        _skipRequested = false;
    }

    private IEnumerator AnimateCamera(Transform cameraTransform, Vector3 targetPosition, float duration)
    {
        duration = Mathf.Max(0.01f, duration);
        Vector3 startPosition = cameraTransform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (_skipRequested)
            {
                cameraTransform.position = targetPosition;
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cameraTransform.position = Vector3.Lerp(startPosition, targetPosition, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        cameraTransform.position = targetPosition;
    }

    private IEnumerator HoldCutscene(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (_skipRequested)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void StartCombat()
    {
        // 실제 전투 시작 시점에만 체력, HUD, 카메라, 입력 상태를 전투용으로 되돌립니다.
        if (_state == EncounterState.Combat)
        {
            return;
        }

        _combatCheckpointPosition = ResolveCombatCheckpointPosition();
        if (playerInteraction != null)
        {
            RestorePlayerForEncounter(_combatCheckpointPosition);
        }

        if (_currentBossInteraction != null)
        {
            _currentBossInteraction.SetRespawnEnabled(false);
            _currentBossInteraction.RestoreFullHealth(notifyListeners: true, reactivateBehaviours: true);
        }

        if (_currentBossController != null)
        {
            _currentBossController.SetCombatActive(true);
        }

        SnapCameraToCombatFrame();
        EnableCameraFollow();
        UnlockPlayerControl();
        SetState(EncounterState.Combat);
    }

    private bool PrepareBossActor(bool cinematicMode)
    {
        // 보스 액터 준비는 기존 인스턴스를 비우고 새 인스턴스를 현재 조우에 다시 바인딩합니다.
        TryAutoWire();
        if (bossPrefab == null)
        {
            Debug.LogWarning("BossEncounterDirector is missing a boss prefab reference.", this);
            return false;
        }

        if (bossSpawnPoint == null)
        {
            Debug.LogWarning("BossEncounterDirector is missing a boss spawn point reference.", this);
            return false;
        }

        DespawnCurrentBoss();

        Transform spawnParent = bossParent;
        Vector3 spawnPosition = ResolveBossSpawnPosition();
        Quaternion spawnRotation = ResolveBossSpawnRotation();
        GameObject bossObject = Instantiate(bossPrefab.gameObject, spawnPosition, spawnRotation, spawnParent);
        bossObject.name = "BossActor";

        BossController controller = bossObject.GetComponent<BossController>();
        BossInteraction bossInteraction = bossObject.GetComponent<BossInteraction>();
        if (controller == null || bossInteraction == null)
        {
            Debug.LogWarning("Spawned boss prefab is missing BossController or BossInteraction.", bossObject);
            DestroyBossObject(bossObject);
            return false;
        }

        UnhookBossEvents();
        ConfigureBossActor(controller, bossInteraction, bossObject, cinematicMode);

        return true;
    }

    private void HookPlayerEvents()
    {
        if (playerInteraction == null)
        {
            return;
        }

        playerInteraction.Died -= HandlePlayerDied;
        playerInteraction.Respawned -= HandlePlayerRespawned;
        playerInteraction.Died += HandlePlayerDied;
        playerInteraction.Respawned += HandlePlayerRespawned;
    }

    private void UnhookPlayerEvents()
    {
        if (playerInteraction == null)
        {
            return;
        }

        playerInteraction.Died -= HandlePlayerDied;
        playerInteraction.Respawned -= HandlePlayerRespawned;
    }

    private void HookBossEvents()
    {
        if (_currentBossInteraction == null)
        {
            return;
        }

        _currentBossInteraction.Died -= HandleBossDied;
        _currentBossInteraction.Died += HandleBossDied;
    }

    private void UnhookBossEvents()
    {
        if (_currentBossInteraction == null)
        {
            return;
        }

        _currentBossInteraction.Died -= HandleBossDied;
    }

    private void HandlePlayerDied()
    {
        // 전투 중 플레이어 사망만 리셋 흐름으로 연결하고 컷신 중 사망은 무시합니다.
        if (_state != EncounterState.Combat)
        {
            return;
        }

        SetState(EncounterState.FailureReset);
        LockPlayerControl();
    }

    private void HandlePlayerRespawned()
    {
        if (_state != EncounterState.FailureReset)
        {
            return;
        }

        LockPlayerControl();
        StartSequence(ResetCombatRoutine());
    }

    private void HandleBossDied()
    {
        // 보스 사망 시에는 HUD 연결을 끊고 승리 루틴으로 넘어갑니다.
        if (_state != EncounterState.Combat)
        {
            return;
        }

        battleHud?.SetBossHealth(null);
        StartSequence(HandleVictoryRoutine());
    }

    private void LockPlayerControl()
    {
        if (playerInteraction != null)
        {
            playerInteraction.SetGameplayControlEnabled(false);
        }
        else if (playerController != null)
        {
            if (_playerBody == null)
            {
                _playerBody = playerController.GetComponent<Rigidbody2D>();
            }

            playerController.enabled = false;
            if (_playerBody != null)
            {
                _playerBody.linearVelocity = Vector2.zero;
            }
        }

        DisableCameraFollow();
    }

    private void UnlockPlayerControl()
    {
        if (playerInteraction != null)
        {
            playerInteraction.SetGameplayControlEnabled(true, clearVelocity: false);
        }
        else if (playerController != null)
        {
            playerController.enabled = true;
        }
    }

    private void DisableCameraFollow()
    {
        if (cameraFollow != null)
        {
            cameraFollow.enabled = false;
        }
    }

    private void EnableCameraFollow()
    {
        if (cameraFollow == null || playerController == null)
        {
            return;
        }

        cameraFollow.SetTarget(playerController.transform);
        cameraFollow.enabled = true;
    }

    private void MovePlayerToPosition(Vector3 position)
    {
        if (playerInteraction != null)
        {
            playerInteraction.MoveToPosition(position);
            return;
        }

        if (playerController == null)
        {
            return;
        }

        playerController.transform.position = position;
        if (_playerBody == null)
        {
            _playerBody = playerController.GetComponent<Rigidbody2D>();
        }

        if (_playerBody != null)
        {
            _playerBody.linearVelocity = Vector2.zero;
        }
    }

    private Vector3 ResolveCombatCheckpointPosition()
    {
        if (combatCheckpoint != null)
        {
            return combatCheckpoint.position;
        }

        if (playerController != null)
        {
            return playerController.transform.position;
        }

        return _combatCheckpointPosition;
    }

    private void TryAutoWire()
    {
        TryAutoWirePlayer();
        TryAutoWirePresentation();
        EnsureDialoguePanelReference();
        EnsureDialogueManagerReference();
        EnsureBossPrefabReference();
    }

    private void RefreshSceneSetup()
    {
        CacheEncounterStartPosition();
        CacheCombatCheckpointPosition();
        CacheBossSpawnPose();
    }

    private void ConfigureHud()
    {
        if (battleHud == null)
        {
            return;
        }

        battleHud.Initialize(playerInteraction, GetDisplayedBossInteraction());
    }

    private void SeedDefaultDialogue()
    {
        if (HasLegacyIntroDefaults())
        {
            introLines.Clear();
        }

        if (introLines.Count == 0)
        {
            introLines.Add(CreateDialogueLine("사령관", "결국 여기까지 올라왔군. 이 구역은 이제 내 손안이야.", EncounterPortraitSide.Right));
            introLines.Add(CreateDialogueLine("플레이어", "그럼 여기서 끝내주지. 네 판도 같이.", EncounterPortraitSide.Left));
        }

        if (HasLegacyVictoryDefaults())
        {
            victoryLines.Clear();
        }

        if (victoryLines.Count == 0)
        {
            victoryLines.Add(CreateDialogueLine("사령관", "큭... 설마 내가 여기서 멈출 줄이야.", EncounterPortraitSide.Right));
            victoryLines.Add(CreateDialogueLine("플레이어", "이제 끝이야. 다시 일어날 생각은 하지 마.", EncounterPortraitSide.Left));
        }
    }

    public void HandleHudButtonPressed()
    {
        HandleEncounterActionRequested();
    }

    private void RefreshHudState()
    {
        battleHud?.SetBossHealth(GetDisplayedBossInteraction());
    }

    private void SetState(EncounterState newState)
    {
        _state = newState;
        ApplyInputModeForState(newState);
        RefreshHudState();
    }

    private bool IsSkippableState(EncounterState state)
    {
        return state == EncounterState.IntroTimeline
            || state == EncounterState.IntroDialogue
            || state == EncounterState.VictoryTimeline
            || state == EncounterState.VictoryDialogue;
    }

    private Vector3 BuildWideFrame(float z)
    {
        Vector3 playerPosition = playerController != null ? playerController.transform.position : Vector3.zero;
        Transform bossTransform = GetBossActorTransform();
        Vector3 bossPosition = bossTransform != null ? bossTransform.position : playerPosition + Vector3.right * 6f;
        return new Vector3((playerPosition.x + bossPosition.x) * 0.5f, Mathf.Max(playerPosition.y, bossPosition.y) + 1.2f, z);
    }

    private Vector3 BuildBossFrame(float z)
    {
        Transform bossTransform = GetBossActorTransform();
        Vector3 bossPosition = bossTransform != null ? bossTransform.position : Vector3.zero;
        return new Vector3(bossPosition.x - 1.15f, bossPosition.y + 1.1f, z);
    }

    private Vector3 BuildCombatFrame(float z)
    {
        Vector3 playerPosition = playerController != null ? playerController.transform.position : Vector3.zero;
        return new Vector3(playerPosition.x + 1.2f, playerPosition.y + 1f, z);
    }

    public bool TryGetTimelineCameraTransform(out Transform cameraTransform)
    {
        cameraTransform = ResolveCutsceneCameraTransform();
        return cameraTransform != null;
    }

    public Vector3 GetTimelineCameraFramePosition(EncounterCameraFrameType frameType, Vector2 offset, float z)
    {
        Vector3 basePosition = frameType switch
        {
            EncounterCameraFrameType.Wide => BuildWideFrame(z),
            EncounterCameraFrameType.Boss => BuildBossFrame(z),
            EncounterCameraFrameType.Combat => BuildCombatFrame(z),
            _ => BuildCombatFrame(z)
        };

        return basePosition + new Vector3(offset.x, offset.y, 0f);
    }

    private void SnapCameraToCombatFrame()
    {
        Transform cameraTransform = ResolveCutsceneCameraTransform();
        if (cameraTransform == null)
        {
            return;
        }

        cameraTransform.position = BuildCombatFrame(cameraTransform.position.z);
    }

    private void SnapCameraToVictoryFrame()
    {
        Transform cameraTransform = ResolveCutsceneCameraTransform();
        if (cameraTransform == null)
        {
            return;
        }

        cameraTransform.position = BuildBossFrame(cameraTransform.position.z);
    }

    private static bool ReadSkipPressed()
    {
        return GameInput.Instance.DialogueSkipPressed;
    }

    private void PrepareTimelineBindings(PlayableDirector director)
    {
        if (!HasPlayableTimeline(director))
        {
            return;
        }

        TryAutoWire();
        foreach (PlayableBinding output in director.playableAsset.outputs)
        {
            BindTimelineOutput(director, output);
        }
    }

    private static bool TimelineContainsDialogue(PlayableDirector director)
    {
        if (!HasPlayableTimeline(director))
        {
            return false;
        }

        foreach (PlayableBinding output in director.playableAsset.outputs)
        {
            if (output.sourceObject is not EncounterDialogueTrack track)
            {
                continue;
            }

            foreach (TimelineClip _ in track.GetClips())
            {
                return true;
            }
        }

        return false;
    }

    private bool HasLegacyIntroDefaults()
    {
        return introLines.Count == 2
            && MatchesDialogue(introLines[0], "Commander", "You finally made it here. This district belongs to me.")
            && MatchesDialogue(introLines[1], "Player", "Then I will shut this place down myself.");
    }

    private bool HasLegacyVictoryDefaults()
    {
        return victoryLines.Count == 2
            && MatchesDialogue(victoryLines[0], "Commander", "Tch... So this is where my run ends.")
            && MatchesDialogue(victoryLines[1], "Player", "Stay down. We are done here.");
    }

    private static bool MatchesDialogue(EncounterDialogueLine line, string speakerName, string text)
    {
        return line != null
            && string.Equals(line.speakerName, speakerName, System.StringComparison.Ordinal)
            && string.Equals(line.text, text, System.StringComparison.Ordinal);
    }

    private static EncounterDialogueLine CreateDialogueLine(string speakerName, string text, EncounterPortraitSide portraitSide)
    {
        return new EncounterDialogueLine
        {
            speakerName = speakerName,
            text = text,
            portraitSide = portraitSide
        };
    }

    private void TryBindVisualTrack(PlayableDirector director, PlayableBinding output)
    {
        bool isPlayerTrack = IsNamedTrack(output, "player");
        bool isBossTrack = IsNamedTrack(output, "boss");

        if (!isPlayerTrack && !isBossTrack)
        {
            return;
        }

        System.Type targetType = output.outputTargetType;
        if (targetType == typeof(Animator))
        {
            Animator animator = isPlayerTrack ? ResolvePlayerVisualAnimator() : ResolveBossVisualAnimator();
            if (animator != null)
            {
                director.SetGenericBinding(output.sourceObject, animator);
            }

            return;
        }

        if (targetType == typeof(GameObject))
        {
            GameObject targetObject = isPlayerTrack
                ? playerController != null ? playerController.gameObject : null
                : ResolveBossBindingObject();

            if (targetObject != null)
            {
                director.SetGenericBinding(output.sourceObject, targetObject);
            }
        }
    }

    private void RefreshIdlePresentation()
    {
        battleHud?.SetBossHealth(null);
        dialoguePanel?.ClearTimelinePreview();
    }

    private Transform GetBossActorTransform()
    {
        if (_currentBossObject != null)
        {
            return _currentBossObject.transform;
        }

        return bossSpawnPoint;
    }

    private Animator ResolvePlayerVisualAnimator()
    {
        if (playerController == null)
        {
            return null;
        }

        Transform visualRoot = playerController.VisualRoot;
        if (visualRoot == null)
        {
            visualRoot = playerController.transform.Find("Visual");
        }

        return visualRoot != null ? visualRoot.GetComponent<Animator>() : null;
    }

    private Animator ResolveBossVisualAnimator()
    {
        if (_currentBossController != null && _currentBossController.VisualRoot != null)
        {
            Animator currentAnimator = _currentBossController.VisualRoot.GetComponent<Animator>();
            if (currentAnimator != null)
            {
                return currentAnimator;
            }
        }

        return null;
    }

    private void RefreshReferencesAndDefaults(bool preparePreview)
    {
        SeedDefaultDialogue();
        TryAutoWire();
        RefreshSceneSetup();
        if (preparePreview)
        {
            RefreshIdlePresentation();
        }
    }

    private void StartSequence(IEnumerator routine)
    {
        StopSequence();
        _sequenceRoutine = StartCoroutine(routine);
    }

    private void StopSequence()
    {
        if (_sequenceRoutine == null)
        {
            return;
        }

        StopCoroutine(_sequenceRoutine);
        _sequenceRoutine = null;
    }

    private void AbortEncounterToIdle()
    {
        UnlockPlayerControl();
        EnableCameraFollow();
        DespawnCurrentBoss();
        SetState(EncounterState.Idle);
        _sequenceRoutine = null;
    }

    private void ApplyInputModeForState(EncounterState state)
    {
        // 조우 상태 하나가 입력 모드 하나를 결정하도록 매핑을 한곳에 모읍니다.
        switch (state)
        {
            case EncounterState.IntroTimeline:
            case EncounterState.IntroDialogue:
            case EncounterState.VictoryTimeline:
            case EncounterState.VictoryDialogue:
                GameInput.Instance.EnableDialogue();
                break;
            case EncounterState.FailureReset:
                GameInput.Instance.DisableAllGameplayInput();
                break;
            default:
                GameInput.Instance.EnableGameplay();
                break;
        }
    }

    private void RestorePlayerForEncounter(Vector3 position)
    {
        if (playerInteraction != null)
        {
            playerInteraction.RestoreAtPosition(position, notifyListeners: true, reactivateBehaviours: false);
            return;
        }

        MovePlayerToPosition(position);
    }

    private IEnumerator PlayDialogueIfNeeded(
        PlayableDirector timelineDirector,
        List<EncounterDialogueLine> lines,
        EncounterState dialogueState)
    {
        if (TimelineContainsDialogue(timelineDirector) || dialogueManager == null || lines.Count == 0)
        {
            yield break;
        }

        _skipRequested = false;
        SetState(dialogueState);

        bool completed = false;
        if (!dialogueManager.TryPlay(CreateRuntimeDialogueLines(lines), new DialoguePlaybackContext
            {
                manageInputMode = false,
                lockPlayerControlOverride = false,
                disableCameraFollowOverride = false,
                onCompleted = () => completed = true
            }))
        {
            yield break;
        }

        while (!completed)
        {
            yield return null;
        }
    }

    private static List<DialogueLineData> CreateRuntimeDialogueLines(IReadOnlyList<EncounterDialogueLine> lines)
    {
        List<DialogueLineData> runtimeLines = new List<DialogueLineData>(lines.Count);
        for (int i = 0; i < lines.Count; i++)
        {
            EncounterDialogueLine line = lines[i];
            if (line == null)
            {
                continue;
            }

            runtimeLines.Add(new DialogueLineData
            {
                speakerName = line.speakerName,
                text = line.text,
                portraitSprite = line.portraitSprite,
                portraitSide = line.portraitSide == EncounterPortraitSide.Right
                    ? DialoguePortraitSide.Right
                    : DialoguePortraitSide.Left
            });
        }

        return runtimeLines;
    }

    private void ConfigureBossActor(
        BossController controller,
        BossInteraction bossInteraction,
        GameObject bossObject,
        bool cinematicMode)
    {
        bossObject.SetActive(true);
        ApplyPreparedBossPose(controller, bossObject);
        controller.Initialize(playerController != null ? playerController.transform : null);
        controller.SetCombatActive(!cinematicMode);
        bossInteraction.SetRespawnEnabled(false);
        bossInteraction.RestoreFullHealth(notifyListeners: true, reactivateBehaviours: true);

        _currentBossObject = bossObject;
        _currentBossController = controller;
        _currentBossInteraction = bossInteraction;
        HookBossEvents();
        battleHud?.SetBossHealth(GetDisplayedBossInteraction());
    }

    private BossInteraction GetDisplayedBossInteraction()
    {
        return _currentBossInteraction;
    }

    private void TryAutoWirePlayer()
    {
        playerController ??= Object.FindFirstObjectByType<SimplePlayerController>();
        playerInteraction ??= Object.FindFirstObjectByType<PlayerInteraction>();

        if (playerController == null && playerInteraction == null)
        {
            return;
        }

        if (playerController == null && playerInteraction != null)
        {
            playerController = playerInteraction.GetComponent<SimplePlayerController>();
        }

        if (playerInteraction == null && playerController != null)
        {
            playerInteraction = playerController.GetComponent<PlayerInteraction>();
        }

        _playerBody ??= playerController != null ? playerController.GetComponent<Rigidbody2D>() : null;
    }

    private void TryAutoWirePresentation()
    {
        cameraFollow ??= Object.FindFirstObjectByType<SimpleCameraFollow>();
        battleHud ??= Object.FindFirstObjectByType<BattleHud>();
        dialoguePanel ??= Object.FindFirstObjectByType<EncounterDialoguePanel>();
        dialogueManager ??= Object.FindFirstObjectByType<DialogueManager>();
    }

    private void EnsureDialoguePanelReference()
    {
        if (dialoguePanel != null)
        {
            return;
        }

        GameObject dialogueObject = GameObject.Find(DefaultDialogueObjectName);
        if (dialogueObject != null)
        {
            dialoguePanel = dialogueObject.GetComponent<EncounterDialoguePanel>();
        }

        if (dialoguePanel == null)
        {
            Debug.LogWarning("BossEncounterDirector could not find EncounterDialoguePanel in the scene. Place the dialogue UI in the scene and assign it instead of relying on runtime creation.", this);
        }
    }

    private void EnsureDialogueManagerReference()
    {
        dialogueManager ??= Object.FindFirstObjectByType<DialogueManager>();
        if (dialogueManager == null)
        {
            GameObject runnerObject = GameObject.Find("DialogueManager");
            if (runnerObject != null)
            {
                dialogueManager = runnerObject.GetComponent<DialogueManager>();
            }
        }

        if (dialogueManager == null)
        {
            Debug.LogWarning(
                "BossEncounterDirector could not find DialogueManager in the scene. Place a DialogueManager object in the scene and assign it instead of relying on runtime creation.",
                this);
            return;
        }

        dialogueManager.BindView(dialoguePanel);
        dialogueManager.BindReferences(playerInteraction, cameraFollow);
    }

    private void EnsureBossPrefabReference()
    {
#if UNITY_EDITOR
        if (bossPrefab == null)
        {
            bossPrefab = AssetDatabase.LoadAssetAtPath<BossController>(DefaultBossPrefabAssetPath);
        }
#endif
    }

    private void CacheEncounterStartPosition()
    {
        if (playerController != null && _encounterStartPosition == Vector3.zero)
        {
            _encounterStartPosition = playerController.transform.position;
        }
    }

    private void CacheCombatCheckpointPosition()
    {
        if (combatCheckpoint != null)
        {
            _combatCheckpointPosition = combatCheckpoint.position;
            return;
        }

        if (_combatCheckpointPosition == Vector3.zero && playerController != null)
        {
            _combatCheckpointPosition = playerController.transform.position;
        }
    }

    private void CacheBossSpawnPose()
    {
        if (bossSpawnPoint == null)
        {
            return;
        }

        _bossSpawnPosition = bossSpawnPoint.position;
        _bossSpawnRotation = bossSpawnPoint.rotation;
    }

    private Vector3 ResolveBossSpawnPosition()
    {
        if (bossSpawnPoint != null)
        {
            return bossSpawnPoint.position;
        }

        return _bossSpawnPosition;
    }

    private Quaternion ResolveBossSpawnRotation()
    {
        if (bossSpawnPoint != null)
        {
            return bossSpawnPoint.rotation;
        }

        return _bossSpawnRotation;
    }

    private Transform ResolveCutsceneCameraTransform()
    {
        return cameraFollow != null ? cameraFollow.transform : Camera.main != null ? Camera.main.transform : null;
    }

    private static bool HasPlayableTimeline(PlayableDirector director)
    {
        return director != null && director.playableAsset != null;
    }

    private void BindTimelineOutput(PlayableDirector director, PlayableBinding output)
    {
        if (TryBindDialogueTrack(director, output) || TryBindCameraTrack(director, output))
        {
            return;
        }

        TryBindVisualTrack(director, output);
    }

    private bool TryBindDialogueTrack(PlayableDirector director, PlayableBinding output)
    {
        if (output.sourceObject is not EncounterDialogueTrack || dialoguePanel == null)
        {
            return false;
        }

        director.SetGenericBinding(output.sourceObject, dialoguePanel);
        return true;
    }

    private bool TryBindCameraTrack(PlayableDirector director, PlayableBinding output)
    {
        if (output.sourceObject is not EncounterCameraTrack)
        {
            return false;
        }

        director.SetGenericBinding(output.sourceObject, this);
        return true;
    }

    private static bool IsNamedTrack(PlayableBinding output, string keyword)
    {
        string streamName = output.streamName ?? string.Empty;
        return streamName.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private GameObject ResolveBossBindingObject()
    {
        return _currentBossObject;
    }

    private void ApplyPreparedBossPose(BossController controller, GameObject bossObject)
    {
        if (bossObject == null || controller == null)
        {
            return;
        }

        controller.transform.SetPositionAndRotation(_bossSpawnPosition, _bossSpawnRotation);
    }

    private void DespawnCurrentBoss()
    {
        if (_currentBossObject == null)
        {
            ClearCurrentBossReferences();
            battleHud?.SetBossHealth(null);
            return;
        }

        GameObject bossObject = _currentBossObject;
        UnhookBossEvents();
        ClearCurrentBossReferences();
        battleHud?.SetBossHealth(null);

        if (bossObject != null)
        {
            bossObject.SetActive(false);
            DestroyBossObject(bossObject);
        }
    }

    private void ClearCurrentBossReferences()
    {
        _currentBossObject = null;
        _currentBossController = null;
        _currentBossInteraction = null;
    }

    private static void DestroyBossObject(GameObject bossObject)
    {
        if (bossObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(bossObject);
            return;
        }

        DestroyImmediate(bossObject);
    }
}
