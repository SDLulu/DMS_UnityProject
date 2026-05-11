using UnityEditor;
using UnityEngine;

// 역할:
// - SimplePlayerCombat 인스펙터에 무기 구조 안내를 표시합니다.

[CustomEditor(typeof(SimplePlayerCombat))]
public class SimplePlayerCombatEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "무기는 WeaponOrigin/MuzzlePoint 구조로 배치합니다.\n" +
            "SwordWeapon/GunWeapon 레퍼런스를 연결하면\n" +
            "Q키로 전환, 좌클릭으로 공격합니다.",
            MessageType.Info);

        EditorGUILayout.Space();
        DrawDefaultInspector();
    }
}
