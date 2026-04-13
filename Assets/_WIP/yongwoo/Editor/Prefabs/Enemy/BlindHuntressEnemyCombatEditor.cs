using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 역할:
// - Blind Huntress 적의 공격 히트박스 위치와 크기를 루트 인스펙터 한 곳에서 같이 조정합니다.

[CustomEditor(typeof(BlindHuntressEnemyCombat))]
public class BlindHuntressEnemyCombatEditor : Editor
{
    private SerializedProperty _attackHitboxAnchor;
    private SerializedProperty _dashAttackHitboxAnchor;
    private SerializedProperty _upAttackHitboxAnchor;
    private SerializedProperty _attackAction;
    private SerializedProperty _dashAttackAction;
    private SerializedProperty _upAttackAction;

    private void OnEnable()
    {
        _attackHitboxAnchor = serializedObject.FindProperty("attackHitboxAnchor");
        _dashAttackHitboxAnchor = serializedObject.FindProperty("dashAttackHitboxAnchor");
        _upAttackHitboxAnchor = serializedObject.FindProperty("upAttackHitboxAnchor");
        _attackAction = serializedObject.FindProperty("attackAction");
        _dashAttackAction = serializedObject.FindProperty("dashAttackAction");
        _upAttackAction = serializedObject.FindProperty("upAttackAction");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "히트박스 위치와 크기는 아래 Hitbox Layout에서 먼저 맞춥니다.\n" +
            "위치는 Sensors 자식 앵커의 localPosition, 크기는 각 액션의 hitboxSize에 연결됩니다.\n" +
            "즉, 루트 인스펙터 한 곳에서 위치와 크기를 같이 조정할 수 있습니다.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hitbox Layout", EditorStyles.boldLabel);

        DrawHitboxEditor("Attack", _attackHitboxAnchor, _attackAction.FindPropertyRelative("hitboxSize"));
        DrawHitboxEditor("DashAttack", _dashAttackHitboxAnchor, _dashAttackAction.FindPropertyRelative("hitboxSize"));
        DrawHitboxEditor("UpAttack", _upAttackHitboxAnchor, _upAttackAction.FindPropertyRelative("hitboxSize"));

        EditorGUILayout.Space();
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "attackHitboxAnchor",
            "dashAttackHitboxAnchor",
            "upAttackHitboxAnchor");

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHitboxEditor(string label, SerializedProperty anchorProperty, SerializedProperty sizeProperty)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        Transform anchor = anchorProperty.objectReferenceValue as Transform;
        if (anchor == null)
        {
            EditorGUILayout.HelpBox("히트박스 앵커가 비어 있습니다. Builder를 다시 돌리거나 직접 연결해야 합니다.", MessageType.Warning);
            EditorGUILayout.PropertyField(anchorProperty, new GUIContent("Anchor"));
            EditorGUILayout.PropertyField(sizeProperty, new GUIContent("Size"));
            EditorGUILayout.EndVertical();
            return;
        }

        Vector3 localPosition = anchor.localPosition;
        Vector2 position2D = new Vector2(localPosition.x, localPosition.y);

        EditorGUI.BeginChangeCheck();
        Vector2 newPosition = EditorGUILayout.Vector2Field("Local Position", position2D);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(anchor, $"Adjust {label} Hitbox Position");
            anchor.localPosition = new Vector3(newPosition.x, newPosition.y, localPosition.z);
            EditorUtility.SetDirty(anchor);
            PrefabUtility.RecordPrefabInstancePropertyModifications(anchor);

            if (anchor.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(anchor.gameObject.scene);
            }
        }

        EditorGUILayout.PropertyField(sizeProperty, new GUIContent("Size"));
        EditorGUILayout.ObjectField("Anchor", anchor, typeof(Transform), true);

        EditorGUILayout.EndVertical();
    }
}
