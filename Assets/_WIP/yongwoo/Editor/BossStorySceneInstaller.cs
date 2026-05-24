using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

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
            BuildEntrySteps());
        SceneEventSequence p1ToP2Sequence = EnsureSequence(
            storyRoot,
            "시퀀스_보스_P1_P2전환",
            BuildP1ToP2Steps());
        SceneEventSequence p2ToP3Sequence = EnsureSequence(
            storyRoot,
            "시퀀스_보스_P2_P3전환",
            BuildP2ToP3Steps());
        GameObject homeCore = EnsureHomeRecoveryCore(bossScene.transform);
        SceneEventSequence finalSequence = EnsureSequence(
            storyRoot,
            "시퀀스_보스_처치후_HOME회수",
            BuildFinalSteps(homeCore));

        EnsureMemoryFragment(
            bossScene.transform,
            storyRoot,
            "기억조각_01_작은집",
            "시퀀스_기억조각_01",
            new Vector3(-3.0f, 1.0f, 0f),
            "[기억 조각 01]\n작은 집");
        EnsureMemoryFragment(
            bossScene.transform,
            storyRoot,
            "기억조각_02_문",
            "시퀀스_기억조각_02",
            new Vector3(3.0f, 1.0f, 0f),
            "[기억 조각 02]\n열린 문");

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
            SetObjectField(rootBoss, "finalDefeatSequence", finalSequence);
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
        string logText)
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
            BuildMemoryFragmentSteps(logText));

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

            BossBattleArena arena = Object.FindFirstObjectByType<BossBattleArena>(FindObjectsInactive.Include);
            existing.position = arena != null ? arena.ArenaBounds.center : bossScene.position;
            existing.localScale = Vector3.one * 1.35f;
            existing.gameObject.SetActive(false);
        }

        if (existing.GetComponent<StoryMemoryVisual>() == null)
        {
            Undo.AddComponent<StoryMemoryVisual>(existing.gameObject);
        }

        TutorialMarker marker = existing.GetComponent<TutorialMarker>();
        if (marker == null)
        {
            marker = Undo.AddComponent<TutorialMarker>(existing.gameObject);
            marker.Configure(TutorialMarker.MarkerType.Interactable, 0.45f);
        }

        return existing.gameObject;
    }

    private static SceneEventSequence EnsureSequence(Transform parent, string name, IEnumerable<SceneEventSequence.Step> defaultSteps)
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

        if (sequence.EditorStepCount == 0)
        {
            sequence.EditorSetSteps(defaultSteps);
        }

        return sequence;
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

    private static IEnumerable<SceneEventSequence.Step> BuildFinalSteps(GameObject homeCore)
    {
        return new[]
        {
            Step(SceneEventSequence.StepType.LockPlayer),
            Step(SceneEventSequence.StepType.FreezeTime),
            Step(SceneEventSequence.StepType.SetObjectActive, targetObject: homeCore, active: true),
            Step(SceneEventSequence.StepType.CameraShake, duration: 0.22f, strength: 0.12f),
            Step(SceneEventSequence.StepType.ShowSystemLog, message: "[인격 흔적 소거]\n[회수 가능]", duration: 1.1f),
            Step(SceneEventSequence.StepType.ShowSystemLog, message: "[HOME 파일 접근 가능]\n[내용 분석 중...]", duration: 1.25f),
            Step(SceneEventSequence.StepType.ShowSystemLog, message: "거주권 키 데이터 없음\n자산 데이터 없음\n보안 암호 없음\n\n정서 기억 백업 확인", duration: 2.1f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "브로커", message: "뭐야.", duration: 0.85f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "브로커", message: "그냥 가족 기록이잖아.", duration: 1.15f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "주인공", message: "그래서 그렇게 지킨 건가.", duration: 1.25f),
            Step(SceneEventSequence.StepType.ShowCommsLine, speaker: "브로커", message: "추억은 싸. 암호는 비싸고.", duration: 1.3f),
            Step(SceneEventSequence.StepType.PlayCutsceneVideo, duration: 0f),
            Step(SceneEventSequence.StepType.HideComms),
            Step(SceneEventSequence.StepType.HideSystemLog),
            Step(SceneEventSequence.StepType.UnfreezeTime),
            Step(SceneEventSequence.StepType.FadeOut, duration: 0.6f),
        };
    }

    private static IEnumerable<SceneEventSequence.Step> BuildMemoryFragmentSteps(string logText)
    {
        return new[]
        {
            Step(SceneEventSequence.StepType.LockPlayer),
            Step(SceneEventSequence.StepType.ShowSystemLog, message: logText, duration: 0.8f),
            Step(SceneEventSequence.StepType.PlayCutsceneVideo),
            Step(SceneEventSequence.StepType.HideSystemLog),
            Step(SceneEventSequence.StepType.UnlockPlayer),
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
        bool active = false)
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
            active = active,
            skippable = true,
        };
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
