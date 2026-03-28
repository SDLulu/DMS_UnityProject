using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PrototypeSaveOnExitPlayMode
{
    static PrototypeSaveOnExitPlayMode()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode)
        {
            return;
        }

        if (SceneManager.GetActiveScene().name != "Yongwoo")
        {
            return;
        }

        SavePlayerTuning();
        SaveBossTuning();
    }

    private static void SavePlayerTuning()
    {
        PrototypePlayerRuntimeConfig runtimeConfig = Object.FindFirstObjectByType<PrototypePlayerRuntimeConfig>();
        if (runtimeConfig == null)
        {
            return;
        }

        PrototypePlayerConfig snapshot = runtimeConfig.CreateConfigSnapshot();
        PrototypePrefabAutoSaveUtility.ApplyPlayerConfigToPrefabAsset(
            PrototypePrefabAutoSaveUtility.PlayerPrefabPath,
            snapshot);
    }

    private static void SaveBossTuning()
    {
        PrototypeBossController bossController = Object.FindFirstObjectByType<PrototypeBossController>();
        if (bossController == null)
        {
            return;
        }

        PrototypeBossConfig snapshot = bossController.CreateConfigSnapshot();
        PrototypePrefabAutoSaveUtility.ApplyBossConfigToPrefabAsset(
            PrototypePrefabAutoSaveUtility.BossPrefabPath,
            snapshot);
    }
}
