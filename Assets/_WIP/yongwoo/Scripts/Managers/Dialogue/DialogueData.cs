using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum DialoguePortraitSide
{
    Left,
    Right
}

[Serializable]
public class DialogueLineData
{
    [Tooltip("대사를 말하는 인물 이름입니다.")]
    public string speakerName;
    [Tooltip("화면에 출력할 본문입니다.")]
    [TextArea(2, 5)] public string text;
    [Tooltip("해당 줄에서 보여줄 초상 이미지입니다. 비워두면 초상을 숨깁니다.")]
    public Sprite portraitSprite;
    [Tooltip("초상을 왼쪽에 둘지 오른쪽에 둘지 정합니다.")]
    public DialoguePortraitSide portraitSide = DialoguePortraitSide.Left;
}

[CreateAssetMenu(
    fileName = "DialogueSequence",
    menuName = "DMS/Dialogue/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    [Header("Playback")]
    [Tooltip("대화 중 플레이어 이동과 공격을 잠글지 정합니다.")]
    [SerializeField] private bool lockPlayerControl = true;
    [Tooltip("대화 중 카메라 팔로우를 잠시 꺼둘지 정합니다.")]
    [SerializeField] private bool disableCameraFollow;
    [Tooltip("대화를 스킵할 수 있게 둘지 정합니다.")]
    [SerializeField] private bool allowSkip = true;

    [Header("Lines")]
    [SerializeField] private List<DialogueLineData> lines = new();

    public bool LockPlayerControl => lockPlayerControl;
    public bool DisableCameraFollow => disableCameraFollow;
    public bool AllowSkip => allowSkip;
    public IReadOnlyList<DialogueLineData> Lines => lines;
}

public sealed class DialoguePlaybackContext
{
    public bool? lockPlayerControlOverride;
    public bool? disableCameraFollowOverride;
    public bool? allowSkipOverride;
    public bool manageInputMode = true;
    public Action onStarted;
    public Action onCompleted;
}
