using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// 역할:
// - 영상 파일은 만들지 않고, 보스 스테이지의 비디오 슬롯/기억조각/보스 내러티브 시퀀스 연결만 씬에 설치합니다.
// - 기존 수동 배치값은 유지하고, 없는 오브젝트/참조만 보강합니다.

public static class BossStorySceneInstaller
{
    private const string MenuPath = "Tools/Yongwoo/Boss/Install Story Sequence Hooks";

    [MenuItem(MenuPath)]
    public static void InstallStorySequenceHooks()
    {
        GameObject bossScene = GameObject.Find("보스씬");
        if (bossScene == null)
        {
            Debug.LogWarning("[BossStorySceneInstaller] 보스씬 루트를 찾지 못했습니다.");
            return;
        }

        CutsceneVideoPanel videoPanel = EnsureCutsceneVideoPanel();
        Transform storyRoot = EnsureChild(bossScene.transform, "보스연출");

        SceneEventSequence entrySequence = EnsureSequence(
            storyRoot,
            "시퀀스_보스전_등장",
            BuildEntrySteps(),
            replaceExistingSteps: true);
        SceneEventSequence p1ToP2Sequence = EnsureSequence(
            storyRoot,
            "시퀀스_보스_P1_P2전환",
            BuildP1ToP2Steps(),
            replaceExistingSteps: true);
        SceneEventSequence p2ToP3Sequence = EnsureSequence(
            storyRoot,
            "시퀀스_보스_P2_P3전환",
            BuildP2ToP3Steps(),
            replaceExistingSteps: true);
        GameObject homeCore = EnsureHomeRecoveryCore(bossScene.transform);
        Transform homeSpawn = FindDeepChild("스폰_주인공집");
        SceneEventSequence finalPreDeathSequence = EnsureSequence(
            storyRoot,
            "시퀀스_보스_처치전_마지막기억",
            BuildFinalPreDeathSteps(),
            replaceExistingSteps: true);
        SceneEventSequence finalPostDeathSequence = EnsureSequence(
            storyRoot,
            "시퀀스_보스_처치후_HOME회수",
            BuildFinalPostDeathSteps(homeCore, homeSpawn),
            replaceExistingSteps: true);

        EnsureMemoryFragment(
            bossScene.transform,
            storyRoot,
            "기억조각_01_작은집",
            "시퀀스_기억조각_01",
            new Vector3(-3.0f, 1.0f, 0f),
            "[기억 조각 01]\n작은 집",
            BuildMemoryFragment01VideoDialogue(),
            BuildMemoryFragment01AfterDialogue());
        EnsureMemoryFragment(
            bossScene.transform,
            storyRoot,
            "기억조각_02_문",
            "시퀀스_기억조각_02",
            new Vector3(3.0f, 1.0f, 0f),
            "[기억 조각 02]\n열린 문",
            BuildMemoryFragment02VideoDialogue(),
            BuildMemoryFragment02AfterDialogue());

        BossBattleEntryTrigger entryTrigger = Object.FindFirstObjectByType<BossBattleEntryTrigger>(FindObjectsInactive.Include);
        if (entryTrigger != null)
        {
            SetObjectField(entryTrigger, "beforeBattleSequence", entrySequence);
        }

        BossPhaseController rootBoss = FindRootBoss();
        if (rootBoss != null)
        {
            SetObjectField(rootBoss, "p1ToP2Sequence", p1ToP2Sequence);
            SetObjectField(rootBoss, "p2ToP3Sequence", p2ToP3Sequence);
            SetObjectField(rootBoss, "finalDefeatSequence", finalPreDeathSequence);
            SetObjectField(rootBoss, "postFinalDefeatSequence", finalPostDeathSequence);
        }

        SceneEventSequence[] sequences = storyRoot.GetComponentsInChildren<SceneEventSequence>(includeInactive: true);
        for (int i = 0; i < sequences.Length; i++)
        {
            SetObjectField(sequences[i], "cutsceneVideoPanel", videoPanel);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[BossStorySceneInstaller] 보스 스토리 시퀀스 훅 설치 완료. 영상 파일은 각 PlayCutsceneVideo step의 VideoClip 슬롯에 연결하세요.", bossScene);
    }

    private static CutsceneVideoPanel EnsureCutsceneVideoPanel()
    {
        CutsceneVideoPanel existing = Object.FindFirstObjectByType<CutsceneVideoPanel>(FindObjectsInactive.Include);
        if (existing != null)
        {
            return existing;
        }

        Canvas canvas = FindHudCanvas();
        GameObject host = canvas != null
            ? canvas.gameObject
            : new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        if (canvas == null)
        {
            Undo.RegisterCreatedObjectUndo(host, "Create HUD for cutscene video");
            canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        CutsceneVideoPanel panel = Undo.AddComponent<CutsceneVideoPanel>(host);
        EditorUtility.SetDirty(host);
        return panel;
    }

    private static void EnsureMemoryFragment(
        Transform bossScene,
        Transform storyRoot,
        string objectName,
        string sequenceName,
        Vector3 localOffsetFromEntry,
        string logText,
        IEnumerable<SceneEventSequence.Step> videoDialogueSteps,
        IEnumerable<SceneEventSequence.Step> afterVideoDialogueSteps)
    {
        Transform existing = bossScene.Find(objectName);
        bool created = false;
        if (existing == null)
        {
            GameObject go = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(go, "Create memory fragment");
            go.transform.SetParent(bossScene, false);
            existing = go.transform;
            created = true;
        }

        if (created)
        {
            Transform entry = bossScene.Find("트리거_보스전입장");
            existing.position = entry != null ? entry.position + localOffsetFromEntry : bossScene.position + localOffsetFromEntry;
        }

        SpriteRenderer renderer = existing.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = Undo.AddComponent<SpriteRenderer>(existing.gameObject);
            renderer.sprite = RuntimeSpriteUtility.RingSprite;
            renderer.sortingLayerName = "Effect";
            renderer.sortingOrder = 28;
            renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
            renderer.color = new Color(0.25f, 0.95f, 1f, 0.72f);
            existing.localScale = Vector3.one * 0.9f;
        }

        if (existing.GetComponent<StoryMemoryVisual>() == null)
        {
            Undo.AddComponent<StoryMemoryVisual>(existing.gameObject);
        }

        CircleCollider2D collider = existing.GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = Undo.AddComponent<CircleCollider2D>(existing.gameObject);
            collider.isTrigger = true;
            collider.radius = 0.9f;
        }

        TutorialMarker marker = existing.GetComponent<TutorialMarker>();
        if (marker == null)
        {
            marker = Undo.AddComponent<TutorialMarker>(existing.gameObject);
            marker.Configure(TutorialMarker.MarkerType.Interactable, 0.35f);
        }

        SceneEventSequence sequence = EnsureSequence(
            storyRoot,
            sequenceName,
            BuildMemoryFragmentSteps(logText, videoDialogueSteps, afterVideoDialogueSteps),
            replaceExistingSteps: true);

        Interactable interactable = existing.GetComponent<Interactable>();
        if (interactable == null)
        {
            interactable = Undo.AddComponent<Interactable>(existing.gameObject);
        }

        SetStringField(interactable, "promptText", "E : 기억 조각 재생");
        SetObjectField(interactable, "onInteractSequence", sequence);
    }

