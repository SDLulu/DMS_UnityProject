using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class PrototypePrefabAutoSaveUtility
{
    public const string PlayerPrefabPath = "Assets/_WIP/yongwoo/Prefabs/Prototype/Player.prefab";
    public const string BossPrefabPath = "Assets/_WIP/yongwoo/Prefabs/Prototype/Boss.prefab";

#if UNITY_EDITOR
    public static void ApplyPlayerConfigToPrefabAsset(string prefabPath, PrototypePlayerConfig config)
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

        PrototypePlayerRuntimeConfig runtimeConfig = prefabAsset.GetComponent<PrototypePlayerRuntimeConfig>();
        SimplePlayerController controller = prefabAsset.GetComponent<SimplePlayerController>();
        SimplePlayerCombat combat = prefabAsset.GetComponent<SimplePlayerCombat>();
        PrototypeHealth health = prefabAsset.GetComponent<PrototypeHealth>();
        BoxCollider2D bodyCollider = prefabAsset.GetComponent<BoxCollider2D>();

        if (runtimeConfig == null)
        {
            return;
        }

        PrototypePlayerConfig snapshot = PrototypePlayerConfigLoader.Sanitize(PrototypePlayerConfigLoader.DeepClone(config));
        runtimeConfig.SetSerializedConfig(snapshot);
        controller?.ApplyConfig(snapshot.movement);
        combat?.ApplyConfig(snapshot.attack);
        health?.ApplyPlayerConfig(snapshot.health, preserveHealthRatio: false);
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
        if (health != null)
        {
            EditorUtility.SetDirty(health);
        }
        if (bodyCollider != null)
        {
            EditorUtility.SetDirty(bodyCollider);
        }

        EditorUtility.SetDirty(prefabAsset);
        PrefabUtility.SavePrefabAsset(prefabAsset);
        AssetDatabase.SaveAssets();
    }

    public static void ApplyBossConfigToPrefabAsset(string prefabPath, PrototypeBossConfig config)
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

        PrototypeBossController controller = prefabAsset.GetComponent<PrototypeBossController>();
        if (controller == null)
        {
            return;
        }

        PrototypeBossConfig snapshot = PrototypeBossConfigLoader.Sanitize(PrototypeBossConfigLoader.DeepClone(config));
        controller.SetSerializedConfig(snapshot);
        controller.RefreshRuntimeConfig(resetBossState: true, preserveHealthRatio: false);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(prefabAsset);
        PrefabUtility.SavePrefabAsset(prefabAsset);
        AssetDatabase.SaveAssets();
    }
#else
    public static void ApplyPlayerConfigToPrefabAsset(string prefabPath, PrototypePlayerConfig config)
    {
    }

    public static void ApplyBossConfigToPrefabAsset(string prefabPath, PrototypeBossConfig config)
    {
    }
#endif
}
