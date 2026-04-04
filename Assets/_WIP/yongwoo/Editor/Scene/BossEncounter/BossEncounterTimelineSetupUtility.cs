using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// 역할:
// - 보스 조우용 Timeline 자산과 트랙 바인딩을 자동으로 만들어 줍니다.
// - 디렉터, 보스 프리팹, 카메라 트랙이 기대하는 기본 배선을 빠르게 준비합니다.
//
// 구조 포인트:
// - 컷신 실험용 배선을 수동 반복 없이 맞추기 위한 제작 도구입니다.

public static class BossEncounterTimelineSetupUtility
{
    private const string DefaultBossPrefabPath = "Assets/_WIP/yongwoo/Prefabs/Prototype/Boss.prefab";
    private const string TimelineFolderPath = "Assets/_WIP/yongwoo/Timelines";
    private const string IntroTimelineAssetPath = TimelineFolderPath + "/BossEncounterIntro.playable";
    private const string VictoryTimelineAssetPath = TimelineFolderPath + "/BossEncounterVictory.playable";
    private const string IntroDirectorObjectName = "BossIntroTimelineDirector";
    private const string VictoryDirectorObjectName = "BossVictoryTimelineDirector";
    private const float DefaultBlendDuration = 0.18f;
    private const string DefaultBossSpawnPointName = "BossSpawnPoint";

    [MenuItem("Tools/Boss Encounter/Generate Default Timelines")]
    public static void GenerateDefaultTimelinesFromMenu()
    {
        if (!TryGetEncounterDirector(out BossEncounterDirector encounterDirector))
        {
            return;
        }

        GenerateDefaultTimelines(encounterDirector);
        Debug.Log("Boss Timeline Setup: 보스 인트로/승리 Timeline 기본 셋업을 생성했습니다.");
    }

    [MenuItem("Tools/Boss Encounter/Ensure Boss Spawn Setup")]
    public static void EnsureBossSpawnSetupFromMenu()
    {
        if (!TryGetEncounterDirector(out BossEncounterDirector encounterDirector))
        {
            return;
        }

        EnsureBossSpawnSetup(encounterDirector);
        Debug.Log("Boss Timeline Setup: 보스 프리팹과 스폰 포인트를 준비했습니다.");
    }

    public static void GenerateDefaultTimelines(BossEncounterDirector encounterDirector)
    {
        if (encounterDirector == null)
        {
            return;
        }

        EnsureBossSpawnSetup(encounterDirector);
        EnsureFolder(TimelineFolderPath);

        TimelineAsset introAsset = LoadOrCreateTimelineAsset(IntroTimelineAssetPath);
        TimelineAsset victoryAsset = LoadOrCreateTimelineAsset(VictoryTimelineAssetPath);

        RebuildIntroTimeline(introAsset, encounterDirector);
        RebuildVictoryTimeline(victoryAsset, encounterDirector);

        PlayableDirector introDirector = EnsureDirector(encounterDirector.transform, IntroDirectorObjectName, introAsset);
        PlayableDirector victoryDirector = EnsureDirector(encounterDirector.transform, VictoryDirectorObjectName, victoryAsset);

        BindTracks(introDirector, introAsset, encounterDirector);
        BindTracks(victoryDirector, victoryAsset, encounterDirector);
        AssignTimelineReferences(encounterDirector, introDirector, victoryDirector);

        MarkTimelineSetupDirty(encounterDirector, introAsset, victoryAsset, introDirector, victoryDirector);
        AssetDatabase.SaveAssets();
    }

    public static void EnsureBossSpawnSetup(BossEncounterDirector encounterDirector)
    {
        if (encounterDirector == null)
        {
            return;
        }

        BossController bossPrefab = AssetDatabase.LoadAssetAtPath<BossController>(DefaultBossPrefabPath);
        Transform bossSpawnPoint = FindOrCreateBossSpawnPoint(encounterDirector);

        AssignBossSpawnReferences(encounterDirector, bossPrefab, bossSpawnPoint);
        EditorUtility.SetDirty(encounterDirector);
        EditorSceneManager.MarkSceneDirty(encounterDirector.gameObject.scene);
    }

    private static void RebuildIntroTimeline(TimelineAsset timelineAsset, BossEncounterDirector encounterDirector)
    {
        EncounterCameraTrack cameraTrack = PrepareGeneratedVisualTracks(timelineAsset);
        double cameraCursor = 0d;

        CreateCameraClip(cameraTrack, "와이드 샷", cameraCursor, encounterDirector.IntroWideDuration + DefaultBlendDuration, EncounterCameraFrameType.Wide);
        cameraCursor += encounterDirector.IntroWideDuration;

        CreateCameraClip(
            cameraTrack,
            "보스 강조 샷",
            cameraCursor - DefaultBlendDuration,
            encounterDirector.IntroBossDuration + encounterDirector.IntroHoldDuration + DefaultBlendDuration,
            EncounterCameraFrameType.Boss);
        cameraCursor += encounterDirector.IntroBossDuration + encounterDirector.IntroHoldDuration;

        CreateCameraClip(
            cameraTrack,
            "전투 복귀 샷",
            cameraCursor - DefaultBlendDuration,
            encounterDirector.IntroWideDuration + DefaultBlendDuration,
            EncounterCameraFrameType.Combat);
        cameraCursor += encounterDirector.IntroWideDuration;

        CreateDialogueTrack(timelineAsset, encounterDirector.IntroLines, cameraCursor, "Intro Dialogue");
    }

