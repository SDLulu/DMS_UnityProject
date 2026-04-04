using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// 역할:
// - 플레이 중 조정한 플레이어/보스 튜닝값을 대응 프리팹 asset에 다시 기록합니다.
// - 에디터 전용 흐름에서 런타임 스냅샷을 프리팹 직렬화 값으로 되돌리는 유틸리티입니다.
//
// 구조 포인트:
// - 저장 버튼이 아니라 에디터 자동 저장 훅에서 호출되는 백엔드 계층입니다.

public static class PrefabAutoSaveUtility
{
    public const string PlayerPrefabPath = "Assets/_WIP/yongwoo/Prefabs/Prototype/Player.prefab";
    public const string BossPrefabPath = "Assets/_WIP/yongwoo/Prefabs/Prototype/Boss.prefab";

#if UNITY_EDITOR
    public static void ApplyPlayerConfigToPrefabAsset(string prefabPath, PlayerConfig config)
    {
        if (string.IsNullOrWhiteSpace(prefabPath) || config == null)
        {
            return;
        }

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null)
        {
            return;
        }

        PlayerRuntimeConfig runtimeConfig = prefabAsset.GetComponent<PlayerRuntimeConfig>();
        SimplePlayerController controller = prefabAsset.GetComponent<SimplePlayerController>();
        SimplePlayerCombat combat = prefabAsset.GetComponent<SimplePlayerCombat>();
        PlayerInteraction interaction = prefabAsset.GetComponent<PlayerInteraction>();
        BoxCollider2D bodyCollider = prefabAsset.GetComponent<BoxCollider2D>();

        if (runtimeConfig == null)
        {
            return;
        }

        PlayerConfig snapshot = PlayerConfigLoader.Sanitize(PlayerConfigLoader.DeepClone(config));
        runtimeConfig.SetSerializedConfig(snapshot);
        controller?.ApplyConfig(snapshot.movement);
        combat?.ApplyConfig(snapshot.attack);
        interaction?.ApplyHealthConfig(snapshot.health, preserveHealthRatio: false);
        if (bodyCollider != null)
        {
            bodyCollider.size = new Vector2(snapshot.collider.width, snapshot.collider.height);
            bodyCollider.offset = new Vector2(snapshot.collider.offsetX, snapshot.collider.offsetY);
            bodyCollider.isTrigger = snapshot.collider.isTrigger;
        }

        EditorUtility.SetDirty(runtimeConfig);
        if (controller != null)
        {
            EditorUtility.SetDirty(controller);
        }
        if (combat != null)
        {
            EditorUtility.SetDirty(combat);
        }
        if (interaction != null)
        {
            EditorUtility.SetDirty(interaction);
        }
        if (bodyCollider != null)
        {
            EditorUtility.SetDirty(bodyCollider);
        }

        EditorUtility.SetDirty(prefabAsset);
        PrefabUtility.SavePrefabAsset(prefabAsset);
        AssetDatabase.SaveAssets();
    }

    public static void ApplyBossConfigToPrefabAsset(string prefabPath, BossConfig config)
    {
        if (string.IsNullOrWhiteSpace(prefabPath) || config == null)
        {
            return;
        }

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null)
        {
            return;
        }

        BossController controller = prefabAsset.GetComponent<BossController>();
        if (controller == null)
        {
            return;
        }

        BossConfig snapshot = BossConfigLoader.Sanitize(BossConfigLoader.DeepClone(config));
        controller.SetSerializedConfig(snapshot);
        controller.RefreshRuntimeConfig(resetBossState: true, preserveHealthRatio: false);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(prefabAsset);
        PrefabUtility.SavePrefabAsset(prefabAsset);
        AssetDatabase.SaveAssets();
    }
#else
    public static void ApplyPlayerConfigToPrefabAsset(string prefabPath, PlayerConfig config)
    {
    }

    public static void ApplyBossConfigToPrefabAsset(string prefabPath, BossConfig config)
    {
    }
#endif
}
