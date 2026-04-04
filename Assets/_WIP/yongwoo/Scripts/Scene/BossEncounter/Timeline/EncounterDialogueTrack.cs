using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// 역할:
// - EncounterDialoguePlayableAsset 클립을 Timeline 트랙으로 묶습니다.
// - 대화 출력 대상을 Director/Panel 쪽에 바인딩하기 위한 선언 계층입니다.
//
// 구조 포인트:
// - 트랙은 배선, 실제 재생은 PlayableAsset과 DialogueManager가 담당합니다.

[TrackClipType(typeof(EncounterDialoguePlayableAsset))]
[TrackBindingType(typeof(EncounterDialoguePanel))]
[TrackColor(0.96f, 0.54f, 0.28f)]
public class EncounterDialogueTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<EncounterDialogueMixerBehaviour>.Create(graph, inputCount);
    }
}
