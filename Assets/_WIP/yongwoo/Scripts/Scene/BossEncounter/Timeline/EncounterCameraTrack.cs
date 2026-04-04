using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// 역할:
// - EncounterCameraPlayableAsset 클립을 Timeline 트랙으로 노출합니다.
// - BossEncounterDirector를 바인딩 대상으로 사용해 컷신 카메라 프레임을 전달합니다.
//
// 구조 포인트:
// - Track은 구조 선언, 실제 프레임 계산은 PlayableAsset/Mixer가 맡습니다.

public enum EncounterCameraFrameType
{
    Wide,
    Boss,
    Combat
}

[TrackClipType(typeof(EncounterCameraPlayableAsset))]
[TrackBindingType(typeof(BossEncounterDirector))]
[TrackColor(0.18f, 0.72f, 0.92f)]
public class EncounterCameraTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<EncounterCameraMixerBehaviour>.Create(graph, inputCount);
    }
}
