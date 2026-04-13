using UnityEngine;

// 역할:
// - 칼 공격의 쿨다운과 SlashHitbox 활성화 타이밍을 관리합니다.
// - 나중에 Animator를 붙이면 애니메이션 이벤트로 SlashHitbox를 제어할 수 있습니다.
//
// 구조 포인트:
// - PlayerHand의 자식으로 배치되어 마우스 회전을 자동으로 따라갑니다.

[DisallowMultipleComponent]
public class SwordWeapon : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float cooldown = 0.28f;
    [SerializeField] private float slashActiveDuration = 0.12f;
    [SerializeField] private SlashHitbox slashHitbox;

    private float _cooldownTimer;
    private float _slashTimer;

    public bool CanAttack => _cooldownTimer <= 0f;

    public void Attack(Vector2 aimDirection, GameObject owner)
    {
        if (!CanAttack)
        {
            return;
        }

        _cooldownTimer = cooldown;
        _slashTimer = slashActiveDuration;

        if (slashHitbox != null)
        {
            slashHitbox.Activate(owner, aimDirection);
        }
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
        }

        if (_slashTimer > 0f)
        {
            _slashTimer -= Time.deltaTime;
            if (_slashTimer <= 0f && slashHitbox != null)
            {
                slashHitbox.Deactivate();
            }
        }
    }

    private void OnDisable()
    {
        if (slashHitbox != null)
        {
            slashHitbox.Deactivate();
        }

        _slashTimer = 0f;
    }
}
