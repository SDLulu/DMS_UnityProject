using System;
using UnityEngine;
using UnityEngine.InputSystem;

// 역할:
// - Input System 액션 맵을 감싸는 단일 진입점으로 게임플레이/대화/UI 입력을 제공합니다.
// - 입력 맵 전환, 리바인드, 감도 값을 한곳에서 관리합니다.
//
// 구조 포인트:
// - 다른 런타임 스크립트는 디바이스를 직접 읽지 않고 이 파일만 통해 입력을 받습니다.

public sealed class GameInput : IDisposable
{
    private const string InputAssetResourcePath = "Input/InputSystem_Actions";
    private const string PlayerMapName = "Player";
    private const string DialogueMapName = "Dialogue";
    private const string UiMapName = "UI";

    private static GameInput _instance;

    private readonly InputActionAsset _actions;
    private readonly InputActionMap _playerMap;
    private readonly InputActionMap _dialogueMap;
    private readonly InputActionMap _uiMap;

    private readonly InputAction _moveAction;
    private readonly InputAction _lookAction;
    private readonly InputAction _jumpAction;
    private readonly InputAction _crouchAction;
    private readonly InputAction _dashAction;
    private readonly InputAction _attackAction;
    private readonly InputAction _interactAction;
    private readonly InputAction _previousWeaponAction;
    private readonly InputAction _nextWeaponAction;

    private readonly InputAction _dialogueAdvanceAction;
    private readonly InputAction _dialogueSkipAction;

    private readonly InputAction _uiPointAction;
    private readonly InputAction _uiClickAction;

    public static GameInput Instance => _instance ??= CreateInstance();

    public bool GameplayEnabled => _playerMap.enabled;
    public bool DialogueEnabled => _dialogueMap.enabled;
    public float LookSensitivity
    {
        get => GameInputSettingsStore.LookSensitivity;
        set
        {
            GameInputSettingsStore.SetLookSensitivity(value);
            SaveSettings();
        }
    }

    public Vector2 Move => GameplayEnabled ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 LookVector => GameplayEnabled ? _lookAction.ReadValue<Vector2>() * LookSensitivity : Vector2.zero;
    public bool MoveTriggeredThisFrame => GameplayEnabled && _moveAction.triggered;
    public bool JumpPressed => GameplayEnabled && _jumpAction.WasPressedThisFrame();
    public bool JumpHeld => GameplayEnabled && _jumpAction.IsPressed();
    public bool CrouchPressed => GameplayEnabled && _crouchAction.WasPressedThisFrame();
    public bool CrouchHeld => GameplayEnabled && _crouchAction.IsPressed();
    public bool DashPressed => GameplayEnabled && _dashAction.WasPressedThisFrame();
    public bool AttackPressed => GameplayEnabled && _attackAction.WasPressedThisFrame();
    public bool InteractPressed => GameplayEnabled && _interactAction.WasPressedThisFrame();
    public bool PreviousWeaponPressed => GameplayEnabled && _previousWeaponAction.WasPressedThisFrame();
    public bool NextWeaponPressed => GameplayEnabled && _nextWeaponAction.WasPressedThisFrame();
    public bool DialogueAdvancePressed => DialogueEnabled && _dialogueAdvanceAction.WasPressedThisFrame();
    public bool DialogueSkipPressed => DialogueEnabled && _dialogueSkipAction.WasPressedThisFrame();
    public bool UiClickPressed => _uiMap.enabled && _uiClickAction.WasPressedThisFrame();

    public Vector2 PointerScreenPosition
    {
        get
        {
            if (!_uiMap.enabled)
            {
                return Vector2.zero;
            }

            return _uiPointAction.ReadValue<Vector2>();
        }
    }

    private GameInput(InputActionAsset actions)
    {
        _actions = actions;
        _playerMap = _actions.FindActionMap(PlayerMapName, throwIfNotFound: true);
        _dialogueMap = _actions.FindActionMap(DialogueMapName, throwIfNotFound: true);
        _uiMap = _actions.FindActionMap(UiMapName, throwIfNotFound: true);

        _moveAction = _playerMap.FindAction("Move", throwIfNotFound: true);
        _lookAction = _playerMap.FindAction("Look", throwIfNotFound: true);
        _jumpAction = _playerMap.FindAction("Jump", throwIfNotFound: true);
        _crouchAction = _playerMap.FindAction("Crouch", throwIfNotFound: true);
        _dashAction = _playerMap.FindAction("Sprint", throwIfNotFound: true);
        _attackAction = _playerMap.FindAction("Attack", throwIfNotFound: true);
        _interactAction = _playerMap.FindAction("Interact", throwIfNotFound: true);
        _previousWeaponAction = _playerMap.FindAction("Previous", throwIfNotFound: true);
        _nextWeaponAction = _playerMap.FindAction("Next", throwIfNotFound: true);

        _dialogueAdvanceAction = _dialogueMap.FindAction("Advance", throwIfNotFound: true);
        _dialogueSkipAction = _dialogueMap.FindAction("Skip", throwIfNotFound: true);

        _uiPointAction = _uiMap.FindAction("Point", throwIfNotFound: true);
        _uiClickAction = _uiMap.FindAction("Click", throwIfNotFound: true);

        _uiMap.Enable();
        _dialogueMap.Disable();
        _playerMap.Enable();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        _instance?.Dispose();
        _instance = null;
    }

