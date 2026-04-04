using UnityEditor;
using UnityEngine;

// 역할:
// - BattleHud가 기대하는 씬 UI 계층과 필수 참조를 인스펙터에서 설명합니다.
// - HUD 루트가 scene-authored 구조를 따르는지 빠르게 점검할 수 있게 합니다.
//
// 구조 포인트:
// - 씬 배치형 HUD를 안정적으로 유지하기 위한 에디터 보조 파일입니다.

[CustomEditor(typeof(BattleHud))]
public class BattleHudEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "이 컴포넌트는 씬에 배치한 플레이어/보스 체력바와 입력 설정 패널을 켜고 끄고 값만 갱신합니다.\n" +
            "즉, HUD 모양은 씬에서 직접 배치하고 이 컴포넌트는 플레이어 HP는 항상 표시, 보스 HP와 설정 패널은 필요할 때만 토글하는 쪽이 기준입니다.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "입력 설정 버튼과 조우 시작 버튼은 HUD가 아니라 BossEncounterDebugPanel이 담당합니다.\n" +
            "이 HUD는 플레이어 HP, 보스 HP, InputSettingsPanel 루트만 관리한다고 보면 됩니다.",
            MessageType.None);

        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("플레이 중에는 플레이어 HP는 항상 유지되고, 보스 HP는 전투 중일 때만 표시됩니다.", MessageType.Info);
        }
    }
}
