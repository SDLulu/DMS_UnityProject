using UnityEditor;
using UnityEngine;

// 역할:
// - BossEncounterDirector의 필수 씬 참조와 조우 흐름 조정 포인트를 인스펙터에서 설명합니다.
// - 컷신, 대화, 전투 연결에 필요한 배선을 한눈에 확인하게 돕습니다.
//
// 구조 포인트:
// - 씬 허브의 복잡한 참조를 줄여 보는 조우 전용 에디터 계층입니다.

[CustomEditor(typeof(BossEncounterDirector))]
public class BossEncounterDirectorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawOverview();
        EditorGUILayout.Space();
        DrawReferenceWarnings();
        EditorGUILayout.Space();
        DrawDefaultInspector();
        EditorGUILayout.Space();
        DrawTimelineModeSummary();
        EditorGUILayout.Space();
        DrawTimelineTools();
        EditorGUILayout.Space();
        DrawSceneSetupGuide();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawOverview()
    {
        EditorGUILayout.HelpBox(
            "이 컴포넌트는 보스 씬의 메인 흐름을 잡습니다.\n" +
            "조우 시작 -> 컷씬 -> 대화 -> 전투 -> 실패 리셋 -> 승리 연출 순서를 여기서 관리합니다.\n" +
            "이 씬을 조립할 때 가장 먼저 보는 컴포넌트라고 생각하면 됩니다.",
            MessageType.Info);

        EditorGUILayout.HelpBox(
            "핵심 인스펙터 섹션:\n" +
            "- Scene References: 씬에서 직접 연결할 대상\n" +
            "- Optional Timelines: 연결하면 Timeline 사용, 비우면 코드 컷씬 사용\n" +
            "- Fallback Cutscene: Timeline이 없을 때만 쓰는 카메라 연출 시간\n" +
            "- Dialogue: 인트로/승리 대사 데이터",
            MessageType.None);
    }

    private void DrawReferenceWarnings()
    {
        SerializedProperty playerController = serializedObject.FindProperty("playerController");
        SerializedProperty playerInteraction = serializedObject.FindProperty("playerInteraction");
        SerializedProperty cameraFollow = serializedObject.FindProperty("cameraFollow");
        SerializedProperty battleHud = serializedObject.FindProperty("battleHud");
        SerializedProperty bossPrefab = serializedObject.FindProperty("bossPrefab");
        SerializedProperty bossSpawnPoint = serializedObject.FindProperty("bossSpawnPoint");

        bool missingCoreReference =
            playerController.objectReferenceValue == null ||
            playerInteraction.objectReferenceValue == null ||
            cameraFollow.objectReferenceValue == null ||
            battleHud.objectReferenceValue == null ||
            bossPrefab.objectReferenceValue == null ||
            bossSpawnPoint.objectReferenceValue == null;

        if (missingCoreReference)
        {
            EditorGUILayout.HelpBox(
                "Scene References에 비어 있는 필수 칸이 있습니다.\n" +
                "최소한 PlayerController, PlayerInteraction, CameraFollow, BattleHud, Boss Prefab, Boss Spawn Point는 채워 두는 편이 안전합니다.",
                MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("핵심 참조가 채워져 있습니다. 지금 상태로도 이 컴포넌트 역할을 인스펙터에서 추적하기 좋습니다.", MessageType.Info);
        }
    }

    private void DrawTimelineModeSummary()
    {
        SerializedProperty introTimeline = serializedObject.FindProperty("introTimeline");
        SerializedProperty victoryTimeline = serializedObject.FindProperty("victoryTimeline");

        bool hasIntroTimeline = introTimeline.objectReferenceValue != null;
        bool hasVictoryTimeline = victoryTimeline.objectReferenceValue != null;

        if (hasIntroTimeline || hasVictoryTimeline)
        {
            EditorGUILayout.HelpBox(
                "현재 Timeline 기반 모드가 부분적으로 연결되어 있습니다.\n" +
                $"- Intro Timeline: {(hasIntroTimeline ? "연결됨" : "비어 있음")}\n" +
                $"- Victory Timeline: {(hasVictoryTimeline ? "연결됨" : "비어 있음")}\n" +
                "비어 있는 쪽은 Fallback Cutscene 값으로 코드 연출이 실행됩니다.",
                MessageType.Info);
            return;
        }

        EditorGUILayout.HelpBox(
            "현재는 Timeline이 연결되지 않아 코드 컷씬(Fallback Cutscene)을 사용합니다.\n" +
            "PlayableDirector를 연결하면 그 순간부터 해당 구간은 Timeline 기반으로 전환됩니다.",
            MessageType.None);
    }

    private void DrawTimelineTools()
    {
        EditorGUILayout.LabelField("Timeline 작업", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "기본 생성 버튼을 누르면 인트로/승리 PlayableDirector와 기본 Timeline 자산을 만들고,\n" +
            "Player Animation / Boss Animation / Camera Shots / Dialogue 트랙을 준비해서 바로 Timeline 창에서 비주얼 값을 만질 수 있게 합니다.\n" +
            "입력 잠금, 전투 시작 같은 상태 전환은 여전히 코드가 담당합니다.",
            MessageType.Info);

        if (GUILayout.Button("보스 프리팹/스폰 포인트 기본값 채우기"))
        {
            BossEncounterTimelineSetupUtility.EnsureBossSpawnSetup((BossEncounterDirector)target);
            serializedObject.Update();
        }

        if (GUILayout.Button("인트로/승리 Timeline 자동 생성"))
        {
            BossEncounterTimelineSetupUtility.GenerateDefaultTimelines((BossEncounterDirector)target);
            serializedObject.Update();
        }
    }

    private void DrawSceneSetupGuide()
    {
        EditorGUILayout.LabelField("씬에서 이렇게 보면 됩니다", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. 이 컴포넌트가 붙은 오브젝트를 조우/전투 흐름의 메인 오브젝트로 둡니다.\n" +
            "2. Boss Prefab과 Boss Spawn Point를 채워 조우 시 런타임 소환되게 맞춥니다.\n" +
            "3. Timeline 자동 생성 버튼으로 기본 자산을 만든 뒤 Timeline 창에서 클립 길이와 순서를 조정합니다.\n" +
            "4. 대사는 Timeline의 Dialogue 클립 인스펙터나 아래 Dialogue 리스트에서 수정합니다.",
            MessageType.None);
    }
}
