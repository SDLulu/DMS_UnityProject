using UnityEditor;
using UnityEngine;

// 역할:
// - Blind Huntress 적 편집 프리뷰 사용법을 인스펙터에 짧게 보여줍니다.

[CustomEditor(typeof(BlindHuntressEnemyEditPreview))]
public class BlindHuntressEnemyEditPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "센서/히트박스를 맞출 때 쓰는 편집 전용 프리뷰입니다.\n" +
            "1. enablePreview를 켭니다.\n" +
            "2. previewState를 DashAttack 같은 상태로 고릅니다.\n" +
            "3. normalizedTime으로 원하는 프레임을 고정합니다.\n" +
            "그 뒤 Sensors 자식을 선택해서 움직여도 해당 프레임이 유지됩니다.",
            MessageType.Info);

        EditorGUILayout.Space();
        DrawDefaultInspector();
    }
}