    private static void RebuildVictoryTimeline(TimelineAsset timelineAsset, BossEncounterDirector encounterDirector)
    {
        EncounterCameraTrack cameraTrack = PrepareGeneratedVisualTracks(timelineAsset);
        double cameraDuration = encounterDirector.VictoryPanDuration + encounterDirector.VictoryHoldDuration;
        CreateCameraClip(cameraTrack, "마무리 샷", 0d, cameraDuration, EncounterCameraFrameType.Boss);

        CreateDialogueTrack(timelineAsset, encounterDirector.VictoryLines, cameraDuration, "Victory Dialogue");
    }

    private static double CreateDialogueTrack(TimelineAsset timelineAsset, IReadOnlyList<EncounterDialogueLine> lines, double startTime, string trackName)
    {
        if (lines == null || lines.Count == 0)
        {
            return startTime;
        }

        EncounterDialogueTrack dialogueTrack = timelineAsset.CreateTrack<EncounterDialogueTrack>(trackName);
        double cursor = startTime;

        for (int i = 0; i < lines.Count; i++)
        {
            EncounterDialogueLine line = lines[i];
            if (line == null)
            {
                continue;
            }

            TimelineClip clip = dialogueTrack.CreateClip<EncounterDialoguePlayableAsset>();
            clip.start = cursor;
            clip.duration = EstimateDialogueDuration(line);
            clip.displayName = string.IsNullOrWhiteSpace(line.speakerName) ? $"대사 {i + 1}" : $"{line.speakerName} {i + 1}";

            EncounterDialoguePlayableAsset playableAsset = clip.asset as EncounterDialoguePlayableAsset;
            if (playableAsset != null)
            {
                playableAsset.template.line = line.Clone();
                playableAsset.template.useTypewriter = true;
            }

            cursor += clip.duration;
        }

        return cursor;
    }

    private static void CreateCameraClip(
        EncounterCameraTrack track,
        string displayName,
        double start,
        double duration,
        EncounterCameraFrameType frameType)
    {
        TimelineClip clip = track.CreateClip<EncounterCameraPlayableAsset>();
        clip.start = start;
        clip.duration = duration;
        clip.displayName = displayName;

        EncounterCameraPlayableAsset playableAsset = clip.asset as EncounterCameraPlayableAsset;
        if (playableAsset != null)
        {
            playableAsset.template.frameType = frameType;
            playableAsset.template.offset = Vector2.zero;
        }
    }

    private static double EstimateDialogueDuration(EncounterDialogueLine line)
    {
        int textLength = Mathf.Max(0, (line?.text ?? string.Empty).Length);
        float typingDuration = Mathf.Max(0.4f, textLength / 24f);
        return typingDuration + 1.1f;
    }

    private static void RemoveGeneratedTracks(TimelineAsset timelineAsset)
    {
        List<TrackAsset> tracksToDelete = new();
        foreach (TrackAsset track in timelineAsset.GetOutputTracks())
        {
            if (track is EncounterCameraTrack || track is EncounterDialogueTrack)
            {
                tracksToDelete.Add(track);
            }
        }

        for (int i = 0; i < tracksToDelete.Count; i++)
        {
            timelineAsset.DeleteTrack(tracksToDelete[i]);
        }
    }

    private static void BindTracks(PlayableDirector playableDirector, TimelineAsset timelineAsset, BossEncounterDirector encounterDirector)
    {
        if (playableDirector == null || timelineAsset == null)
        {
            return;
        }

        EncounterDialoguePanel dialoguePanel = Object.FindFirstObjectByType<EncounterDialoguePanel>();
        foreach (TrackAsset track in timelineAsset.GetOutputTracks())
        {
            if (TryBindCameraTrack(playableDirector, track, encounterDirector))
            {
                continue;
            }

            TryBindDialogueTrack(playableDirector, track, dialoguePanel);
        }
    }

    private static EncounterCameraTrack PrepareGeneratedVisualTracks(TimelineAsset timelineAsset)
    {
        RemoveGeneratedTracks(timelineAsset);
        EnsureAnimationTrack(timelineAsset, "Player Animation");
        EnsureAnimationTrack(timelineAsset, "Boss Animation");
        return timelineAsset.CreateTrack<EncounterCameraTrack>("Camera Shots");
    }

