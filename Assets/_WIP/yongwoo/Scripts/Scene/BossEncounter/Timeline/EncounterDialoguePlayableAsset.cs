using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// 역할:
// - Timeline 클립에서 재생할 대사 줄과 표시 옵션을 자산 형태로 보관합니다.
// - 컷신 트랙이 DialogueManager와 패널을 통해 대사를 재생할 수 있게 데이터만 넘깁니다.
//
// 구조 포인트:
// - 보스 조우 컷신 대사를 Timeline 안에서 선언적으로 배치할 때 보는 파일입니다.

[Serializable]
public class EncounterDialoguePlayableBehaviour : PlayableBehaviour
{
    [Tooltip("이 클립에서 표시할 대사 한 줄입니다.")]
    public EncounterDialogueLine line = new();
    [Tooltip("체크하면 Timeline 시간에 맞춰 타이핑처럼 글자가 드러납니다.")]
    public bool useTypewriter = true;
}

[Serializable]
public class EncounterDialoguePlayableAsset : PlayableAsset, ITimelineClipAsset
{
    [Tooltip("이 Timeline 클립의 대사 설정입니다.")]
    public EncounterDialoguePlayableBehaviour template = new();

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<EncounterDialoguePlayableBehaviour>.Create(graph, template);
    }
}

public class EncounterDialogueMixerBehaviour : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (playerData is not EncounterDialoguePanel dialoguePanel)
        {
            return;
        }

        int inputCount = playable.GetInputCount();
        float highestWeight = 0f;
        EncounterDialoguePlayableBehaviour activeBehaviour = null;
        double activeTime = 0d;
        double activeDuration = 0d;

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            if (inputWeight <= highestWeight)
            {
                continue;
            }

            ScriptPlayable<EncounterDialoguePlayableBehaviour> inputPlayable = (ScriptPlayable<EncounterDialoguePlayableBehaviour>)playable.GetInput(i);
            activeBehaviour = inputPlayable.GetBehaviour();
            activeTime = inputPlayable.GetTime();
            activeDuration = inputPlayable.GetDuration();
            highestWeight = inputWeight;
        }

        if (activeBehaviour == null || highestWeight <= 0f)
        {
            dialoguePanel.ClearTimelinePreview();
            return;
        }

        dialoguePanel.PreviewTimelineLine(activeBehaviour.line, activeTime, activeDuration, activeBehaviour.useTypewriter);
    }
}
