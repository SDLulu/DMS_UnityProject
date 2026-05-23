using UnityEngine;

[DisallowMultipleComponent]
public class P_PlayerAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private P_PlayerCombat combat;

    private void Awake()
    {
        if (combat == null)
        {
            combat = GetComponentInParent<P_PlayerCombat>();
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
}
