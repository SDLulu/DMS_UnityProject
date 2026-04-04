using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 역할:
// - Yongwoo 씬의 카메라와 배경 루트를 각각 프리팹으로 저장하고 씬 인스턴스로 연결합니다.
// - 오브젝트-부착형 컴포넌트 구조를 유지하면서 씬 구성만 프리팹 기반으로 정리하기 위한 도구입니다.
//
// 구조 포인트:
// - 카메라/배경을 시스템 루트가 아닌 전용 오브젝트 프리팹으로 관리하려는 셋업 유틸리티입니다.

public static class YongwooScenePrefabSetupUtility
{
    public const string CameraPrefabPath = "Assets/_WIP/yongwoo/Prefabs/Scene/Yongwoo/MainCameraRig.prefab";
    public const string BackgroundPrefabPath = "Assets/_WIP/yongwoo/Prefabs/Scene/Yongwoo/ParallaxBackground.prefab";

    private const string TargetSceneName = "Yongwoo";
    private const string TargetScenePath = "Assets/_Scenes/Yongwoo.unity";
    private const string AutoRunFlagPath = "ProjectSettings/YongwooScenePrefabSetup.flag";
    private const string CameraObjectName = "Main Camera";
    private const string BackgroundObjectName = "Background";

    [InitializeOnLoadMethod]
    private static void SchedulePendingAutoRun()
    {
        if (!File.Exists(AutoRunFlagPath))
        {
            return;
        }

        EditorApplication.delayCall -= RunPendingAutoConvert;
        EditorApplication.delayCall += RunPendingAutoConvert;
    }

    [MenuItem("Tools/Yongwoo/Convert Camera And Background To Prefabs")]
    public static void ConvertCameraAndBackgroundToPrefabs()
    {
        TryConvertCameraAndBackgroundToPrefabs();
    }

    private static bool TryConvertCameraAndBackgroundToPrefabs()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || activeScene.name != TargetSceneName)
        {
            if (!File.Exists(TargetScenePath))
            {
                Debug.LogWarning($"Yongwoo Prefab Setup: 대상 씬을 찾지 못했습니다. {TargetScenePath}");
                return false;
            }

            activeScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        }

        GameObject cameraObject = FindRootObject(activeScene, CameraObjectName);
        GameObject backgroundObject = FindRootObject(activeScene, BackgroundObjectName);
        if (cameraObject == null || backgroundObject == null)
        {
            Debug.LogWarning(
                $"Yongwoo Prefab Setup: 씬 루트에서 '{CameraObjectName}' 또는 '{BackgroundObjectName}' 오브젝트를 찾지 못했습니다.");
            return false;
        }

        EnsureFoldersExist(CameraPrefabPath);
        EnsureFoldersExist(BackgroundPrefabPath);

        Undo.RegisterFullObjectHierarchyUndo(cameraObject, "Convert Camera To Prefab");
        Undo.RegisterFullObjectHierarchyUndo(backgroundObject, "Convert Background To Prefab");

        GameObject cameraPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
            cameraObject,
            CameraPrefabPath,
            InteractionMode.UserAction);
        GameObject backgroundPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
            backgroundObject,
            BackgroundPrefabPath,
            InteractionMode.UserAction);

        if (cameraPrefab == null || backgroundPrefab == null)
        {
            Debug.LogWarning("Yongwoo Prefab Setup: 프리팹 저장 또는 씬 연결에 실패했습니다.");
            return false;
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        AssetDatabase.SaveAssets();
        Selection.objects = new Object[] { cameraPrefab, backgroundPrefab };

        string sceneLabel = activeScene.name == TargetSceneName ? TargetSceneName : activeScene.path;
        Debug.Log(
            $"Yongwoo Prefab Setup: {sceneLabel} 씬의 카메라와 배경을 프리팹으로 연결했습니다.\n" +
            $"- Camera: {CameraPrefabPath}\n" +
            $"- Background: {BackgroundPrefabPath}");
        return true;
    }

    [MenuItem("Tools/Yongwoo/Convert Camera And Background To Prefabs", true)]
    private static bool ValidateConvertCameraAndBackgroundToPrefabs()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
        {
            return false;
        }

        if (activeScene.name != TargetSceneName)
        {
            return File.Exists(TargetScenePath);
        }

        return FindRootObject(activeScene, CameraObjectName) != null
            && FindRootObject(activeScene, BackgroundObjectName) != null;
    }

    private static GameObject FindRootObject(Scene scene, string objectName)
    {
        GameObject[] rootObjects = scene.GetRootGameObjects();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            GameObject rootObject = rootObjects[i];
            if (rootObject != null && rootObject.name == objectName)
            {
                return rootObject;
            }
        }

        return null;
    }

    private static void RunPendingAutoConvert()
    {
        EditorApplication.delayCall -= RunPendingAutoConvert;
        if (!File.Exists(AutoRunFlagPath))
        {
            return;
        }

        if (TryConvertCameraAndBackgroundToPrefabs())
        {
            File.Delete(AutoRunFlagPath);
        }
    }

    private static void EnsureFoldersExist(string assetPath)
    {
        string directoryPath = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
        if (string.IsNullOrWhiteSpace(directoryPath) || AssetDatabase.IsValidFolder(directoryPath))
        {
            return;
        }

        string[] parts = directoryPath.Split('/');
        string currentPath = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = $"{currentPath}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            }

            currentPath = nextPath;
        }
    }
}
