using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PrototypeBossController))]
public class PrototypeBossControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("보스 패턴과 전투값은 이 컴포넌트에 모아둡니다. 플레이 중 바꾼 값은 즉시 반영되고, 플레이 모드를 끌 때 Boss 프리팹에 저장됩니다.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("플레이 종료 시 저장", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("플레이 중 이 컴포넌트 값을 바꾸면 즉시 적용됩니다. 저장은 플레이 모드를 끌 때 Boss 프리팹에 한 번만 반영됩니다.", MessageType.None);
    }
}
