using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PrototypeHealth))]
public class PrototypeHealthEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "이 컴포넌트는 체력, 피격 무적, 피격 플래시, 사망, 부활을 담당합니다.\n" +
            "플레이어와 보스가 같은 규칙 층을 공유하므로,\n" +
            "전투 감각을 바꿀 때는 이 값을 양쪽 모두에 미치는 공용 규칙으로 이해하는 편이 좋습니다.",
            MessageType.Info);

        EditorGUILayout.Space();
        DrawDefaultInspector();
    }
}
