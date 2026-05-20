using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 역할:
// - 목업 타이틀 씬의 버튼 입력을 처리합니다.

[DisallowMultipleComponent]
public class TitleSceneController : MonoBehaviour
{
    [SerializeField] private string stageSceneName = "Yongwoo_Stage";
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    private void Reset()
    {
        TryAutoWire();
    }

    private void Awake()
    {
        TryAutoWire();
    }

    private void OnEnable()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(LoadStageScene);
            startButton.onClick.AddListener(LoadStageScene);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    public void LoadStageScene()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        SceneManager.LoadScene(stageSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void TryAutoWire()
    {
        startButton ??= GameObject.Find("StartButton")?.GetComponent<Button>();
        quitButton ??= GameObject.Find("QuitButton")?.GetComponent<Button>();
    }
}