    private static GameObject EnsureHomeRecoveryCore(Transform bossScene)
    {
        Transform existing = bossScene.Find("HOME코어_회수가능");
        if (existing == null)
        {
            GameObject go = new GameObject("HOME코어_회수가능");
            Undo.RegisterCreatedObjectUndo(go, "Create HOME recovery core");
            go.transform.SetParent(bossScene, false);
            existing = go.transform;
            existing.localScale = Vector3.one * 1.35f;
        }

        existing.position = ResolveHomeRecoveryCorePosition(bossScene);
        existing.gameObject.SetActive(false);

        if (existing.GetComponent<StoryMemoryVisual>() == null)
        {
            Undo.AddComponent<StoryMemoryVisual>(existing.gameObject);
        }

        ApplyHomeCoreSprite(existing);

        TutorialMarker marker = existing.GetComponent<TutorialMarker>();
        if (marker == null)
        {
            marker = Undo.AddComponent<TutorialMarker>(existing.gameObject);
            marker.Configure(TutorialMarker.MarkerType.Interactable, 0.45f);
        }

        return existing.gameObject;
    }

    private static Vector3 ResolveHomeRecoveryCorePosition(Transform bossScene)
    {
        BossPhaseController rootBoss = FindRootBoss();
        if (rootBoss != null)
        {
            return rootBoss.transform.position + Vector3.up * 0.85f;
        }

        BossBattleArena arena = Object.FindFirstObjectByType<BossBattleArena>(FindObjectsInactive.Include);
        if (arena != null)
        {
            return arena.ArenaBounds.center + Vector3.up * 0.6f;
        }

        return bossScene != null ? bossScene.position : Vector3.zero;
    }

