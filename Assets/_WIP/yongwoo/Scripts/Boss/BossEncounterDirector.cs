using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class BossEncounterDirector : MonoBehaviour
{
    private const string DefaultBossPrefabAssetPath = "Assets/_WIP/yongwoo/Prefabs/Prototype/Boss.prefab";
    private const string DefaultGameplayRootName = "Gameplay";
    private const string DefaultDialogueObjectName = "EncounterDialoguePanel";

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
    [SerializeField] private SimplePlayerController playerController;
    [SerializeField] private SimplePlayerCombat playerCombat;
    [SerializeField] private PrototypeHealth playerHealth;
    [SerializeField] private SimpleCameraFollow cameraFollow;
    [SerializeField] private PrototypeBattleHud battleHud;
    [SerializeField] private EncounterDialoguePanel dialoguePanel;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Transform bossParent;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private Transform combatCheckpoint;

    [Header("Optional Timelines")]
    [SerializeField] private PlayableDirector introTimeline;
    [SerializeField] private PlayableDirector victoryTimeline;

    [Header("Fallback Cutscene")]
    [SerializeField] private float introWideDuration = 0.6f;
    [SerializeField] private float introBossDuration = 0.7f;
    [SerializeField] private float introHoldDuration = 0.4f;
    [SerializeField] private float victoryPanDuration = 0.65f;
    [SerializeField] private float victoryHoldDuration = 0.85f;

    [Header("Dialogue")]
    [SerializeField] private List<EncounterDialogueLine> introLines = new();
    [SerializeField] private List<EncounterDialogueLine> victoryLines = new();

    private Rigidbody2D _playerBody;
    private PrototypeBossController _currentBossController;
    private PrototypeHealth _currentBossHealth;
    private GameObject _currentBossObject;
    private Coroutine _sequenceRoutine;
    private bool _skipRequested;
    private Vector3 _encounterStartPosition;
    private Vector3 _combatCheckpointPosition;
    private EncounterState _state;

    public EncounterState CurrentState => _state;
    public PrototypeHealth CurrentBossHealth => _currentBossHealth;
    public bool IsHudButtonInteractable => _state != EncounterState.FailureReset;
    public string CurrentHudButtonLabel
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

    private void Awake()
    {
        SeedDefaultDialogue();
        TryAutoWire();
    }

    private void OnEnable()
    {
        TryAutoWire();
        HookPlayerEvents();
        battleHud?.BindEncounterDirector(this);
        RefreshHudState();
    }

    private void OnDisable()
    {
        UnhookPlayerEvents();
        UnhookBossEvents();
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
        SimplePlayerCombat newPlayerCombat,
        PrototypeHealth newPlayerHealth,
        SimpleCameraFollow newCameraFollow,
        PrototypeBattleHud newBattleHud)
    {
        playerController = newPlayerController;
        playerCombat = newPlayerCombat;
        playerHealth = newPlayerHealth;
        cameraFollow = newCameraFollow;
        battleHud = newBattleHud;

        SeedDefaultDialogue();
        TryAutoWire();
        HookPlayerEvents();

        if (playerController != null)
        {
            _encounterStartPosition = playerController.transform.position;
            _combatCheckpointPosition = ResolveCombatCheckpointPosition();
        }

        battleHud?.BindEncounterDirector(this);
        battleHud?.SetBossHealth(_currentBossHealth);
        RefreshHudState();
        SetState(EncounterState.Idle);
    }

    public void HandleHudButtonPressed()
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
        if (playerController == null || playerHealth == null || cameraFollow == null)
        {
            Debug.LogWarning("BossEncounterDirector is missing required references.");
            return;
        }

        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
        }

        _sequenceRoutine = StartCoroutine(BeginEncounterRoutine());
    }

    public void ResetCombat()
    {
        if (_state != EncounterState.Combat && _state != EncounterState.Completed)
        {
            return;
        }

        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
        }

        _sequenceRoutine = StartCoroutine(ResetCombatRoutine());
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
        _skipRequested = false;
        SetState(EncounterState.IntroTimeline);
        DespawnCurrentBoss();
        LockPlayerControl();
        MovePlayerToPosition(_encounterStartPosition);

        if (playerHealth != null)
        {
            playerHealth.SetSpawnPosition(_encounterStartPosition);
            playerHealth.RestoreFullHealth(notifyListeners: true, reactivateBehaviours: false);
        }

        if (!SpawnBoss(cinematicMode: true))
        {
            UnlockPlayerControl();
            EnableCameraFollow();
            SetState(EncounterState.Idle);
            yield break;
        }

        yield return PlayIntroSequence();
        if (_state != EncounterState.IntroTimeline)
        {
            yield break;
        }

        _skipRequested = false;
        SetState(EncounterState.IntroDialogue);
        if (dialoguePanel != null && introLines.Count > 0)
        {
            bool completed = false;
            dialoguePanel.Play(introLines, () => completed = true);
            while (!completed)
            {
                yield return null;
            }
        }

        StartCombat();
        _sequenceRoutine = null;
    }

    private IEnumerator ResetCombatRoutine()
    {
        SetState(EncounterState.FailureReset);
        LockPlayerControl();
        DisableCameraFollow();
        yield return null;

        _combatCheckpointPosition = ResolveCombatCheckpointPosition();
        MovePlayerToPosition(_combatCheckpointPosition);
        if (playerHealth != null)
        {
            playerHealth.SetSpawnPosition(_combatCheckpointPosition);
            playerHealth.RestoreFullHealth(notifyListeners: true, reactivateBehaviours: false);
        }

        DespawnCurrentBoss();
        if (!SpawnBoss(cinematicMode: false))
        {
            UnlockPlayerControl();
            EnableCameraFollow();
            SetState(EncounterState.Idle);
            yield break;
        }

        SnapCameraToCombatFrame();
        StartCombat();
        _sequenceRoutine = null;
    }

    private IEnumerator HandleVictoryRoutine()
    {
        _skipRequested = false;
        LockPlayerControl();
        DisableCameraFollow();
        SetState(EncounterState.VictoryTimeline);

        yield return PlayVictorySequence();
        if (_state != EncounterState.VictoryTimeline)
        {
            yield break;
        }

        _skipRequested = false;
        SetState(EncounterState.VictoryDialogue);
        if (dialoguePanel != null && victoryLines.Count > 0)
        {
            bool completed = false;
            dialoguePanel.Play(victoryLines, () => completed = true);
            while (!completed)
            {
                yield return null;
            }
        }

        SetState(EncounterState.Completed);
        _sequenceRoutine = null;
    }

    private IEnumerator PlayIntroSequence()
    {
        if (introTimeline != null && introTimeline.playableAsset != null)
        {
            yield return PlayPlayableDirector(introTimeline, SnapCameraToCombatFrame);
            yield break;
        }

        Transform cameraTransform = cameraFollow != null ? cameraFollow.transform : Camera.main != null ? Camera.main.transform : null;
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
        if (victoryTimeline != null && victoryTimeline.playableAsset != null)
        {
            yield return PlayPlayableDirector(victoryTimeline, SnapCameraToVictoryFrame);
            yield break;
        }

        Transform cameraTransform = cameraFollow != null ? cameraFollow.transform : Camera.main != null ? Camera.main.transform : null;
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
        _skipRequested = false;
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
        _combatCheckpointPosition = ResolveCombatCheckpointPosition();
        if (playerHealth != null)
        {
            playerHealth.SetSpawnPosition(_combatCheckpointPosition);
            playerHealth.RestoreFullHealth(notifyListeners: true, reactivateBehaviours: false);
        }

        if (_currentBossHealth != null)
        {
            _currentBossHealth.SetRespawnEnabled(false);
            _currentBossHealth.RestoreFullHealth(notifyListeners: true, reactivateBehaviours: true);
        }

        if (_currentBossController != null)
        {
            _currentBossController.SetCombatActive(true);
        }

        SnapCameraToCombatFrame();
        EnableCameraFollow();
        UnlockPlayerControl();
        battleHud?.SetBossHealth(_currentBossHealth);
        SetState(EncounterState.Combat);
    }

    private bool SpawnBoss(bool cinematicMode)
    {
        GameObject prefab = ResolveBossPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("BossEncounterDirector could not find the boss prefab.");
            return false;
        }

        TryAutoWire();
        PrototypeBossController prefabController = prefab.GetComponent<PrototypeBossController>();
        PrototypeBossConfig config = prefabController != null
            ? prefabController.RuntimeConfig
            : PrototypeBossConfigLoader.CreateDefault();

        Vector3 spawnPosition = ResolveBossSpawnPosition(config);
        GameObject bossObject = Instantiate(prefab, spawnPosition, Quaternion.identity, bossParent);
        PrototypeBossController controller = bossObject.GetComponent<PrototypeBossController>();
        PrototypeHealth health = bossObject.GetComponent<PrototypeHealth>();
        if (controller == null || health == null)
        {
            Destroy(bossObject);
            Debug.LogWarning("Boss prefab is missing PrototypeBossController or PrototypeHealth.");
            return false;
        }

        controller.Initialize(playerController != null ? playerController.transform : null);
        controller.SetCombatActive(!cinematicMode);
        health.SetRespawnEnabled(false);

        _currentBossObject = bossObject;
        _currentBossController = controller;
        _currentBossHealth = health;
        HookBossEvents();
        battleHud?.SetBossHealth(_currentBossHealth);
        return true;
    }

    private void DespawnCurrentBoss()
    {
        UnhookBossEvents();
        if (_currentBossObject != null)
        {
            Destroy(_currentBossObject);
        }

        _currentBossObject = null;
        _currentBossController = null;
        _currentBossHealth = null;
        battleHud?.SetBossHealth(null);
    }

    private void HookPlayerEvents()
    {
        if (playerHealth == null)
        {
            return;
        }

        playerHealth.Died -= HandlePlayerDied;
        playerHealth.Respawned -= HandlePlayerRespawned;
        playerHealth.Died += HandlePlayerDied;
        playerHealth.Respawned += HandlePlayerRespawned;
    }

    private void UnhookPlayerEvents()
    {
        if (playerHealth == null)
        {
            return;
        }

        playerHealth.Died -= HandlePlayerDied;
        playerHealth.Respawned -= HandlePlayerRespawned;
    }

    private void HookBossEvents()
    {
        if (_currentBossHealth == null)
        {
            return;
        }

        _currentBossHealth.Died -= HandleBossDied;
        _currentBossHealth.Died += HandleBossDied;
    }

    private void UnhookBossEvents()
    {
        if (_currentBossHealth == null)
        {
            return;
        }

        _currentBossHealth.Died -= HandleBossDied;
    }

    private void HandlePlayerDied()
    {
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
        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
        }

        _sequenceRoutine = StartCoroutine(ResetCombatRoutine());
    }

    private void HandleBossDied()
    {
        if (_state != EncounterState.Combat)
        {
            return;
        }

        if (_sequenceRoutine != null)
        {
            StopCoroutine(_sequenceRoutine);
        }

        _sequenceRoutine = StartCoroutine(HandleVictoryRoutine());
    }

    private void LockPlayerControl()
    {
        if (playerController == null)
        {
            return;
        }

        if (_playerBody == null)
        {
            _playerBody = playerController.GetComponent<Rigidbody2D>();
        }

        playerController.enabled = false;
        if (playerCombat != null)
        {
            playerCombat.enabled = false;
        }

        if (_playerBody != null)
        {
            _playerBody.linearVelocity = Vector2.zero;
        }

        DisableCameraFollow();
    }

    private void UnlockPlayerControl()
    {
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        if (playerCombat != null)
        {
            playerCombat.enabled = true;
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

    private Vector3 ResolveBossSpawnPosition(PrototypeBossConfig config)
    {
        if (bossSpawnPoint != null)
        {
            return bossSpawnPoint.position;
        }

        float spawnX = Mathf.Clamp(config.core.arenaRight - 1.5f, config.core.arenaLeft, config.core.arenaRight);
        float spawnY = config.core.groundY;

        if (bossParent != null)
        {
            return new Vector3(spawnX, spawnY, bossParent.position.z);
        }

        return new Vector3(spawnX, spawnY, 0f);
    }

    private GameObject ResolveBossPrefab()
    {
        if (bossPrefab != null)
        {
            return bossPrefab;
        }

#if UNITY_EDITOR
        bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultBossPrefabAssetPath);
#endif
        return bossPrefab;
    }

    private void TryAutoWire()
    {
        playerController ??= Object.FindFirstObjectByType<SimplePlayerController>();
        if (playerController != null)
        {
            playerCombat ??= playerController.GetComponent<SimplePlayerCombat>();
            playerHealth ??= playerController.GetComponent<PrototypeHealth>();
            _playerBody ??= playerController.GetComponent<Rigidbody2D>();
        }

        cameraFollow ??= Object.FindFirstObjectByType<SimpleCameraFollow>();
        battleHud ??= Object.FindFirstObjectByType<PrototypeBattleHud>();
        dialoguePanel ??= Object.FindFirstObjectByType<EncounterDialoguePanel>();

        if (dialoguePanel == null)
        {
            GameObject dialogueObject = GameObject.Find(DefaultDialogueObjectName) ?? new GameObject(DefaultDialogueObjectName, typeof(RectTransform));
            dialoguePanel = dialogueObject.GetComponent<EncounterDialoguePanel>();
            if (dialoguePanel == null)
            {
                dialoguePanel = dialogueObject.AddComponent<EncounterDialoguePanel>();
            }
        }

        if (bossParent == null)
        {
            GameObject gameplayRoot = GameObject.Find(DefaultGameplayRootName);
            bossParent = gameplayRoot != null ? gameplayRoot.transform : null;
        }

        if (playerController != null && _encounterStartPosition == Vector3.zero)
        {
            _encounterStartPosition = playerController.transform.position;
        }

        if (combatCheckpoint != null)
        {
            _combatCheckpointPosition = combatCheckpoint.position;
        }
        else if (_combatCheckpointPosition == Vector3.zero && playerController != null)
        {
            _combatCheckpointPosition = playerController.transform.position;
        }
    }

    private void SeedDefaultDialogue()
    {
        if (introLines.Count == 0)
        {
            introLines.Add(new EncounterDialogueLine
            {
                speakerName = "Commander",
                text = "You finally made it here. This district belongs to me.",
                portraitSide = EncounterPortraitSide.Right
            });
            introLines.Add(new EncounterDialogueLine
            {
                speakerName = "Player",
                text = "Then I will shut this place down myself.",
                portraitSide = EncounterPortraitSide.Left
            });
        }

        if (victoryLines.Count == 0)
        {
            victoryLines.Add(new EncounterDialogueLine
            {
                speakerName = "Commander",
                text = "Tch... So this is where my run ends.",
                portraitSide = EncounterPortraitSide.Right
            });
            victoryLines.Add(new EncounterDialogueLine
            {
                speakerName = "Player",
                text = "Stay down. We are done here.",
                portraitSide = EncounterPortraitSide.Left
            });
        }
    }

    private void RefreshHudState()
    {
        battleHud?.SetEncounterButtonState(CurrentHudButtonLabel, IsHudButtonInteractable);
    }

    private void SetState(EncounterState newState)
    {
        _state = newState;
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
        Vector3 bossPosition = _currentBossObject != null ? _currentBossObject.transform.position : playerPosition + Vector3.right * 6f;
        return new Vector3((playerPosition.x + bossPosition.x) * 0.5f, Mathf.Max(playerPosition.y, bossPosition.y) + 1.2f, z);
    }

    private Vector3 BuildBossFrame(float z)
    {
        Vector3 bossPosition = _currentBossObject != null ? _currentBossObject.transform.position : Vector3.zero;
        return new Vector3(bossPosition.x - 1.15f, bossPosition.y + 1.1f, z);
    }

    private Vector3 BuildCombatFrame(float z)
    {
        Vector3 playerPosition = playerController != null ? playerController.transform.position : Vector3.zero;
        return new Vector3(playerPosition.x + 1.2f, playerPosition.y + 1f, z);
    }

    private void SnapCameraToCombatFrame()
    {
        Transform cameraTransform = cameraFollow != null ? cameraFollow.transform : Camera.main != null ? Camera.main.transform : null;
        if (cameraTransform == null)
        {
            return;
        }

        cameraTransform.position = BuildCombatFrame(cameraTransform.position.z);
    }

    private void SnapCameraToVictoryFrame()
    {
        Transform cameraTransform = cameraFollow != null ? cameraFollow.transform : Camera.main != null ? Camera.main.transform : null;
        if (cameraTransform == null)
        {
            return;
        }

        cameraTransform.position = BuildBossFrame(cameraTransform.position.z);
    }

    private static bool ReadSkipPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.tabKey.wasPressedThisFrame
                || Keyboard.current.escapeKey.wasPressedThisFrame;
        }
#endif
        return Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape);
    }
}
