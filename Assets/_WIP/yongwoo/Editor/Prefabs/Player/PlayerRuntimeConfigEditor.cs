using UnityEditor;
using UnityEngine;

// 역할:
// - PlayerRuntimeConfig가 직접 조정 대상이 아니라는 점을 인스펙터에서 안내합니다.
// - 실제 튜닝은 어떤 컴포넌트에서 해야 하는지 연결 포인트를 보여줍니다.
//
// 구조 포인트:
// - 런타임 설정 브리지의 사용 맥락을 설명하는 안내용 에디터입니다.

[CustomEditor(typeof(PlayerRuntimeConfig))]
public class PlayerRuntimeConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "이 컴포넌트는 플레이어 파라미터의 소유자가 아니라 저장/복원용 브리지입니다.\n" +
            "평소 조정은 SimplePlayerController, SimplePlayerCombat, PlayerInteraction, BoxCollider2D에서 직접 진행하고,\n" +
            "이 컴포넌트는 플레이 종료 시 현재 값을 Player 프리팹에 다시 모아 저장합니다.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("권장 수정 위치", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "- 이동감: SimplePlayerController\n" +
            "- 공격 판정: SimplePlayerCombat\n" +
            "- 체력/피격/부활: PlayerInteraction\n" +
            "- 몸통 판정: BoxCollider2D",
            MessageType.None);

        if (!Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("플레이를 시작하면 저장된 스냅샷이 실제 컴포넌트들에 적용됩니다.", MessageType.None);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("플레이 중 동작", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "플레이 중에는 실제 컴포넌트 값을 직접 조정하세요. 플레이 모드를 끌 때 현재 상태가 Player 프리팹에 한 번 저장됩니다.",
            MessageType.None);
    }
}
