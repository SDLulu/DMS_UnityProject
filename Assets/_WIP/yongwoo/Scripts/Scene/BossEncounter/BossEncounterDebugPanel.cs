using UnityEngine;
using UnityEngine.UI;

// 역할:
// - 보스 조우를 반복 테스트할 수 있도록 디버그 버튼과 상태 표시를 묶습니다.
// - 디렉터와 HUD가 노출한 액션을 에디터/플레이 중 빠르게 호출하기 위한 보조 UI입니다.
//
// 구조 포인트:
// - 실서비스 UI보다 개발용 반복 테스트를 빠르게 만드는 지원 레이어입니다.

[DisallowMultipleComponent]
public class BossEncounterDebugPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossEncounterDirector encounterDirector;
    [SerializeField] private Button encounterButton;
    [SerializeField] private Text encounterButtonText;
    [SerializeField] private Text stateText;

    private string _lastLabel = string.Empty;
    private BossEncounterDirector.EncounterState _lastState;
    private bool _lastInteractable;

    private void Reset()
    {
        AutoWire();
    }

    private void Awake()
    {
        AutoWire();
        WireButton();
        RefreshUi(force: true);
    }

    private void OnEnable()
    {
        AutoWire();
        WireButton();
        RefreshUi(force: true);
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        AutoWire();
    }

    private void Update()
    {
        RefreshUi(force: false);
    }

    private void AutoWire()
    {
        encounterDirector ??= Object.FindFirstObjectByType<BossEncounterDirector>();
        encounterButton ??= FindInChildren<Button>("EncounterActionButton");
        encounterButtonText ??= FindInChildren<Text>("EncounterActionButton/Label");
        stateText ??= FindInChildren<Text>("StateText");
    }

    private void WireButton()
    {
        if (encounterButton != null)
        {
            encounterButton.onClick.RemoveAllListeners();
            encounterButton.onClick.AddListener(HandleEncounterButtonPressed);
        }
    }

    private void HandleEncounterButtonPressed()
    {
        encounterDirector?.HandleEncounterActionRequested();
        RefreshUi(force: true);
    }

    private void RefreshUi(bool force)
    {
        if (encounterDirector == null)
        {
            return;
        }

        string label = encounterDirector.CurrentEncounterActionLabel;
        bool interactable = encounterDirector.IsEncounterActionInteractable;
        BossEncounterDirector.EncounterState state = encounterDirector.CurrentState;
        if (!force && label == _lastLabel && interactable == _lastInteractable && state == _lastState)
        {
            return;
        }

        _lastLabel = label;
        _lastInteractable = interactable;
        _lastState = state;

        if (encounterButtonText != null)
        {
            encounterButtonText.text = label;
        }

        if (encounterButton != null)
        {
            encounterButton.interactable = interactable;
        }

        if (stateText != null)
        {
            stateText.text = $"상태: {GetStateLabel(state)}";
        }
    }

    private static string GetStateLabel(BossEncounterDirector.EncounterState state)
    {
        return state switch
        {
            BossEncounterDirector.EncounterState.Idle => "대기",
            BossEncounterDirector.EncounterState.IntroTimeline => "인트로 컷씬",
            BossEncounterDirector.EncounterState.IntroDialogue => "인트로 대사",
            BossEncounterDirector.EncounterState.Combat => "전투",
            BossEncounterDirector.EncounterState.FailureReset => "리셋 중",
            BossEncounterDirector.EncounterState.VictoryTimeline => "승리 연출",
            BossEncounterDirector.EncounterState.VictoryDialogue => "승리 대사",
            BossEncounterDirector.EncounterState.Completed => "완료",
            _ => state.ToString()
        };
    }

    private T FindInChildren<T>(string path) where T : Component
    {
        Transform child = transform.Find(path);
        if (child != null)
        {
            return child.GetComponent<T>();
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform nested = transform.GetChild(i).Find(path);
            if (nested != null)
            {
                return nested.GetComponent<T>();
            }
        }

        return null;
    }
}
