using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        SetObjectActive
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
    }

    [Header("Playback")]
    [SerializeField] private bool playOnStart;
    [SerializeField] private bool playOnce = true;
    [SerializeField] private List<Step> steps = new();

    [Header("References")]
    [SerializeField] private SystemLogPanel systemLogPanel;
    [SerializeField] private CommsPanel commsPanel;
    [SerializeField] private PlayerInteraction playerInteraction;

    private Coroutine _playRoutine;
    private bool _hasPlayed;

    public bool IsPlaying => _playRoutine != null;

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
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        systemLogPanel?.Hide();
        GameInput.Instance.EnableGameplay();
        playerInteraction?.SetGameplayControlEnabled(true, clearVelocity: false);
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

            if (step.type == StepType.FadeSystemLog)
            {
                if (systemLogPanel != null)
                {
                    yield return systemLogPanel.FadeTo(step.alpha, step.duration);
                }

                continue;
            }

            ExecuteStep(step);

            if (step.duration > 0f)
            {
                yield return new WaitForSecondsRealtime(step.duration);
            }
        }

        _playRoutine = null;
    }

    private void ExecuteStep(Step step)
    {
        switch (step.type)
        {
            case StepType.Delay:
                break;
            case StepType.LockPlayer:
                GameInput.Instance.DisableAllGameplayInput();
                playerInteraction?.SetGameplayControlEnabled(false);
                break;
            case StepType.UnlockPlayer:
                GameInput.Instance.EnableGameplay();
                playerInteraction?.SetGameplayControlEnabled(true, clearVelocity: false);
                break;
            case StepType.ShowSystemLog:
                systemLogPanel?.Show(step.message);
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
                commsPanel?.ShowLine(step.speaker, step.message);
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
    }
}
