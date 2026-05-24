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
        GameObject homeCore = EnsureHomeRecoveryCore(parent, fallbackCorePosition);
        return EnsureSequence(parent, "런타임_시퀀스_보스_처치후_HOME회수", BuildFinalSteps(homeCore));
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
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 1.35f;
            existing = go.transform;
        }

        if (existing.GetComponent<StoryMemoryVisual>() == null)
        {
            existing.gameObject.AddComponent<StoryMemoryVisual>();
        }

        existing.gameObject.SetActive(false);
        return existing.gameObject;
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
            Step(SceneEventSequence.StepType.HideComms),
            Step(SceneEventSequence.StepType.HideSystemLog),
            Step(SceneEventSequence.StepType.UnfreezeTime),
            Step(SceneEventSequence.StepType.FadeOut, duration: 0.6f),
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
