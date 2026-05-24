using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

// 역할:
// - 씬에 배치한 이벤트 단계를 순서대로 실행하는 얇은 진행 컴포넌트입니다.
// - 튜토리얼 시작처럼 "트리거/이벤트 기반"으로 흘러가는 장면에 사용합니다.

[DisallowMultipleComponent]
public class SceneEventSequence : MonoBehaviour
{
    public enum StepType
    {
        Delay,
        LockPlayer,
        UnlockPlayer,
        ShowSystemLog,
        HideSystemLog,
        SetSystemLogAlpha,
        FadeSystemLog,
        ShowCommsLine,
        HideComms,
        SetObjectActive,
        WaitForInput,
        TeleportPlayer,
        SnapCamera,
        WaitForEnemiesDead,
        FadeOut,
        FadeIn,
        PlaySequence,
        CameraShake,
        GlitchPulse,
        GlitchFade,
        ShowProgressLog,
        HoldInteractProgress,
        CameraFocus,
        FreezeTime,
        UnfreezeTime,
        SetCameraTarget,
        LockInput,
        UnlockInput,
        PlayCutsceneVideo
    }

    public enum InputWaitType
    {
        AnyKey,
        Move,
        Jump,
        Dash,
        Attack,
        Interact,
        Roll
    }

    [System.Serializable]
    public class Step
    {
        public StepType type;
        public string speaker;
        [TextArea(1, 3)] public string message;
        [Min(0f)] public float duration;
        [Range(0f, 1f)] public float alpha = 1f;
        public GameObject targetObject;
        public bool active;
        public InputWaitType inputWaitType;
        public Transform targetTransform;
        public GameObject[] watchTargets;
        public SceneEventSequence targetSequence;
        public bool waitForCompletion = true;
        [Min(0f)] public float strength = 0.15f;
        [Range(0f, 1f)] public float glitchIntensity = 0.6f;
        [Range(0f, 100f)] public float progressFrom;
        [Range(0f, 100f)] public float progressTo = 100f;
        public VideoClip videoClip;
        public bool skippable = true;
    }

    [Header("Playback")]
    [SerializeField] private bool playOnStart;
    [SerializeField] private bool playOnce = true;
    [SerializeField] private List<Step> steps = new();

    [Header("References")]
    [SerializeField] private SystemLogPanel systemLogPanel;
    [SerializeField] private CommsPanel commsPanel;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private SimplePlayerController playerController;
    [SerializeField] private SimpleCameraFollow cameraFollow;
    [SerializeField] private ScreenFade screenFade;
    [SerializeField] private ScreenGlitchOverlay glitchOverlay;
    [SerializeField] private PlayerSlowMotion playerSlowMotion;
    [SerializeField] private CutsceneVideoPanel cutsceneVideoPanel;

    private Coroutine _playRoutine;
    private bool _hasPlayed;
    private bool _ownsTimeFreeze;

    public bool IsPlaying => _playRoutine != null;
    public int StepCount => steps.Count;

    private void Reset()
    {
        AutoWire();
    }

    private void Awake()
    {
        AutoWire();
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        AutoWire();
    }

    public void Play()
    {
        if (_playRoutine != null)
        {
            return;
        }

        if (playOnce && _hasPlayed)
        {
            return;
        }

        _playRoutine = StartCoroutine(PlayRoutine());
    }

    public void Stop()
    {
        AutoWire();

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        ReleaseTimeFreezeIfHeld();
        systemLogPanel?.Hide();
        ResetGlitchOverlay();
        GameInput.Instance.EnableGameplay();
        playerInteraction?.SetGameplayControlEnabled(true, clearVelocity: false);
    }

