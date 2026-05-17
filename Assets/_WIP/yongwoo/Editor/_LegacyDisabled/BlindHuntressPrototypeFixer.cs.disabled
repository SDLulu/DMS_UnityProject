using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// 역할:
// - 현재 씬의 Blind Huntress Prototype 방향/물리 설정을 즉시 수정합니다.
// - 빌더를 다시 돌리지 않아도 현재 배치된 오브젝트를 바로 고칠 수 있게 합니다.

public static class BlindHuntressPrototypeFixer
{
    [MenuItem("Tools/Yongwoo/Fix Blind Huntress Facing")]
    public static void FixFacing()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null || root.name != "Blind Huntress Prototype")
            {
                continue;
            }

            SimplePlayerController controller = root.GetComponent<SimplePlayerController>();
            if (controller == null)
            {
                continue;
            }

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("invertVisualFacing").boolValue = false;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            Rigidbody2D body = root.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                EditorUtility.SetDirty(body);
            }

            EditorUtility.SetDirty(root);
            Debug.Log("Blind Huntress facing/physics fixed on current scene object.", root);
        }
    }
}
