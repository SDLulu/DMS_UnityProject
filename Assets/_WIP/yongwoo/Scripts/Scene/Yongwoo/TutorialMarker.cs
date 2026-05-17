using UnityEngine;

// 역할:
// - 씬 뷰에서 스폰 포인트, 트리거, 게이트 등 튜토리얼 오브젝트의 위치를 기즈모로 표시합니다.
// - 라벨 텍스트와 색상으로 역할을 구분합니다.

public class TutorialMarker : MonoBehaviour
{
    public enum MarkerType
    {
        SpawnPoint,
        Trigger,
        Gate,
        Interactable
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
            _ => Color.white
        };
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);

        UnityEditor.Handles.color = gizmoColor;
        var style = new GUIStyle(UnityEditor.EditorStyles.boldLabel);
        style.normal.textColor = gizmoColor;
        style.fontSize = 11;
        style.alignment = TextAnchor.MiddleCenter;

        Vector3 labelPos = transform.position + Vector3.up * (gizmoRadius + 0.3f);
        string label = gameObject.name;
        UnityEditor.Handles.Label(labelPos, label, style);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.25f);
        Gizmos.DrawSphere(transform.position, gizmoRadius);
    }
#endif
}
