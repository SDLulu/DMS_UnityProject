using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// 역할:
// - Timeline 클립 한 개가 사용할 카메라 프레임 종류와 오프셋을 정의합니다.
// - 믹서에서 여러 클립의 가중치를 받아 실제 컷신 카메라 위치를 블렌딩합니다.
//
// 구조 포인트:
// - 컷신 카메라 연출을 코드 상태 기계와 분리할 때 쓰는 Timeline 계층입니다.

[Serializable]
public class EncounterCameraPlayableBehaviour : PlayableBehaviour
{
    [Tooltip("이 클립이 사용할 기본 카메라 프레임입니다.")]
    public EncounterCameraFrameType frameType = EncounterCameraFrameType.Wide;
    [Tooltip("기본 프레임에서 추가로 보정할 X/Y 오프셋입니다.")]
    public Vector2 offset;
}

[Serializable]
public class EncounterCameraPlayableAsset : PlayableAsset, ITimelineClipAsset
{
    [Tooltip("이 클립이 재생하는 카메라 샷 설정입니다.")]
    public EncounterCameraPlayableBehaviour template = new();

    public ClipCaps clipCaps => ClipCaps.Blending;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<EncounterCameraPlayableBehaviour>.Create(graph, template);
    }
}

public class EncounterCameraMixerBehaviour : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (playerData is not BossEncounterDirector director || !director.TryGetTimelineCameraTransform(out Transform cameraTransform))
        {
            return;
        }

        int inputCount = playable.GetInputCount();
        float totalWeight = 0f;
        Vector3 blendedPosition = Vector3.zero;

        for (int i = 0; i < inputCount; i++)
        {
            float inputWeight = playable.GetInputWeight(i);
            if (inputWeight <= 0f)
            {
                continue;
            }

            ScriptPlayable<EncounterCameraPlayableBehaviour> inputPlayable = (ScriptPlayable<EncounterCameraPlayableBehaviour>)playable.GetInput(i);
            EncounterCameraPlayableBehaviour behaviour = inputPlayable.GetBehaviour();
            Vector3 targetPosition = director.GetTimelineCameraFramePosition(behaviour.frameType, behaviour.offset, cameraTransform.position.z);
            blendedPosition += targetPosition * inputWeight;
            totalWeight += inputWeight;
        }

        if (totalWeight <= 0f)
        {
            return;
        }

        if (totalWeight < 1f)
        {
            blendedPosition += cameraTransform.position * (1f - totalWeight);
        }

        cameraTransform.position = blendedPosition;
    }
}
