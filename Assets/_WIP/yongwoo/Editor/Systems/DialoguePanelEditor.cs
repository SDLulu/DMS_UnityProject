using UnityEditor;
using UnityEngine;

// 역할:
// - DialoguePanel이 기대하는 이름 규칙과 UI 참조를 인스펙터에서 설명합니다.

[CustomEditor(typeof(DialoguePanel))]
public class DialoguePanelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "이 컴포넌트는 대화 UI를 담당합니다.\n" +
            "씬에 직접 배치한 DialogueRoot 계층을 켜고 끄는 것이 기준이며, 런타임에 기본 UI를 새로 만들지 않습니다.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "입력 규칙:\n" +
            "- Space / Enter / 좌클릭: 다음 대사 또는 현재 줄 즉시 표시\n" +
            "- Tab / Escape: 현재 대화 묶음 전체 스킵",
            MessageType.None);

        EditorGUILayout.Space();
        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();
    }
}
