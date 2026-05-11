using UnityEngine;

// 역할:
// - 플레이어 Visual Animator의 Animation Event를 전투 계층으로 전달합니다.
// - 현재는 검 판정 시작/종료 타이밍만 전달합니다.

[DisallowMultipleComponent]
public class PlayerAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private SimplePlayerCombat combat;

    private void Awake()
    {
        if (combat == null)
        {
            combat = GetComponentInParent<SimplePlayerCombat>();
        }
    }

    public void AnimationEvent_BeginSwordHitbox()
    {
        combat?.AnimationEvent_BeginSwordHitbox();
    }

    public void AnimationEvent_EndSwordHitbox()
    {
        combat?.AnimationEvent_EndSwordHitbox();
    }
}
