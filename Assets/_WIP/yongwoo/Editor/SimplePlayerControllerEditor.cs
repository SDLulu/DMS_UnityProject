using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimplePlayerController))]
public class SimplePlayerControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "이 컴포넌트는 플레이어 이동감의 기준입니다.\n" +
            "속도, 가속, 점프, 낙하, 코요테 타임, 점프 버퍼를 여기서 조정합니다.\n" +
            "Sensors 자식은 바닥 판정 기준점으로 사용됩니다.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "먼저 만질 핵심 값:\n" +
            "- jumpForce: 점프 높이\n" +
            "- fallGravityMultiplier: 내려오는 속도\n" +
            "- jumpCutGravityMultiplier: 짧게 누른 점프 높이\n" +
            "- groundCheckRadius: 바닥 판정의 후함",
            MessageType.None);

        EditorGUILayout.Space();
        DrawDefaultInspector();
    }
}
