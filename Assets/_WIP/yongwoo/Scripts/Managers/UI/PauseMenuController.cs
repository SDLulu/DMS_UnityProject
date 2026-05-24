using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 역할:
// - ESC / HUD 버튼으로 게임을 멈추고, 씬에 배치된 일시정지 패널을 표시합니다.
// - 패널 버튼에서 게임 재개와 타이틀 씬 이동을 처리합니다.

[DisallowMultipleComponent]
public class PauseMenuController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameObject pausePanelRoot;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Image pauseButtonImage;
    [SerializeField] private Sprite resumeIconSprite;
    [SerializeField] private Sprite pauseIconSprite;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button titleButton;

    [Header("Scene Loading")]
    [SerializeField] private string titleSceneName = "Yongwoo_Title";

    private bool _isPaused;
    private bool _gameplayWasEnabled;
    private float _timeScaleBeforePause = 1f;
    private float _fixedDeltaTimeBeforePause = 0.02f;

    public static bool IsPaused { get; private set; }

    private void Reset()
    {
        TryAutoWire();
    }

    private void Awake()
    {
        TryAutoWire();
        BindButtons();
        SetPanelVisible(false);
        UpdatePauseButtonVisual();
    }

    private void OnEnable()
    {
        TryAutoWire();
        BindButtons();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        TryAutoWire();
    }

    private void OnDisable()
    {
        if (_isPaused)
        {
            ResumeGame();
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (_isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (_isPaused)
        {
            return;
        }

        _isPaused = true;
        IsPaused = true;
        _gameplayWasEnabled = GameInput.Instance.GameplayEnabled;
        _timeScaleBeforePause = Time.timeScale;
        _fixedDeltaTimeBeforePause = Time.fixedDeltaTime;

        GameInput.Instance.DisableAllGameplayInput();
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
        SetPanelVisible(true);
        UpdatePauseButtonVisual();
        YongwooAudioManager.Play(YongwooSfxId.PauseOpen, 0.58f, 0.02f);

        if (EventSystem.current != null && resumeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }
    }

    public void ResumeGame()
    {
        if (!_isPaused)
        {
            return;
        }

        _isPaused = false;
        IsPaused = false;
        SetPanelVisible(false);
        UpdatePauseButtonVisual();
        YongwooAudioManager.Play(YongwooSfxId.PauseClose, 0.52f, 0.02f);

        Time.timeScale = _timeScaleBeforePause;
        Time.fixedDeltaTime = _fixedDeltaTimeBeforePause;

        if (_gameplayWasEnabled)
        {
            GameInput.Instance.EnableGameplay();
        }
        else
        {
            GameInput.Instance.DisableAllGameplayInput();
        }
    }

    public void LoadTitleScene()
    {
        _isPaused = false;
        IsPaused = false;
        UpdatePauseButtonVisual();
        YongwooAudioManager.Play(YongwooSfxId.UiClick, 0.5f, 0.02f);
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        GameInput.Instance.EnableGameplay();
        SceneManager.LoadScene(titleSceneName);
    }

    private void TryAutoWire()
    {
        pausePanelRoot ??= GameObject.Find("PauseMenuRoot");
        pauseButton ??= FindButtonByName("PauseButton");
        pauseButton ??= FindButtonByName("설정버튼");
        if (pauseButtonImage == null && pauseButton != null)
        {
            pauseButtonImage = pauseButton.targetGraphic as Image;
            pauseButtonImage ??= pauseButton.GetComponent<Image>();
        }
        resumeButton ??= GameObject.Find("ResumeButton")?.GetComponent<Button>();
        titleButton ??= GameObject.Find("TitleButton")?.GetComponent<Button>();
    }

    private void BindButtons()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(TogglePause);
            pauseButton.onClick.AddListener(TogglePause);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (titleButton != null)
        {
            titleButton.onClick.RemoveListener(LoadTitleScene);
            titleButton.onClick.AddListener(LoadTitleScene);
        }
    }

    private void SetPanelVisible(bool visible)
    {
        if (pausePanelRoot != null)
        {
            if (visible)
            {
                pausePanelRoot.transform.SetAsFirstSibling();
            }

            pausePanelRoot.SetActive(visible);
        }
    }

    private void UpdatePauseButtonVisual()
    {
        if (pauseButtonImage == null)
        {
            return;
        }

        Sprite targetSprite = _isPaused ? pauseIconSprite : resumeIconSprite;
        if (targetSprite != null)
        {
            pauseButtonImage.sprite = targetSprite;
        }
    }

    private static Button FindButtonByName(string objectName)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
            {
                return transforms[i].GetComponent<Button>();
            }
        }

        return null;
    }
}