    private static void AssignTimelineReferences(
        BossEncounterDirector encounterDirector,
        PlayableDirector introDirector,
        PlayableDirector victoryDirector)
    {
        SerializedObject serializedObject = new(encounterDirector);
        serializedObject.Update();
        serializedObject.FindProperty("introTimeline").objectReferenceValue = introDirector;
        serializedObject.FindProperty("victoryTimeline").objectReferenceValue = victoryDirector;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignBossSpawnReferences(BossEncounterDirector encounterDirector, BossController bossPrefab, Transform bossSpawnPoint)
    {
        SerializedObject serializedObject = new(encounterDirector);
        serializedObject.Update();
        serializedObject.FindProperty("bossPrefab").objectReferenceValue = bossPrefab;
        serializedObject.FindProperty("bossSpawnPoint").objectReferenceValue = bossSpawnPoint;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform FindOrCreateBossSpawnPoint(BossEncounterDirector encounterDirector)
    {
        if (encounterDirector.BossSpawnPoint != null)
        {
            return encounterDirector.BossSpawnPoint;
        }

        BossController sceneBoss = Object.FindFirstObjectByType<BossController>();
        Vector3 spawnPosition = sceneBoss != null ? sceneBoss.transform.position : encounterDirector.transform.position + Vector3.right * 5.5f;
        Quaternion spawnRotation = sceneBoss != null ? sceneBoss.transform.rotation : Quaternion.identity;

        GameObject markerObject = new GameObject(DefaultBossSpawnPointName);
        Undo.RegisterCreatedObjectUndo(markerObject, "Create Boss Spawn Point");
        markerObject.transform.SetParent(encounterDirector.transform.parent, false);
        markerObject.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        return markerObject.transform;
    }

    private static PlayableDirector EnsureDirector(Transform parent, string objectName, TimelineAsset timelineAsset)
    {
        Transform child = parent.Find(objectName);
        GameObject directorObject = child != null ? child.gameObject : new GameObject(objectName);
        if (child == null)
        {
            directorObject.transform.SetParent(parent, false);
        }

        PlayableDirector playableDirector = directorObject.GetComponent<PlayableDirector>();
        if (playableDirector == null)
        {
            playableDirector = directorObject.AddComponent<PlayableDirector>();
        }

        playableDirector.playableAsset = timelineAsset;
        playableDirector.extrapolationMode = DirectorWrapMode.None;
        return playableDirector;
    }

    private static TimelineAsset LoadOrCreateTimelineAsset(string assetPath)
    {
        TimelineAsset timelineAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(assetPath);
        if (timelineAsset != null)
        {
            return timelineAsset;
        }

        timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timelineAsset, assetPath);
        return timelineAsset;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = currentPath + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            }

            currentPath = nextPath;
        }
    }

    private static void EnsureAnimationTrack(TimelineAsset timelineAsset, string trackName)
    {
        foreach (TrackAsset track in timelineAsset.GetOutputTracks())
        {
            if (track is AnimationTrack && track.name == trackName)
            {
                return;
            }
        }

        timelineAsset.CreateTrack<AnimationTrack>(trackName);
    }

    private static bool TryBindCameraTrack(PlayableDirector playableDirector, TrackAsset track, BossEncounterDirector encounterDirector)
    {
        if (track is not EncounterCameraTrack)
        {
            return false;
        }

        playableDirector.SetGenericBinding(track, encounterDirector);
        return true;
    }

    private static void TryBindDialogueTrack(PlayableDirector playableDirector, TrackAsset track, EncounterDialoguePanel dialoguePanel)
    {
        if (track is EncounterDialogueTrack && dialoguePanel != null)
        {
            playableDirector.SetGenericBinding(track, dialoguePanel);
        }
    }

    private static bool TryGetEncounterDirector(out BossEncounterDirector encounterDirector)
    {
        encounterDirector = Object.FindFirstObjectByType<BossEncounterDirector>();
        if (encounterDirector != null)
        {
            return true;
        }

        Debug.LogWarning("Boss Timeline Setup: 활성 씬에서 BossEncounterDirector를 찾지 못했습니다.");
        return false;
    }

    private static void MarkTimelineSetupDirty(
        BossEncounterDirector encounterDirector,
        TimelineAsset introAsset,
        TimelineAsset victoryAsset,
        PlayableDirector introDirector,
        PlayableDirector victoryDirector)
    {
        EditorUtility.SetDirty(introAsset);
        EditorUtility.SetDirty(victoryAsset);
        EditorUtility.SetDirty(introDirector);
        EditorUtility.SetDirty(victoryDirector);
        EditorUtility.SetDirty(encounterDirector);
        EditorSceneManager.MarkSceneDirty(encounterDirector.gameObject.scene);
    }
}
