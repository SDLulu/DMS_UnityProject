using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimplePlayerCombat))]
public class SimplePlayerCombatEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "이 컴포넌트는 플레이어 근접 공격의 기준입니다.\n" +
            "attackSize와 attackOffset은 플레이어 루트 기준 판정 박스를 뜻하고,\n" +
            "FacingDirection에 따라 좌우가 자동 반전됩니다.",
            MessageType.Info);

        EditorGUILayout.Space();
        DrawDefaultInspector();
    }
}
