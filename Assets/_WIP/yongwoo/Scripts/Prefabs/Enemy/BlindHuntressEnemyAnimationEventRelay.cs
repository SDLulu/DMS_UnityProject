using UnityEngine;

// 역할:
// - Visual의 Animator에서 발생한 Animation Event를 루트 Combat로 전달합니다.

[DisallowMultipleComponent]
public class BlindHuntressEnemyAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private BlindHuntressEnemyCombat combat;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnValidate()
    {
        CacheReferences();
    }

    public void AnimationEvent_BeginAttackHitbox()
    {
        combat?.AnimationEvent_BeginAttackHitbox();
    }

    public void AnimationEvent_EndAttackHitbox()
    {
        combat?.AnimationEvent_EndAttackHitbox();
    }

    public void AnimationEvent_BeginDashAttackHitbox()
    {
        combat?.AnimationEvent_BeginDashAttackHitbox();
    }

    public void AnimationEvent_EndDashAttackHitbox()
    {
        combat?.AnimationEvent_EndDashAttackHitbox();
    }

    public void AnimationEvent_BeginUpAttackHitbox()
    {
        combat?.AnimationEvent_BeginUpAttackHitbox();
    }

    public void AnimationEvent_EndUpAttackHitbox()
    {
        combat?.AnimationEvent_EndUpAttackHitbox();
    }

    private void CacheReferences()
    {
        if (combat == null)
        {
            combat = GetComponentInParent<BlindHuntressEnemyCombat>();
        }
    }
}