    private static void ApplyHomeCoreSprite(Transform recoveryCore)
    {
        if (recoveryCore == null)
        {
            return;
        }

        SpriteRenderer sourceRenderer = null;
        SpriteRenderer[] renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer candidate = renderers[i];
            if (candidate == null || candidate.transform == recoveryCore)
            {
                continue;
            }

            if (candidate.name == "HOME코어" && candidate.sprite != null)
            {
                sourceRenderer = candidate;
                break;
            }
        }

        if (sourceRenderer == null)
        {
            return;
        }

        SpriteRenderer targetRenderer = recoveryCore.GetComponent<SpriteRenderer>();
        if (targetRenderer == null)
        {
            targetRenderer = Undo.AddComponent<SpriteRenderer>(recoveryCore.gameObject);
        }

        targetRenderer.sprite = sourceRenderer.sprite;
        targetRenderer.color = sourceRenderer.color;
        targetRenderer.sharedMaterial = sourceRenderer.sharedMaterial != null
            ? sourceRenderer.sharedMaterial
            : RuntimeSpriteUtility.UnlitSpriteMaterial;
        targetRenderer.sortingLayerName = "Effect";
        targetRenderer.sortingOrder = 30;
        EditorUtility.SetDirty(targetRenderer);
    }

    private static SceneEventSequence EnsureSequence(
        Transform parent,
        string name,
        IEnumerable<SceneEventSequence.Step> defaultSteps,
        bool replaceExistingSteps = false)
    {
        Transform existing = parent.Find(name);
        if (existing == null)
        {
            GameObject go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create boss story sequence");
            go.transform.SetParent(parent, false);
            existing = go.transform;
        }

        SceneEventSequence sequence = existing.GetComponent<SceneEventSequence>();
        if (sequence == null)
        {
            sequence = Undo.AddComponent<SceneEventSequence>(existing.gameObject);
        }

        if (replaceExistingSteps || sequence.EditorStepCount == 0)
        {
            VideoClip preservedClip = ExtractFirstVideoClip(sequence);
            List<SceneEventSequence.Step> steps = new(defaultSteps);
            RestoreFirstVideoClip(steps, preservedClip);
            sequence.EditorSetSteps(steps);
        }

        return sequence;
    }

    private static VideoClip ExtractFirstVideoClip(SceneEventSequence sequence)
    {
        if (sequence == null || sequence.EditorStepCount == 0)
        {
            return null;
        }

        SerializedObject serialized = new SerializedObject(sequence);
        SerializedProperty steps = serialized.FindProperty("steps");
        if (steps == null || !steps.isArray)
        {
            return null;
        }

        for (int i = 0; i < steps.arraySize; i++)
        {
            SerializedProperty item = steps.GetArrayElementAtIndex(i);
            SerializedProperty type = item.FindPropertyRelative("type");
            SerializedProperty clip = item.FindPropertyRelative("videoClip");
            if (type != null
                && clip != null
                && type.enumValueIndex == (int)SceneEventSequence.StepType.PlayCutsceneVideo
                && clip.objectReferenceValue is VideoClip videoClip)
            {
                return videoClip;
            }
        }

        return null;
    }

    private static void RestoreFirstVideoClip(List<SceneEventSequence.Step> steps, VideoClip preservedClip)
    {
        if (preservedClip == null || steps == null)
        {
            return;
        }

        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] != null && steps[i].type == SceneEventSequence.StepType.PlayCutsceneVideo)
            {
                steps[i].videoClip = preservedClip;
                return;
            }
        }
    }

    private static IEnumerable<SceneEventSequence.Step> BuildEntrySteps()
    {
        return new[]
        {
            Step(SceneEventSequence.StepType.LockPlayer),
            Step(SceneEventSequence.StepType.FadeOut, duration: 0.35f),
            Step(SceneEventSequence.StepType.ShowSystemLog, message: "[심층 클리어]\n[잔류 인격 반응 고정]", duration: 1.1f),
            Step(SceneEventSequence.StepType.GlitchPulse, duration: 0.35f, glitch: 0.45f),
            Step(SceneEventSequence.StepType.FadeIn, duration: 0.45f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "잔류 인격", message: "그건 팔 물건이 아니야.", duration: 1.45f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "브로커", message: "잔류 인격이다. 지워.", duration: 1.25f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "주인공", message: "대상자 047인가.", duration: 1.15f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "잔류 인격", message: "이름으로 부르지 마.", duration: 1.25f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "잔류 인격", message: "가져갈 거면, 그냥 데이터라고 불러.", duration: 1.55f),
            Step(SceneEventSequence.StepType.HideComms),
            Step(SceneEventSequence.StepType.HideSystemLog),
            Step(SceneEventSequence.StepType.UnlockPlayer),
        };
    }

    private static IEnumerable<SceneEventSequence.Step> BuildP1ToP2Steps()
    {
        return new[]
        {
            Step(SceneEventSequence.StepType.LockPlayer),
            Step(SceneEventSequence.StepType.FreezeTime),
            Step(SceneEventSequence.StepType.GlitchFade, duration: 0.22f, glitch: 0.38f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "잔류 인격", message: "나는 집을 남기려 했을 뿐이야.", duration: 1.55f),
            Step(SceneEventSequence.StepType.GlitchPulse, duration: 0.32f, glitch: 0.7f),
            Step(SceneEventSequence.StepType.HideComms),
            Step(SceneEventSequence.StepType.UnfreezeTime),
            Step(SceneEventSequence.StepType.UnlockPlayer),
        };
    }

    private static IEnumerable<SceneEventSequence.Step> BuildP2ToP3Steps()
    {
        return new[]
        {
            Step(SceneEventSequence.StepType.LockPlayer),
            Step(SceneEventSequence.StepType.FreezeTime),
            Step(SceneEventSequence.StepType.GlitchFade, duration: 0.18f, glitch: 0.55f),
            Step(SceneEventSequence.StepType.ShowSystemLog, message: "[인격 흔적 재분리]", duration: 0.85f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "잔류 인격", message: "나눠도... 남는 게 있어.", duration: 1.45f),
            Step(SceneEventSequence.StepType.GlitchPulse, duration: 0.4f, glitch: 0.85f),
            Step(SceneEventSequence.StepType.HideComms),
            Step(SceneEventSequence.StepType.HideSystemLog),
            Step(SceneEventSequence.StepType.UnfreezeTime),
            Step(SceneEventSequence.StepType.UnlockPlayer),
        };
    }

    private static IEnumerable<SceneEventSequence.Step> BuildFinalPreDeathSteps()
    {
        return new[]
        {
            Step(SceneEventSequence.StepType.LockPlayer),
            Step(SceneEventSequence.StepType.FreezeTime),
            Step(SceneEventSequence.StepType.WhiteFlash, duration: 0.18f, strength: 0.08f),
            Step(SceneEventSequence.StepType.PlayCutsceneVideo, duration: 0f, waitForCompletion: false),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "잔류 인격", message: "문을 열어둔다고 했지.", duration: 1.35f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "잔류 인격", message: "내가 너무 늦었다.", duration: 1.25f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "잔류 인격", message: "미안하다. 아빠가... 집을 잃어버렸다.", duration: 1.7f),
            Step(SceneEventSequence.StepType.ShowSystemLog, message: "[잔류 인격 신호 감쇠]", duration: 0.75f),
            Step(SceneEventSequence.StepType.WaitForCutsceneVideo),
            Step(SceneEventSequence.StepType.HideComms),
            Step(SceneEventSequence.StepType.HideSystemLog),
            Step(SceneEventSequence.StepType.UnfreezeTime),
        };
    }

    private static IEnumerable<SceneEventSequence.Step> BuildFinalPostDeathSteps(GameObject homeCore, Transform homeSpawn)
    {
        return new[]
        {
            Step(SceneEventSequence.StepType.LockPlayer),
            Step(SceneEventSequence.StepType.FreezeTime),
            Step(SceneEventSequence.StepType.GlitchFade, duration: 0.28f, glitch: 0.62f),
            Step(SceneEventSequence.StepType.GlitchPulse, duration: 0.42f, glitch: 0.9f),
            Step(SceneEventSequence.StepType.RiseObject, targetObject: homeCore, duration: 0.9f, strength: 1.2f),
            Step(SceneEventSequence.StepType.CameraShake, duration: 0.22f, strength: 0.12f),
            Step(SceneEventSequence.StepType.ShowSystemLog, message: "[인격 흔적 소거]\n[HOME 코어 부상]\n[회수 가능]", duration: 1.1f),
            Step(SceneEventSequence.StepType.ShowSystemLog, message: "[HOME 파일 접근 가능]\n[내용 분석 중...]", duration: 1.25f),
            Step(SceneEventSequence.StepType.ShowSystemLog, message: "거주권 키 데이터 없음\n자산 데이터 없음\n보안 암호 없음\n\n정서 기억 백업 확인", duration: 2.1f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "브로커", message: "뭐야.", duration: 0.85f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "브로커", message: "그냥 가족 기록이잖아.", duration: 1.15f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "주인공", message: "그래서 그렇게 지킨 건가.", duration: 1.25f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "브로커", message: "추억은 싸. 암호는 비싸고.", duration: 1.3f),
            Step(SceneEventSequence.StepType.ShowSystemLog, message: "[HOME 파일 회수 완료]", duration: 0.9f),
            Step(SceneEventSequence.StepType.FadeOut, duration: 0.65f),
            Step(SceneEventSequence.StepType.ExitBossArena),
            Step(SceneEventSequence.StepType.TeleportPlayer, targetTransform: homeSpawn),
            Step(SceneEventSequence.StepType.SnapCamera),
            Step(SceneEventSequence.StepType.GlitchFade, duration: 0.18f, glitch: 0f),
            Step(SceneEventSequence.StepType.FadeIn, duration: 0.55f),
            Step(SceneEventSequence.StepType.ShowSystemLog, message: "[파일 전송 요청]\n전송 대상: 브로커\n파일명: HOME", duration: 1.35f),
            Step(SceneEventSequence.StepType.ShowSystemLog, message: "[HOME 전송 중...]\n[전송 완료]", duration: 1.2f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "브로커", message: "끝났나?", duration: 0.9f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "주인공", message: "끝났어.", duration: 0.85f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "브로커", message: "쓸모없는 파일이었군.", duration: 1.1f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "브로커", message: "보상은 절반만 지급한다.", duration: 1.1f),
            Step(SceneEventSequence.StepType.ShowSystemLog, message: "[보상 지급: 60C]\n[채무 잔액: 83,360C]", duration: 1.25f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "주인공", message: "사람 하나의 집값이 60C인가.", duration: 1.45f),
            Step(SceneEventSequence.StepType.ShowSystemLog, message: "[미전송 데이터: 1KB]\nvoice_001: \"아빠, 문 열어둘게.\"", duration: 1.55f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "브로커", message: "전송 누락은 없겠지?", duration: 1.05f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "주인공", message: "없어.", duration: 0.9f),
            Step(SceneEventSequence.StepType.HideComms),
            Step(SceneEventSequence.StepType.Delay, duration: 0.45f),
            Step(SceneEventSequence.StepType.FadeOut, duration: 0.8f),
            Step(SceneEventSequence.StepType.ShowSystemLog, message: "END", duration: 0.5f),
            Step(SceneEventSequence.StepType.WaitForInput, inputWaitType: SceneEventSequence.InputWaitType.Space),
            Step(SceneEventSequence.StepType.HideSystemLog),
            Step(SceneEventSequence.StepType.UnfreezeTime),
            Step(SceneEventSequence.StepType.LoadTitleScene),
        };
    }

    private static IEnumerable<SceneEventSequence.Step> BuildMemoryFragmentSteps(
        string logText,
        IEnumerable<SceneEventSequence.Step> videoDialogueSteps,
        IEnumerable<SceneEventSequence.Step> afterVideoDialogueSteps)
    {
        List<SceneEventSequence.Step> steps = new()
        {
            Step(SceneEventSequence.StepType.LockPlayer),
            Step(SceneEventSequence.StepType.ShowSystemLog, message: logText, duration: 0.8f),
            Step(SceneEventSequence.StepType.PlayCutsceneVideo, waitForCompletion: false),
            Step(SceneEventSequence.StepType.HideSystemLog),
        };

        if (videoDialogueSteps != null)
        {
            steps.AddRange(videoDialogueSteps);
        }

        steps.AddRange(new[]
        {
            Step(SceneEventSequence.StepType.WaitForCutsceneVideo),
        });

        if (afterVideoDialogueSteps != null)
        {
            steps.AddRange(afterVideoDialogueSteps);
        }

        steps.AddRange(new[]
        {
            Step(SceneEventSequence.StepType.HideComms),
            Step(SceneEventSequence.StepType.UnlockPlayer),
        });

        return steps;
    }

    private static IEnumerable<SceneEventSequence.Step> BuildMemoryFragment01VideoDialogue()
    {
        return new[]
        {
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "아이", message: "저 불빛들, 전부 집이야?", duration: 1.25f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "아버지", message: "그래. 언젠가 우리 창문에도 저런 빛이 켜질 거야.", duration: 1.75f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "아이", message: "그럼 문 열어두면 아빠가 바로 찾겠네.", duration: 1.55f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "아버지", message: "어디 있어도 돌아올 수 있게. 그게 집이니까.", duration: 1.75f),
        };
    }

    private static IEnumerable<SceneEventSequence.Step> BuildMemoryFragment01AfterDialogue()
    {
        return new[]
        {
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "주인공", message: "...가족 기록?", duration: 1.0f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "브로커", message: "잡음이다. 무시해.", duration: 1.15f),
        };
    }

    private static IEnumerable<SceneEventSequence.Step> BuildMemoryFragment02VideoDialogue()
    {
        return new[]
        {
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "아이", message: "아빠, 도시는 왜 우리를 싫어해?", duration: 1.35f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "아버지", message: "싫어하는 게 아니야.", duration: 1.1f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "아버지", message: "우리가 돈이 없는 거지.", duration: 1.15f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "아이", message: "그럼 돈이 있으면 집에 갈 수 있어?", duration: 1.45f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "아버지", message: "돈이 있으면... 문 앞까진 갈 수 있지.", duration: 1.55f),
        };
    }

    private static IEnumerable<SceneEventSequence.Step> BuildMemoryFragment02AfterDialogue()
    {
        return new[]
        {
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "주인공", message: "...", duration: 0.8f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "브로커", message: "거의 다 왔다. 감상은 나와서 해.", duration: 1.35f),
        };
    }

    private static SceneEventSequence.Step Step(
        SceneEventSequence.StepType type,
        string speaker = null,
        string message = null,
        float duration = 0f,
        float glitch = 0.6f,
        float strength = 0.15f,
        GameObject targetObject = null,
        Transform targetTransform = null,
        bool active = false,
        bool waitForCompletion = true,
        SceneEventSequence.InputWaitType inputWaitType = SceneEventSequence.InputWaitType.AnyKey)
    {
        return new SceneEventSequence.Step
        {
            type = type,
            speaker = speaker,
            message = message,
            duration = duration,
            glitchIntensity = glitch,
            strength = strength,
            targetObject = targetObject,
            targetTransform = targetTransform,
            active = active,
            waitForCompletion = waitForCompletion,
            inputWaitType = inputWaitType,
            skippable = true,
        };
    }

    private static Transform FindDeepChild(string name)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null && candidate.name == name)
            {
                return candidate;
            }
        }

        return null;
    }

    private static Transform EnsureChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child;
        }

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create boss story root");
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static BossPhaseController FindRootBoss()
    {
        BossPhaseController[] controllers = Object.FindObjectsByType<BossPhaseController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            BossPhaseController controller = controllers[i];
            if (controller != null && controller.IsRootController)
            {
                return controller;
            }
        }

        return controllers.Length > 0 ? controllers[0] : null;
    }

    private static Canvas FindHudCanvas()
    {
        GameObject hud = GameObject.Find("HUD");
        if (hud != null && hud.TryGetComponent(out Canvas canvas))
        {
            return canvas;
        }

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas candidate = canvases[i];
            if (candidate != null && candidate.isRootCanvas)
            {
                return candidate;
            }
        }

        return null;
    }

    private static void SetObjectField(Object target, string fieldName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogWarning($"[BossStorySceneInstaller] {target.name}.{fieldName} 필드를 찾지 못했습니다.", target);
            return;
        }

        property.objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private static void SetStringField(Object target, string fieldName, string value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogWarning($"[BossStorySceneInstaller] {target.name}.{fieldName} 필드를 찾지 못했습니다.", target);
            return;
        }

        property.stringValue = value;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }
}
