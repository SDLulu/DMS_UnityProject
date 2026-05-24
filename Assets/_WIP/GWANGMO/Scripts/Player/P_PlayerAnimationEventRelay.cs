using UnityEngine;

[DisallowMultipleComponent]
public class P_PlayerAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private P_PlayerCombat combat;
    [SerializeField] private P_PlayerController controller;

    private void Awake()
    {
        if (combat == null)
        {
            combat = GetComponentInParent<P_PlayerCombat>();
        }

        if (controller == null)
        {
            controller = GetComponentInParent<P_PlayerController>();
        }
    }

    public void BeginAttack1Hitbox()
    {
        combat?.BeginAttack1Hitbox();
    }

    public void EndAttack1Hitbox()
    {
        combat?.EndAttack1Hitbox();
    }

    public void BeginDashAttackMovement()
    {
        controller?.BeginDashAttackMovement();
        combat?.BeginDashAttackHitbox();
    }

    public void EndDashAttackMovement()
    {
        combat?.EndDashAttackHitbox();
        controller?.EndDashAttackMovement();
    }

    public void FinishDashAttack()
    {
        combat?.EndDashAttackHitbox();
        controller?.FinishDashAttack();
    }
}
