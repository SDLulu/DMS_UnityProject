using UnityEngine;

// 역할:
// - 씬 뷰에서 스폰 포인트, 트리거, 게이트, 보스 아레나 앵커 등 배치 오브젝트를 기즈모로 표시합니다.
// - 라벨 텍스트와 색상으로 역할을 구분합니다.

public class TutorialMarker : MonoBehaviour
{
    public enum MarkerType
    {
        SpawnPoint,
        Trigger,
        Gate,
        Interactable,
        BossCameraAnchor,
        BossTeleportAnchor,
    }

    [SerializeField] private MarkerType markerType = MarkerType.SpawnPoint;
    [SerializeField] private Color gizmoColor = Color.cyan;
    [SerializeField] private float gizmoRadius = 0.4f;

    private void Reset()
    {
        AutoColor();
    }

    private void OnValidate()
    {
        AutoColor();
    }

    private void AutoColor()
    {
        gizmoColor = markerType switch
        {
            MarkerType.SpawnPoint => Color.cyan,
            MarkerType.Trigger => Color.yellow,
            MarkerType.Gate => Color.red,
            MarkerType.Interactable => Color.green,
            MarkerType.BossCameraAnchor => new Color(0.2f, 0.85f, 1f),
            MarkerType.BossTeleportAnchor => new Color(0.2f, 1f, 0.35f),
            _ => Color.white
        };
    }

    public void Configure(MarkerType type, float radius = 0.4f)
    {
        markerType = type;
        gizmoRadius = radius;
        AutoColor();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        TutorialGizmoDraw.DrawPoint(transform.position, gizmoRadius, gizmoColor, gameObject.name);
    }

    private void OnDrawGizmosSelected()
    {
        TutorialGizmoDraw.DrawFilledPoint(transform.position, gizmoRadius, gizmoColor);
    }
#endif
}
