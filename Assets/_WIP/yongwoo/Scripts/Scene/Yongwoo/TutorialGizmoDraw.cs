#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// 역할:
// - TutorialMarker / BossBattleArena 등 씬 배치용 기즈모를 같은 스타일로 그립니다.

public static class TutorialGizmoDraw
{
    public static void DrawPoint(Vector3 position, float radius, Color color, string label)
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(position, radius);

        if (string.IsNullOrEmpty(label))
        {
            return;
        }

        Handles.color = color;
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = color;

        Vector3 labelPos = position + Vector3.up * (radius + 0.3f);
        Handles.Label(labelPos, label, style);
    }

    public static void DrawFilledPoint(Vector3 position, float radius, Color color)
    {
        Color fill = new Color(color.r, color.g, color.b, 0.25f);
        Gizmos.color = fill;
        Gizmos.DrawSphere(position, radius);
    }

    public static void DrawWireBox(Vector3 center, Vector3 size, Color color, string label)
    {
        Gizmos.color = color;
        Gizmos.DrawWireCube(center, size);

        if (string.IsNullOrEmpty(label))
        {
            return;
        }

        Handles.color = color;
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = color;

        Vector3 labelPos = center + new Vector3(0f, size.y * 0.5f + 0.35f, 0f);
        Handles.Label(labelPos, label, style);
    }
}
#endif
