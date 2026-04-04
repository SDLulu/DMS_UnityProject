using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// 역할:
// - 플레이 모드를 종료할 때 조정된 튜닝값을 감지해 프리팹 저장 유틸리티를 호출합니다.
// - Yongwoo 씬에서만 자동 저장 흐름을 붙여 반복 조정을 빠르게 만듭니다.
//
// 구조 포인트:
// - 런타임 조정값을 에디터 자산으로 되돌리는 마지막 훅입니다.

[InitializeOnLoad]
public static class SaveOnExitPlayMode
{
    static SaveOnExitPlayMode()
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
        PlayerRuntimeConfig runtimeConfig = Object.FindFirstObjectByType<PlayerRuntimeConfig>();
        if (runtimeConfig == null)
        {
            return;
        }

        PlayerConfig snapshot = runtimeConfig.CreateConfigSnapshot();
        PrefabAutoSaveUtility.ApplyPlayerConfigToPrefabAsset(
            PrefabAutoSaveUtility.PlayerPrefabPath,
            snapshot);
    }

    private static void SaveBossTuning()
    {
        BossController bossController = Object.FindFirstObjectByType<BossController>();
        if (bossController == null)
        {
            return;
        }

        BossConfig snapshot = bossController.CreateConfigSnapshot();
        PrefabAutoSaveUtility.ApplyBossConfigToPrefabAsset(
            PrefabAutoSaveUtility.BossPrefabPath,
            snapshot);
    }
}
