using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    // 넘어갈 씬 이름
    public string nextSceneName;

    private bool isPlayerInRange = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            Debug.Log("문에 닿았습니다");
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }

    public void Interact()
    {
        if (isPlayerInRange)
        {
            Debug.Log("다음 Scene 넘어가기");
            SceneManager.LoadScene(nextSceneName);
        }
    }
}