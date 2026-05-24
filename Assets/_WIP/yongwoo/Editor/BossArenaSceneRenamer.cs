#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BossArenaSceneRenamer
{
    private const string ArenaName = "아레나";
    private const string CameraAnchorName = "카메라앵커";
    private const string TeleportFolderName = "텔포앵커";

    [MenuItem("Tools/Yongwoo/Boss/Fix Arena Scene Setup")]
    public static void FixArenaSceneSetup()
    {
        FixArenaObjectNames();
        EnsureArenaTutorialMarkers();
    }

    [MenuItem("Tools/Yongwoo/Boss/Fix Arena Object Names")]
    public static void FixArenaObjectNames()
    {
        BossBattleArena arena = Object.FindFirstObjectByType<BossBattleArena>();
        if (arena == null)
        {
            Debug.LogWarning("[BossArenaRenamer] BossBattleArena not found.");
            return;
        }

        Transform arenaTf = arena.transform;
        arenaTf.name = ArenaName;

        Transform camAnchor = null;
        Transform anchorRoot = null;

        for (int i = arenaTf.childCount - 1; i >= 0; i--)
        {
            Transform child = arenaTf.GetChild(i);
            if (child.GetComponent<BoxCollider2D>() != null)
            {
                Object.DestroyImmediate(child.gameObject);
                continue;
            }

            bool hasAnchorChild = false;
            for (int j = 0; j < child.childCount; j++)
            {
                if (child.GetChild(j).name.StartsWith("Anchor_"))
                {
                    hasAnchorChild = true;
                    break;
                }
            }

            if (hasAnchorChild)
            {
                anchorRoot = child;
                child.name = TeleportFolderName;
            }
            else
            {
                camAnchor = child;
                child.name = CameraAnchorName;
            }
        }

        SerializedObject arenaSo = new SerializedObject(arena);
        arenaSo.FindProperty("cameraAnchor").objectReferenceValue = camAnchor;

        if (anchorRoot != null)
        {
            SerializedProperty anchorsProp = arenaSo.FindProperty("teleportAnchors");
            anchorsProp.arraySize = anchorRoot.childCount;
            for (int i = 0; i < anchorRoot.childCount; i++)
            {
                anchorsProp.GetArrayElementAtIndex(i).objectReferenceValue = anchorRoot.GetChild(i);
            }
        }

        arenaSo.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(arena.gameObject.scene);
        Debug.Log($"[BossArenaRenamer] {ArenaName} / {CameraAnchorName} / {TeleportFolderName}", arena);
    }

    [MenuItem("Tools/Yongwoo/Boss/Place P1 Teleport Anchors")]
    public static void PlaceP1TeleportAnchors()
    {
        BossBattleArena arena = Object.FindFirstObjectByType<BossBattleArena>();
        if (arena == null)
        {
            Debug.LogWarning("[BossArenaRenamer] BossBattleArena not found.");
            return;
        }

        Transform anchorRoot = FindTeleportAnchorRoot(arena.transform);
        if (anchorRoot == null)
        {
            Debug.LogWarning("[BossArenaRenamer] 텔포앵커 folder not found.");
            return;
        }

        Bounds bounds = ComputeArenaBounds(arena, out Transform cameraAnchor);
        if (cameraAnchor == null)
        {
            Debug.LogWarning("[BossArenaRenamer] cameraAnchor not assigned.");
            return;
        }

        // 보스전로직.md §4.3 — 4모서리 + 중앙 위. 가장자리에서 살짝 안쪽(inset).
        const float inset = 0.75f;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3 center = bounds.center;

        SetAnchorPosition(anchorRoot, "Anchor_01", new Vector3(min.x + inset, min.y + inset, 0f));
        SetAnchorPosition(anchorRoot, "Anchor_02", new Vector3(max.x - inset, min.y + inset, 0f));
        SetAnchorPosition(anchorRoot, "Anchor_03", new Vector3(min.x + inset, max.y - inset, 0f));
        SetAnchorPosition(anchorRoot, "Anchor_04", new Vector3(max.x - inset, max.y - inset, 0f));
        SetAnchorPosition(anchorRoot, "Anchor_05", new Vector3(center.x, max.y - inset, 0f));

        SerializedObject arenaSo = new SerializedObject(arena);
        SerializedProperty anchorsProp = arenaSo.FindProperty("teleportAnchors");
        anchorsProp.arraySize = anchorRoot.childCount;
        for (int i = 0; i < anchorRoot.childCount; i++)
        {
            anchorsProp.GetArrayElementAtIndex(i).objectReferenceValue = anchorRoot.GetChild(i);
        }

        arenaSo.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(arena.gameObject.scene);
        Debug.Log(
            $"[BossArenaRenamer] P1 anchors placed (4 corners + center-top, inset={inset}u). bounds={bounds.size}",
            arena);
    }

    [MenuItem("Tools/Yongwoo/Boss/Ensure Arena Tutorial Markers")]
    public static void EnsureArenaTutorialMarkers()
    {
        BossBattleArena arena = Object.FindFirstObjectByType<BossBattleArena>();
        if (arena == null)
        {
            Debug.LogWarning("[BossArenaRenamer] BossBattleArena not found.");
            return;
        }

        Transform arenaTf = arena.transform;
        Transform camAnchor = null;
        Transform anchorRoot = null;

        for (int i = 0; i < arenaTf.childCount; i++)
        {
            Transform child = arenaTf.GetChild(i);
            bool hasAnchorChild = false;
            for (int j = 0; j < child.childCount; j++)
            {
                if (child.GetChild(j).name.StartsWith("Anchor_"))
                {
                    hasAnchorChild = true;
                    break;
                }
            }

            if (hasAnchorChild)
            {
                anchorRoot = child;
            }
            else if (child.GetComponent<BoxCollider2D>() == null)
            {
                camAnchor = child;
            }
        }

        if (camAnchor != null)
        {
            EnsureMarker(camAnchor.gameObject, TutorialMarker.MarkerType.BossCameraAnchor, 0.45f);
        }

        if (anchorRoot != null)
        {
            for (int i = 0; i < anchorRoot.childCount; i++)
            {
                Transform anchor = anchorRoot.GetChild(i);
                EnsureMarker(anchor.gameObject, TutorialMarker.MarkerType.BossTeleportAnchor, 0.28f);
            }
        }

        BossBattleEntryTrigger[] entryTriggers = Object.FindObjectsByType<BossBattleEntryTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < entryTriggers.Length; i++)
        {
            EnsureMarker(entryTriggers[i].gameObject, TutorialMarker.MarkerType.Trigger, 0.45f);
        }

        EditorSceneManager.MarkSceneDirty(arena.gameObject.scene);
        Debug.Log("[BossArenaRenamer] TutorialMarker wired for boss arena.", arena);
    }

    private static void EnsureMarker(GameObject target, TutorialMarker.MarkerType type, float radius)
    {
        TutorialMarker marker = target.GetComponent<TutorialMarker>();
        if (marker == null)
        {
            marker = target.AddComponent<TutorialMarker>();
        }

        marker.Configure(type, radius);
        EditorUtility.SetDirty(marker);
    }

    private static Transform FindTeleportAnchorRoot(Transform arenaTf)
    {
        Transform named = arenaTf.Find(TeleportFolderName);
        if (named != null)
        {
            return named;
        }

        for (int i = 0; i < arenaTf.childCount; i++)
        {
            Transform child = arenaTf.GetChild(i);
            for (int j = 0; j < child.childCount; j++)
            {
                if (child.GetChild(j).name.StartsWith("Anchor_"))
                {
                    return child;
                }
            }
        }

        return null;
    }

    private static Bounds ComputeArenaBounds(BossBattleArena arena, out Transform cameraAnchor)
    {
        SerializedObject arenaSo = new SerializedObject(arena);
        cameraAnchor = arenaSo.FindProperty("cameraAnchor").objectReferenceValue as Transform;
        float padding = arenaSo.FindProperty("boundsPadding").floatValue;
        float orthoOverride = arenaSo.FindProperty("cameraOrthoSize").floatValue;

        Camera cam = Camera.main;
        Vector3 center = cameraAnchor != null ? cameraAnchor.position : arena.transform.position;
        float halfHeight = orthoOverride > 0f
            ? orthoOverride
            : cam != null
                ? cam.orthographicSize
                : 5f;
        float halfWidth = halfHeight * (cam != null ? cam.aspect : 16f / 9f);

        return new Bounds(
            center,
            new Vector3(
                Mathf.Max(0.1f, halfWidth * 2f - padding * 2f),
                Mathf.Max(0.1f, halfHeight * 2f - padding * 2f),
                0f));
    }

    private static void SetAnchorPosition(Transform anchorRoot, string anchorName, Vector3 worldPosition)
    {
        Transform anchor = anchorRoot.Find(anchorName);
        if (anchor == null)
        {
            Debug.LogWarning($"[BossArenaRenamer] Missing {anchorName}.");
            return;
        }

        Undo.RecordObject(anchor, "Place P1 Teleport Anchor");
        worldPosition.z = anchor.position.z;
        anchor.position = worldPosition;
        EditorUtility.SetDirty(anchor);
    }
}
#endif
