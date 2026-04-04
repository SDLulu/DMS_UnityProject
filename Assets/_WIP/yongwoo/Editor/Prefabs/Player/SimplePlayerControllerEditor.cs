using UnityEditor;
using UnityEngine;

// 역할:
// - SimplePlayerController의 이동, 점프, 대시 조정 포인트를 인스펙터에서 묶어 설명합니다.
// - 프리팹 구조와 런타임 튜닝 흐름을 같이 확인하도록 돕습니다.
//
// 구조 포인트:
// - 플레이어 이동 계층의 조정 경험을 정돈하는 에디터 도구입니다.

[CustomEditor(typeof(SimplePlayerController))]
public class SimplePlayerControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "이 컴포넌트는 플레이어 이동감과 행동 상태의 기준입니다.\n" +
            "이동, 점프, 대쉬, 앉기, 구르기를 여기서 조정합니다.\n" +
            "Sensors 자식은 바닥 판정 기준점으로 사용됩니다.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "먼저 만질 핵심 값:\n" +
            "- jumpForce: 점프 높이\n" +
            "- dashSpeed / dashDuration: 대쉬 거리감\n" +
            "- rollSpeed / rollDuration: 구르기 거리감\n" +
            "- fallGravityMultiplier: 내려오는 속도\n" +
            "- jumpCutGravityMultiplier: 짧게 누른 점프 높이\n" +
            "- groundCheckRadius: 바닥 판정의 후함",
            MessageType.None);

        EditorGUILayout.Space();
        DrawDefaultInspector();
    }
}
