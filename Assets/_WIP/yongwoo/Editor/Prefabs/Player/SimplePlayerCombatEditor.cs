using UnityEditor;
using UnityEngine;

// 역할:
// - SimplePlayerCombat 조정 시 함께 봐야 할 무기/시각화 포인트를 인스펙터에서 안내합니다.
// - 플레이 중 튜닝과 프리팹 반영 흐름을 에디터 관점에서 보조합니다.
//
// 구조 포인트:
// - 전투 수치를 손댈 때 실수하기 쉬운 지점을 줄이는 안내 계층입니다.

[CustomEditor(typeof(SimplePlayerCombat))]
public class SimplePlayerCombatEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "이 컴포넌트는 플레이어 마우스 조준 전투의 기준입니다.\n" +
            "마우스 휠로 칼/총을 전환하고, 좌클릭으로 현재 무기를 사용합니다.\n" +
            "칼은 마우스 방향의 부채꼴 근접 판정, 총은 단발 투사체로 동작합니다.",
            MessageType.Info);

        EditorGUILayout.Space();
        DrawDefaultInspector();
    }
}