    private static GameInput CreateInstance()
    {
        InputActionAsset inputAsset = Resources.Load<InputActionAsset>(InputAssetResourcePath);
        if (inputAsset == null)
        {
            throw new InvalidOperationException($"GameInput could not load InputActionAsset at Resources/{InputAssetResourcePath}.");
        }

        GameInput instance = new GameInput(UnityEngine.Object.Instantiate(inputAsset));
        GameInputSettingsStore.Load(instance);
        return instance;
    }

    public void EnableGameplay()
    {
        _playerMap.Enable();
        _dialogueMap.Disable();
        _uiMap.Enable();
    }

    public void EnableDialogue()
    {
        _playerMap.Disable();
        _dialogueMap.Enable();
        _uiMap.Enable();
    }

    public void DisableAllGameplayInput()
    {
        _playerMap.Disable();
        _dialogueMap.Disable();
        _uiMap.Enable();
    }

    public bool TryGetPointerScreenPosition(out Vector2 screenPosition)
    {
        screenPosition = PointerScreenPosition;
        return screenPosition.sqrMagnitude > 0.0001f;
    }

    public InputAction FindAction(string mapName, string actionName)
    {
        InputActionMap map = _actions.FindActionMap(mapName, throwIfNotFound: false);
        return map?.FindAction(actionName, throwIfNotFound: false);
    }

    public int FindBindingIndex(
        string mapName,
        string actionName,
        string bindingName = null,
        string groupContains = null)
    {
        InputAction action = FindAction(mapName, actionName);
        if (action == null)
        {
            return -1;
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (!string.IsNullOrWhiteSpace(bindingName)
                && !string.Equals(binding.name, bindingName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(groupContains)
                && (string.IsNullOrWhiteSpace(binding.groups)
                    || binding.groups.IndexOf(groupContains, StringComparison.OrdinalIgnoreCase) < 0))
            {
                continue;
            }

            return i;
        }

        return -1;
    }

    public InputActionRebindingExtensions.RebindingOperation StartInteractiveRebind(
        string mapName,
        string actionName,
        int bindingIndex,
        Action onComplete,
        Action onCancel)
    {
        InputAction action = FindAction(mapName, actionName);
        if (action == null)
        {
            throw new InvalidOperationException($"GameInput could not find action {mapName}/{actionName}.");
        }

        action.Disable();
        return action.PerformInteractiveRebinding(bindingIndex)
            .OnCancel(operation =>
            {
                action.Enable();
                operation.Dispose();
                onCancel?.Invoke();
            })
            .OnComplete(operation =>
            {
                action.Enable();
                SaveSettings();
                operation.Dispose();
                onComplete?.Invoke();
            })
            .Start();
    }

    public string GetBindingDisplayString(string mapName, string actionName, int bindingIndex)
    {
        InputAction action = FindAction(mapName, actionName);
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            return string.Empty;
        }

        return action.GetBindingDisplayString(bindingIndex);
    }

    public void ResetBinding(string mapName, string actionName, int bindingIndex)
    {
        InputAction action = FindAction(mapName, actionName);
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            return;
        }

        action.RemoveBindingOverride(bindingIndex);
        SaveSettings();
    }

    public void RemoveAllBindingOverrides()
    {
        _actions.RemoveAllBindingOverrides();
    }

    public string SaveBindingOverridesAsJson()
    {
        return _actions.SaveBindingOverridesAsJson();
    }

    public void LoadBindingOverridesFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        _actions.LoadBindingOverridesFromJson(json);
    }

    public void SaveSettings()
    {
        GameInputSettingsStore.Save(this);
    }

    public void Dispose()
    {
        _playerMap.Disable();
        _dialogueMap.Disable();
        _uiMap.Disable();
        UnityEngine.Object.Destroy(_actions);
    }
}