    private void ReleaseTimeFreezeIfHeld()
    {
        if (!_ownsTimeFreeze)
        {
            return;
        }

        _ownsTimeFreeze = false;
        if (playerSlowMotion != null)
        {
            playerSlowMotion.PopExternalFreeze();
        }
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    private IEnumerator PlayRoutine()
    {
        _hasPlayed = true;
        AutoWire();

        for (int i = 0; i < steps.Count; i++)
        {
            Step step = steps[i];
            if (step == null)
            {
                continue;
            }

            switch (step.type)
            {
                case StepType.FadeSystemLog:
                    if (systemLogPanel != null)
                    {
                        yield return systemLogPanel.FadeTo(step.alpha, step.duration);
                    }
                    continue;

                case StepType.LockPlayer:
                    yield return WaitForPlayerActionReady();
                    ExecuteStep(step);
                    continue;

                case StepType.WaitForInput:
                    yield return WaitForInputRoutine(step.inputWaitType);
                    continue;

                case StepType.WaitForEnemiesDead:
                    yield return WaitForAllDead(step.watchTargets);
                    continue;

                case StepType.FadeOut:
                    if (screenFade != null)
                    {
                        yield return screenFade.FadeOut(step.duration);
                    }
                    continue;

                case StepType.FadeIn:
                    if (screenFade != null)
                    {
                        yield return screenFade.FadeIn(step.duration);
                    }
                    continue;

                case StepType.GlitchPulse:
                    if (glitchOverlay != null)
                    {
                        yield return glitchOverlay.Pulse(step.glitchIntensity, step.duration);
                    }
                    else if (step.duration > 0f)
                    {
                        yield return new WaitForSecondsRealtime(step.duration);
                    }
                    continue;

                case StepType.GlitchFade:
                    if (glitchOverlay != null)
                    {
                        yield return glitchOverlay.FadeTo(step.glitchIntensity, step.duration);
                    }
                    else if (step.duration > 0f)
                    {
                        yield return new WaitForSecondsRealtime(step.duration);
                    }
                    continue;

                case StepType.ShowProgressLog:
                    if (systemLogPanel != null)
                    {
                        yield return ShowProgressLogRoutine(step);
                    }
                    continue;

                case StepType.HoldInteractProgress:
                    if (systemLogPanel != null)
                    {
                        yield return HoldInteractProgressRoutine(step);
                    }
                    continue;

                case StepType.CameraFocus:
                    ResetGlitchOverlay();
                    yield return CameraFocusRoutine(step);
                    continue;

                case StepType.PlaySequence:
                    if (step.targetSequence != null)
                    {
                        step.targetSequence.Play();
                        if (step.waitForCompletion)
                        {
                            while (step.targetSequence.IsPlaying)
                            {
                                yield return null;
                            }
                        }
                    }
                    continue;

                case StepType.PlayCutsceneVideo:
                    if (cutsceneVideoPanel != null)
                    {
                        yield return cutsceneVideoPanel.Play(step.videoClip, step.skippable);
                    }
                    continue;
            }

            ExecuteStep(step);

            if (step.duration > 0f)
            {
                yield return new WaitForSecondsRealtime(step.duration);
            }
        }

        ReleaseTimeFreezeIfHeld();
        ResetGlitchOverlay();
        _playRoutine = null;
    }

    private void ExecuteStep(Step step)
    {
        switch (step.type)
        {
            case StepType.Delay:
                break;
            case StepType.LockPlayer:
                ResetGlitchOverlay();
                GameInput.Instance.DisableAllGameplayInput();
                playerInteraction?.SetGameplayControlEnabled(false);
                break;
            case StepType.UnlockPlayer:
                GameInput.Instance.EnableGameplay();
                playerInteraction?.SetGameplayControlEnabled(true, clearVelocity: false);
                break;
            case StepType.ShowSystemLog:
                systemLogPanel?.Show(step.message, step.duration);
                break;
            case StepType.HideSystemLog:
                systemLogPanel?.Hide();
                break;
            case StepType.SetSystemLogAlpha:
                systemLogPanel?.SetAlpha(step.alpha);
                break;
            case StepType.FadeSystemLog:
                break;
            case StepType.ShowCommsLine:
                commsPanel?.ShowLine(step.speaker, step.message, step.duration);
                break;
            case StepType.HideComms:
                commsPanel?.Hide();
                break;
            case StepType.SetObjectActive:
                if (step.targetObject != null)
                {
                    step.targetObject.SetActive(step.active);
                }
                break;
            case StepType.TeleportPlayer:
                if (playerInteraction != null && step.targetTransform != null)
                {
                    Vector3 pos = step.targetTransform.position;
                    playerInteraction.MoveToPosition(pos);
                    playerInteraction.SetSpawnPosition(pos);
                }
                break;
            case StepType.SnapCamera:
                cameraFollow?.SnapToTarget();
                break;
            case StepType.CameraShake:
                cameraFollow?.AddShake(step.strength, step.duration);
                break;
            case StepType.FreezeTime:
                ResetGlitchOverlay();
                if (!_ownsTimeFreeze)
                {
                    _ownsTimeFreeze = true;
                    if (playerSlowMotion != null)
                    {
                        playerSlowMotion.PushExternalFreeze();
                    }
                }
                YongwooAudioManager.Play(YongwooSfxId.TimeFreeze, 0.58f, 0.02f);
                Time.timeScale = 0f;
                Time.fixedDeltaTime = 0f;
                break;
            case StepType.UnfreezeTime:
                ReleaseTimeFreezeIfHeld();
                YongwooAudioManager.Play(YongwooSfxId.TimeUnfreeze, 0.5f, 0.02f);
                break;
            case StepType.SetCameraTarget:
                if (cameraFollow != null)
                {
                    Transform target = step.targetTransform;
                    if (target == null && playerInteraction != null)
                    {
                        target = playerInteraction.transform;
                    }
                    if (target != null)
                    {
                        cameraFollow.SetTarget(target);
                        cameraFollow.SnapToTarget();
                    }
                }
                break;
            case StepType.LockInput:
                ResetGlitchOverlay();
                // controller는 살려둔다 (애니메이션/물리/중력 정상). 입력만 차단.
                GameInput.Instance.DisableAllGameplayInput();
                break;
            case StepType.UnlockInput:
                GameInput.Instance.EnableGameplay();
                break;
        }
    }

    private IEnumerator ShowProgressLogRoutine(Step step)
    {
        float duration = Mathf.Max(0f, step.duration);
        float from = Mathf.Clamp(step.progressFrom, 0f, 100f);
        float to = Mathf.Clamp(step.progressTo, 0f, 100f);
        string label = string.IsNullOrWhiteSpace(step.message) ? "[회수 진행]" : step.message;

        if (duration <= 0f)
        {
            systemLogPanel.Show($"{label}\n{Mathf.RoundToInt(to)}%");
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float progress = Mathf.Lerp(from, to, t);
            systemLogPanel.Show($"{label}\n{Mathf.RoundToInt(progress)}%");
            yield return null;
        }

        systemLogPanel.Show($"{label}\n{Mathf.RoundToInt(to)}%");
    }

    private IEnumerator HoldInteractProgressRoutine(Step step)
    {
        float from = Mathf.Clamp(step.progressFrom, 0f, 100f);
        float to = Mathf.Clamp(step.progressTo, 0f, 100f);
        float progress = from;
        float requiredHoldSeconds = Mathf.Max(0.1f, step.duration);
        float range = Mathf.Max(1f, Mathf.Abs(to - from));
        float rate = range / requiredHoldSeconds;
        string label = string.IsNullOrWhiteSpace(step.message) ? "[회수 진행]" : step.message;

        GameInput.Instance.EnableGameplay();
        playerInteraction?.SetGameplayControlEnabled(false);

        while (progress < to)
        {
            bool held = GameInput.Instance.InteractHeld;
            float dt = Time.unscaledDeltaTime;

            if (held)
            {
                progress = Mathf.Min(to, progress + rate * dt);
                float normalized = Mathf.InverseLerp(from, to, progress);
                glitchOverlay?.SetIntensity(Mathf.Lerp(0.12f, step.glitchIntensity, normalized));
            }
            else
            {
                progress = Mathf.Max(from, progress - rate * dt);
                glitchOverlay?.SetIntensity(0f);
            }

            systemLogPanel.Show($"{label}\n{Mathf.RoundToInt(progress)}%");
            yield return null;
        }

        glitchOverlay?.SetIntensity(0f);
        systemLogPanel.Show($"{label}\n100%");
    }

    private void ResetGlitchOverlay()
    {
        glitchOverlay?.ResetGlitch();
    }

    private IEnumerator CameraFocusRoutine(Step step)
    {
        if (cameraFollow == null || step.targetTransform == null)
        {
            if (step.duration > 0f)
            {
                yield return new WaitForSecondsRealtime(step.duration);
            }
            yield break;
        }

        Transform playerTarget = playerInteraction != null ? playerInteraction.transform : null;
        cameraFollow.SetTarget(step.targetTransform);
        cameraFollow.SnapToTarget();

        if (step.duration > 0f)
        {
            yield return new WaitForSecondsRealtime(step.duration);
        }

        if (playerTarget != null)
        {
            cameraFollow.SetTarget(playerTarget);
            cameraFollow.SnapToTarget();
        }
    }

    private IEnumerator WaitForInputRoutine(InputWaitType waitType)
    {
        GameInput input = GameInput.Instance;
        while (true)
        {
            yield return null;
            bool detected = waitType switch
            {
                InputWaitType.AnyKey => UnityEngine.Input.anyKeyDown,
                InputWaitType.Move => input.Move.sqrMagnitude > 0.01f,
                InputWaitType.Jump => input.JumpPressed,
                InputWaitType.Dash => input.DashPressed,
                InputWaitType.Attack => input.AttackPressed,
                InputWaitType.Interact => input.InteractPressed,
                InputWaitType.Roll => input.Move.y < -0.5f && Mathf.Abs(input.Move.x) > 0.5f,
                _ => false
            };
            if (detected)
            {
                YongwooAudioManager.Play(YongwooSfxId.UiConfirm, 0.45f, 0.02f);
                yield break;
            }
        }
    }

    private IEnumerator WaitForPlayerActionReady()
    {
        float timeout = 1.5f;
        while (playerController != null && playerController.IsActionLocked && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static IEnumerator WaitForAllDead(GameObject[] targets)
    {
        if (targets == null || targets.Length == 0)
        {
            yield break;
        }

        while (true)
        {
            bool allDead = true;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null && targets[i].activeInHierarchy)
                {
                    allDead = false;
                    break;
                }
            }

            if (allDead)
            {
                yield break;
            }

            yield return null;
        }
    }

    private void AutoWire()
    {
        if (systemLogPanel == null)
        {
            systemLogPanel = Object.FindFirstObjectByType<SystemLogPanel>();
        }

        if (commsPanel == null)
        {
            commsPanel = Object.FindFirstObjectByType<CommsPanel>();
        }

        if (playerInteraction == null)
        {
            playerInteraction = Object.FindFirstObjectByType<PlayerInteraction>();
        }

        if (playerController == null && playerInteraction != null)
        {
            playerController = playerInteraction.GetComponent<SimplePlayerController>();
        }

        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<SimplePlayerController>();
        }

        if (cameraFollow == null)
        {
            cameraFollow = Object.FindFirstObjectByType<SimpleCameraFollow>();
        }

        if (screenFade == null)
        {
            screenFade = Object.FindFirstObjectByType<ScreenFade>();
        }

        if (glitchOverlay == null)
        {
            glitchOverlay = Object.FindFirstObjectByType<ScreenGlitchOverlay>();
        }

        if (playerSlowMotion == null)
        {
            if (playerInteraction != null)
            {
                playerSlowMotion = playerInteraction.GetComponent<PlayerSlowMotion>();
            }
            if (playerSlowMotion == null)
            {
                playerSlowMotion = Object.FindFirstObjectByType<PlayerSlowMotion>();
            }
        }

        if (cutsceneVideoPanel == null)
        {
            cutsceneVideoPanel = Object.FindFirstObjectByType<CutsceneVideoPanel>(FindObjectsInactive.Include);
        }
    }

    public void ConfigureSteps(IEnumerable<Step> newSteps)
    {
        steps.Clear();
        if (newSteps != null)
        {
            steps.AddRange(newSteps);
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

#if UNITY_EDITOR
    public int EditorStepCount => steps.Count;

    public void EditorSetSteps(IEnumerable<Step> newSteps)
    {
        ConfigureSteps(newSteps);
    }
#endif
}
