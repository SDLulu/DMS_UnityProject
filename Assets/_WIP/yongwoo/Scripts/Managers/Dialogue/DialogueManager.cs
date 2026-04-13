using System;
using System.Collections.Generic;
using UnityEngine;

// 역할:
// - 대사 재생 생명주기와 입력 모드 전환을 수행하는 범용 대화 매니저입니다.
// - NPC 대화, 스토리 이벤트 등 여러 객체가 대화를 재생할 때 이 매니저를 호출합니다.

[DisallowMultipleComponent]
public class DialogueManager : MonoBehaviour
{
    private const string DefaultDialogueObjectName = "DialogueUI";

    [Header("References")]
    [SerializeField] private DialoguePanel dialogueView;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private SimpleCameraFollow cameraFollow;

    private DialoguePlaybackContext _activeContext;
    private bool _isPlaying;
    private bool _lockedPlayerControl;
    private bool _disabledCameraFollow;
    private bool _manageInputMode;
    private bool _allowSkip;
    private bool _previousCameraFollowState;

    public bool IsPlaying => _isPlaying;
    public DialoguePanel DialogueView => dialogueView;

    private void Awake()
    {
        AutoWire();
        EnsureDialogueView();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        AutoWire();
    }

    public void BindView(DialoguePanel view)
    {
        dialogueView = view;
    }

    public void BindReferences(
        PlayerInteraction interaction,
        SimpleCameraFollow follow)
    {
        playerInteraction = interaction;
        cameraFollow = follow;
    }

    public bool TryPlay(DialogueSequence sequence, DialoguePlaybackContext context = null)
    {
        if (sequence == null)
        {
            return false;
        }

        bool lockPlayer = context?.lockPlayerControlOverride ?? sequence.LockPlayerControl;
        bool disableFollow = context?.disableCameraFollowOverride ?? sequence.DisableCameraFollow;
        bool allowSkip = context?.allowSkipOverride ?? sequence.AllowSkip;
        return TryPlayLines(sequence.Lines, context, lockPlayer, disableFollow, allowSkip);
    }

    public bool TryPlay(IReadOnlyList<DialogueLineData> lines, DialoguePlaybackContext context = null)
    {
        bool lockPlayer = context?.lockPlayerControlOverride ?? false;
        bool disableFollow = context?.disableCameraFollowOverride ?? false;
        bool allowSkip = context?.allowSkipOverride ?? true;
        return TryPlayLines(lines, context, lockPlayer, disableFollow, allowSkip);
    }

    public void SkipCurrent()
    {
        if (!_isPlaying || !_allowSkip)
        {
            return;
        }

        dialogueView?.SkipAll();
    }

    private bool TryPlayLines(
        IReadOnlyList<DialogueLineData> lines,
        DialoguePlaybackContext context,
        bool lockPlayerControl,
        bool disableCameraFollow,
        bool allowSkip)
    {
        if (_isPlaying)
        {
            return false;
        }

        AutoWire();
        EnsureDialogueView();
        if (dialogueView == null || lines == null || lines.Count == 0)
        {
            return false;
        }

        _activeContext = context;
        _isPlaying = true;
        _manageInputMode = context?.manageInputMode ?? true;
        _allowSkip = allowSkip;

        if (_manageInputMode)
        {
            GameInput.Instance.EnableDialogue();
        }

        if (lockPlayerControl)
        {
            LockPlayerControl();
        }

        if (disableCameraFollow)
        {
            DisableCameraFollow();
        }

        _activeContext?.onStarted?.Invoke();
        dialogueView.Play((IList<DialogueLineData>)lines, HandleDialogueCompleted);
        return true;
    }

    private void HandleDialogueCompleted()
    {
        RestorePresentationState();

        Action onCompleted = _activeContext?.onCompleted;
        _activeContext = null;
        _isPlaying = false;

        if (_manageInputMode)
        {
            GameInput.Instance.EnableGameplay();
        }

        _manageInputMode = false;
        _allowSkip = true;
        onCompleted?.Invoke();
    }

    private void RestorePresentationState()
    {
        if (_lockedPlayerControl)
        {
            UnlockPlayerControl();
        }

        if (_disabledCameraFollow && cameraFollow != null)
        {
            cameraFollow.enabled = _previousCameraFollowState;
        }

        _lockedPlayerControl = false;
        _disabledCameraFollow = false;
    }

    private void LockPlayerControl()
    {
        if (playerInteraction == null)
        {
            return;
        }

        playerInteraction.SetGameplayControlEnabled(false);
        _lockedPlayerControl = true;
    }

    private void UnlockPlayerControl()
    {
        playerInteraction?.SetGameplayControlEnabled(true, clearVelocity: false);
    }

    private void DisableCameraFollow()
    {
        if (cameraFollow == null)
        {
            return;
        }

        _previousCameraFollowState = cameraFollow.enabled;
        cameraFollow.enabled = false;
        _disabledCameraFollow = true;
    }

    private void AutoWire()
    {
        dialogueView ??= UnityEngine.Object.FindFirstObjectByType<DialoguePanel>();
        playerInteraction ??= UnityEngine.Object.FindFirstObjectByType<PlayerInteraction>();
        cameraFollow ??= UnityEngine.Object.FindFirstObjectByType<SimpleCameraFollow>();
    }

    private void EnsureDialogueView()
    {
        if (dialogueView != null)
        {
            return;
        }

        GameObject dialogueObject = GameObject.Find(DefaultDialogueObjectName);
        if (dialogueObject != null)
        {
            dialogueView = dialogueObject.GetComponent<DialoguePanel>();
        }

        if (dialogueView == null)
        {
            Debug.LogWarning("DialogueManager could not find DialoguePanel in the scene. Place the dialogue UI in the scene and bind it instead of relying on runtime creation.", this);
        }
    }
}
