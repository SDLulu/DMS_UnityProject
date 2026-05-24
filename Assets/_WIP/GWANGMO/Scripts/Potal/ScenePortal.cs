using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class ScenePortal : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnPointName;

    [Header("Trigger")]
    [SerializeField] private bool triggerOnEnter = true;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private string requiredTag = "Player";

    [Header("Interaction")]
    [SerializeField] private bool requireInteraction = true;
    [SerializeField] private string promptText = "F : 이동";
    [SerializeField] private SystemLogPanel promptPanel;

    private bool hasTriggered;
    private bool playerInside;
    private Collider2D portalCollider;
    private Collider2D currentPlayerCollider;
    private P_PlayerController currentPlayerController;

    private void Reset()
    {
        portalCollider = GetComponent<Collider2D>();
        portalCollider.isTrigger = true;
    }

    private void Awake()
    {
        portalCollider = GetComponent<Collider2D>();
        portalCollider.isTrigger = true;

        if (promptPanel == null)
        {
            promptPanel = FindFirstObjectByType<SystemLogPanel>();
        }
    }

    private void Update()
    {
        if (!requireInteraction || !playerInside || (triggerOnce && hasTriggered))
        {
            return;
        }

        if (currentPlayerController != null && currentPlayerController.InteractPressedThisFrame)
        {
            HidePrompt();
            TryUsePortal(currentPlayerCollider);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        currentPlayerCollider = other;
        currentPlayerController = other.GetComponentInParent<P_PlayerController>();

        if (requireInteraction)
        {
            playerInside = true;
            ShowPrompt();
            return;
        }

        if (!triggerOnEnter)
        {
            return;
        }

        TryUsePortal(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerInside = false;
        currentPlayerCollider = null;
        currentPlayerController = null;
        HidePrompt();
    }

    public void TryUsePortal(Collider2D playerCollider)
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (!IsPlayer(playerCollider))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("Portal target scene name is empty.", this);
            return;
        }

        GameObject playerObject = playerCollider.GetComponentInParent<PlayerInteraction>()?.gameObject;
        if (playerObject == null)
        {
            if (!string.IsNullOrWhiteSpace(requiredTag) && playerCollider.CompareTag(requiredTag))
            {
                playerObject = playerCollider.gameObject;
            }
            else if (!string.IsNullOrWhiteSpace(requiredTag))
            {
                playerObject = GameObject.FindGameObjectWithTag(requiredTag);
            }
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager instance was not found. Place one GameManager in the first scene.", this);
            return;
        }

        hasTriggered = true;
        GameManager.Instance.LoadSceneFromPortal(targetSceneName, targetSpawnPointName, playerObject);
    }

    public void UsePortal()
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("Portal target scene name is empty.", this);
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager instance was not found. Place one GameManager in the first scene.", this);
            return;
        }

        hasTriggered = true;
        GameManager.Instance.LoadSceneFromPortal(targetSceneName, targetSpawnPointName);
    }

    private void ShowPrompt()
    {
        if (promptPanel != null && !string.IsNullOrWhiteSpace(promptText))
        {
            promptPanel.Show(promptText);
        }
    }

    private void HidePrompt()
    {
        if (promptPanel != null)
        {
            promptPanel.Hide();
        }
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        bool hasRequiredTag = string.IsNullOrWhiteSpace(requiredTag) || other.CompareTag(requiredTag);
        bool hasPlayerInteraction = other.GetComponentInParent<PlayerInteraction>() != null;
        return hasRequiredTag || hasPlayerInteraction;
    }
}
