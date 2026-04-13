using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 역할:
// - 감도 슬라이더와 리바인드 버튼 UI를 실제 GameInput 설정과 연결합니다.
// - 사용자 입력 설정을 scene-authored 패널 위에 올리는 표시/반영 계층입니다.
//
// 구조 포인트:
// - 저장은 Store가, 실제 입력 데이터는 GameInput이 담당하고 이 파일은 둘을 묶습니다.

[DisallowMultipleComponent]
public class GameInputSettingsPanel : MonoBehaviour
{
    [Serializable]
    public sealed class BindingRow
    {
        public string label = string.Empty;
        public string mapName = "Player";
        public string actionName = string.Empty;
        public int bindingIndex = 0;
        public Text labelText = null;
        public Text bindingValueText = null;
        public Button rebindButton = null;
        public Button resetButton = null;
    }

    [Header("Bindings")]
    [SerializeField] private List<BindingRow> bindingRows = new();

    [Header("Actions")]
    [SerializeField] private Button resetAllButton;
    [SerializeField] private Text statusText;

    private InputActionRebindingExtensions.RebindingOperation _rebindOperation;

    private void Awake()
    {
        WireUi();
        RefreshUi();
    }

    private void OnEnable()
    {
        WireUi();
        RefreshUi();
    }

    private void OnDisable()
    {
        DisposeRebindOperation();
    }

    public void SetBindingRows(IEnumerable<BindingRow> rows)
    {
        bindingRows.Clear();
        if (rows == null)
        {
            RefreshUi();
            return;
        }

        foreach (BindingRow row in rows)
        {
            if (row != null)
            {
                bindingRows.Add(row);
            }
        }

        WireUi();
        RefreshUi();
    }

    public void SetActionWidgets(Button resetButton, Text status)
    {
        resetAllButton = resetButton;
        statusText = status;
        WireUi();
        RefreshUi();
    }

    public void RefreshPanel()
    {
        RefreshUi();
    }

    private void WireUi()
    {
        for (int i = 0; i < bindingRows.Count; i++)
        {
            BindingRow row = bindingRows[i];
            if (row == null)
            {
                continue;
            }

            int index = i;
            if (row.rebindButton != null)
            {
                row.rebindButton.onClick.RemoveAllListeners();
                row.rebindButton.onClick.AddListener(() => StartRebind(index));
            }

            if (row.resetButton != null)
            {
                row.resetButton.onClick.RemoveAllListeners();
                row.resetButton.onClick.AddListener(() => ResetBinding(index));
            }
        }

        if (resetAllButton != null)
        {
            resetAllButton.onClick.RemoveAllListeners();
            resetAllButton.onClick.AddListener(ResetAllBindings);
        }
    }

    private void RefreshUi()
    {
        GameInput input = GameInput.Instance;
        for (int i = 0; i < bindingRows.Count; i++)
        {
            BindingRow row = bindingRows[i];
            if (row == null)
            {
                continue;
            }

            if (row.labelText != null)
            {
                row.labelText.text = string.IsNullOrWhiteSpace(row.label)
                    ? $"{row.mapName}/{row.actionName}"
                    : row.label;
            }

            if (row.bindingValueText != null)
            {
                row.bindingValueText.text = input.GetBindingDisplayString(row.mapName, row.actionName, row.bindingIndex);
            }
        }

    }

    private void StartRebind(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= bindingRows.Count)
        {
            return;
        }

        BindingRow row = bindingRows[rowIndex];
        if (row == null || string.IsNullOrWhiteSpace(row.actionName))
        {
            return;
        }

        DisposeRebindOperation();
        SetStatus($"{row.mapName}/{row.actionName} 입력 대기 중...");

        if (row.bindingValueText != null)
        {
            row.bindingValueText.text = "입력 대기...";
        }

        _rebindOperation = GameInput.Instance.StartInteractiveRebind(
            row.mapName,
            row.actionName,
            row.bindingIndex,
            onComplete: () =>
            {
                DisposeRebindOperation();
                SetStatus("리바인드 완료");
                RefreshUi();
            },
            onCancel: () =>
            {
                DisposeRebindOperation();
                SetStatus("리바인드 취소");
                RefreshUi();
            });
    }

    private void ResetBinding(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= bindingRows.Count)
        {
            return;
        }

        BindingRow row = bindingRows[rowIndex];
        if (row == null)
        {
            return;
        }

        GameInput.Instance.ResetBinding(row.mapName, row.actionName, row.bindingIndex);
        SetStatus($"{row.mapName}/{row.actionName} 기본값 복구");
        RefreshUi();
    }

    private void ResetAllBindings()
    {
        DisposeRebindOperation();
        GameInputSettingsStore.ResetToDefaults(GameInput.Instance);
        SetStatus("입력 설정을 기본값으로 복구했습니다.");
        RefreshUi();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message ?? string.Empty;
        }
    }

    private void DisposeRebindOperation()
    {
        _rebindOperation?.Dispose();
        _rebindOperation = null;
    }
}
