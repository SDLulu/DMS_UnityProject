using System.Collections.Generic;
using UnityEngine;

// 역할:
// - 씬 설치 메뉴를 아직 실행하지 않았거나 시퀀스 참조가 비어 있어도 보스 서사 연출이 기본값으로 동작하게 합니다.
// - 인스펙터에서 연결한 수동 SceneEventSequence가 있으면 이 factory는 건드리지 않습니다.

public static class BossStoryRuntimeSequenceFactory
{
    public static SceneEventSequence EnsureEntrySequence(Transform parent)
    {
        return EnsureSequence(parent, "런타임_시퀀스_보스전_등장", BuildEntrySteps());
    }

    public static SceneEventSequence EnsureP1ToP2Sequence(Transform parent)
    {
        return EnsureSequence(parent, "런타임_시퀀스_보스_P1_P2전환", BuildP1ToP2Steps());
    }

    public static SceneEventSequence EnsureP2ToP3Sequence(Transform parent)
    {
        return EnsureSequence(parent, "런타임_시퀀스_보스_P2_P3전환", BuildP2ToP3Steps());
    }

    public static SceneEventSequence EnsureFinalDefeatSequence(Transform parent, Vector3 fallbackCorePosition)
    {
        return EnsureSequence(parent, "런타임_시퀀스_보스_처치전_마지막기억", BuildFinalPreDeathSteps());
    }

    public static SceneEventSequence EnsurePostFinalDefeatSequence(Transform parent, Vector3 fallbackCorePosition)
    {
        GameObject homeCore = EnsureHomeRecoveryCore(parent, fallbackCorePosition);
        Transform homeSpawn = FindDeepChild(parent != null ? parent.root : null, "스폰_주인공집");
        return EnsureSequence(parent, "런타임_시퀀스_보스_처치후_HOME회수", BuildFinalPostDeathSteps(homeCore, homeSpawn));
    }

    private static SceneEventSequence EnsureSequence(Transform parent, string name, IEnumerable<SceneEventSequence.Step> steps)
    {
        Transform root = EnsureRuntimeRoot(parent);
        Transform existing = root.Find(name);
        if (existing == null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(root, false);
            existing = go.transform;
        }

        SceneEventSequence sequence = existing.GetComponent<SceneEventSequence>();
        if (sequence == null)
        {
            sequence = existing.gameObject.AddComponent<SceneEventSequence>();
        }

        if (sequence.StepCount == 0)
        {
            sequence.ConfigureSteps(steps);
        }

        return sequence;
    }

    private static Transform EnsureRuntimeRoot(Transform parent)
    {
        Transform searchRoot = parent != null ? parent.root : null;
        Transform existing = searchRoot != null ? FindDeepChild(searchRoot, "런타임_보스연출") : null;
        if (existing != null)
        {
            return existing;
        }

        GameObject root = new GameObject("런타임_보스연출");
        if (parent != null)
        {
            root.transform.SetParent(parent.root, false);
        }

        return root.transform;
    }

    private static GameObject EnsureHomeRecoveryCore(Transform parent, Vector3 position)
    {
        Transform root = EnsureRuntimeRoot(parent);
        Transform existing = root.Find("HOME코어_회수가능");
        if (existing == null)
        {
            GameObject go = new GameObject("HOME코어_회수가능");
            go.transform.SetParent(root, false);
            go.transform.localScale = Vector3.one * 1.35f;
            existing = go.transform;
        }

        existing.position = position;

        if (existing.GetComponent<StoryMemoryVisual>() == null)
        {
            existing.gameObject.AddComponent<StoryMemoryVisual>();
        }

        ApplyHomeCoreSprite(existing);
        existing.gameObject.SetActive(false);
        return existing.gameObject;
    }

    private static void ApplyHomeCoreSprite(Transform recoveryCore)
    {
        if (recoveryCore == null)
        {
            return;
        }

        Transform root = recoveryCore.root;
        Transform source = FindDeepChild(root, "HOME코어");
        if (source == null || source == recoveryCore)
        {
            return;
        }

        SpriteRenderer sourceRenderer = source.GetComponent<SpriteRenderer>();
        if (sourceRenderer == null || sourceRenderer.sprite == null)
        {
            return;
        }

        SpriteRenderer targetRenderer = recoveryCore.GetComponent<SpriteRenderer>();
        if (targetRenderer == null)
        {
            targetRenderer = recoveryCore.gameObject.AddComponent<SpriteRenderer>();
        }

        targetRenderer.sprite = sourceRenderer.sprite;
        targetRenderer.color = sourceRenderer.color;
        targetRenderer.sharedMaterial = sourceRenderer.sharedMaterial != null
            ? sourceRenderer.sharedMaterial
            : RuntimeSpriteUtility.UnlitSpriteMaterial;
        targetRenderer.sortingLayerName = "Effect";
        targetRenderer.sortingOrder = 30;
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
            Step(SceneEventSequence.StepType.PlayCutsceneVideo, waitForCompletion: false),
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

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
