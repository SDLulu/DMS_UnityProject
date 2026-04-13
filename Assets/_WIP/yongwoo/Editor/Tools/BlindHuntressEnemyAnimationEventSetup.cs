using UnityEditor;
using UnityEngine;

// 역할:
// - Blind Huntress 공격 클립에 판정 시작/종료 Animation Event를 심습니다.
// - Combat의 이벤트 기반 히트박스 타이밍과 클립을 맞춰주는 편집용 유틸리티입니다.

public static class BlindHuntressEnemyAnimationEventSetup
{
    private const string AnimationFolder = "Assets/_WIP/yongwoo/Animations/BlindHuntress";

    [MenuItem("Tools/Yongwoo/Setup Blind Huntress Enemy Animation Events")]
    public static void Apply()
    {
        ApplyEvents("Attack", "AnimationEvent_BeginAttackHitbox", 0.02f, "AnimationEvent_EndAttackHitbox", 0.14f);
        ApplyEvents("DashAttack", "AnimationEvent_BeginDashAttackHitbox", 0.04f, "AnimationEvent_EndDashAttackHitbox", 0.24f);
        ApplyEvents("IdleUpAttack", "AnimationEvent_BeginUpAttackHitbox", 0.04f, "AnimationEvent_EndUpAttackHitbox", 0.16f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Blind Huntress enemy animation events applied.");
    }

    private static void ApplyEvents(string clipName, string beginFunction, float beginTime, string endFunction, float endTime)
    {
        string clipPath = $"{AnimationFolder}/{clipName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            Debug.LogWarning($"Animation clip not found: {clipPath}");
            return;
        }

        AnimationEvent[] events =
        {
            new AnimationEvent
            {
                functionName = beginFunction,
                time = beginTime
            },
            new AnimationEvent
            {
                functionName = endFunction,
                time = endTime
            }
        };

        AnimationUtility.SetAnimationEvents(clip, events);
        EditorUtility.SetDirty(clip);
    }
}
