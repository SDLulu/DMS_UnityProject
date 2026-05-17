using UnityEngine;

// 역할:
// - 감시 대상(적)이 전부 죽으면 진행 차단 오브젝트를 비활성화합니다.
// - 기존 씬 직렬화 호환 때문에 클래스명은 유지하지만, 시나리오상 역할은 ProgressBlocker입니다.

[DisallowMultipleComponent]
public class TutorialGate : MonoBehaviour
{
    [Header("Condition")]
    [SerializeField] private GameObject[] enemies;

    [Header("Progress Blocker")]
    [SerializeField] private GameObject gateVisual;
    [SerializeField] private bool startClosed = true;

    [Header("On Open")]
    [SerializeField] private SceneEventSequence onOpenSequence;

    private bool _opened;

    private void Start()
    {
        if (enemies == null || enemies.Length == 0)
        {
            Debug.LogWarning($"[{nameof(TutorialGate)}] '{name}' has no enemies assigned, so it opens immediately.", this);
            Open();
            return;
        }

        if (startClosed && gateVisual != null)
        {
            gateVisual.SetActive(true);
        }
    }

    private void Update()
    {
        if (_opened)
        {
            return;
        }

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].activeInHierarchy)
            {
                return;
            }
        }

        Open();
    }

    private void Open()
    {
        _opened = true;

        if (gateVisual != null)
        {
            gateVisual.SetActive(false);
        }

        if (onOpenSequence != null)
        {
            onOpenSequence.Play();
        }
    }
}
