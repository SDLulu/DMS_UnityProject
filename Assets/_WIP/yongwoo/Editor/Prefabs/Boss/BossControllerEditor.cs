using UnityEditor;
using UnityEngine;

// 역할:
// - BossController 인스펙터에서 핵심 조정 포인트와 전제 구조를 설명합니다.
// - 런타임 전투 규칙을 바꾸기 전에 프리팹에서 무엇을 같이 봐야 하는지 안내합니다.
//
// 구조 포인트:
// - 보스 튜닝 경험을 돕는 에디터 보조 계층입니다.

[CustomEditor(typeof(BossController))]
public class BossControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "이 컴포넌트는 보스 전투 규칙과 튜닝값의 기준입니다.\n" +
            "Boss.prefab의 Visual / Sensors / Debug 자식 구조를 전제로 동작하며,\n" +
            "패턴 수치, 몸통 판정, 투사체 발사 기준은 여기에서 조정합니다.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("프리팹에서 같이 볼 것", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "- Visual: SpriteRenderer + Animator\n" +
            "- Sensors/ProjectileSpawn: 투사체 발사 기준점\n" +
            "- Debug: 대시/내려찍기 시각화 부모",
            MessageType.None);

        EditorGUILayout.Space();
        DrawDefaultInspector();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("플레이 중 바꾼 값은 즉시 반영되고, 플레이 모드를 끌 때 Boss 프리팹에 저장됩니다.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("플레이 종료 시 저장", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("플레이 중 이 컴포넌트 값을 바꾸면 즉시 적용됩니다. 저장은 플레이 모드를 끌 때 Boss 프리팹에 한 번만 반영됩니다.", MessageType.None);
    }
}
