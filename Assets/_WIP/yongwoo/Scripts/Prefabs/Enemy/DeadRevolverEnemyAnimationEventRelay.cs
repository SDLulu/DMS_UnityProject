using UnityEngine;

// 역할:
// - Visual에 붙은 Animator의 Animation Event를 루트 적 컨트롤러로 전달합니다.
// - 총알 발사와 근접 히트박스 개폐 타이밍을 애니메이션 프레임에 맞춥니다.

[DisallowMultipleComponent]
public class DeadRevolverEnemyAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private DeadRevolverEnemyController controller;

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponentInParent<DeadRevolverEnemyController>();
        }
    }

    public void AnimationEvent_FireProjectile()
    {
        controller?.FireProjectileFromAnimation();
    }

    public void AnimationEvent_BeginPrimaryHitbox()
    {
        controller?.BeginPrimaryHitboxWindow();
    }

    public void AnimationEvent_EndPrimaryHitbox()
    {
        controller?.EndPrimaryHitboxWindow();
    }
}
