using UnityEngine;

// 역할:
// - 칼 공격의 쿨다운과 SlashHitbox 활성화 타이밍을 관리합니다.
// - 나중에 Animator를 붙이면 애니메이션 이벤트로 SlashHitbox를 제어할 수 있습니다.
//
// 구조 포인트:
// - 칼 포즈는 플레이어 애니메이션이 담당하고, 이 스크립트는 판정 타이밍만 맡습니다.

[DisallowMultipleComponent]
public class SwordWeapon : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float cooldown = 0.28f;
    [SerializeField] private bool useAnimationEvents = true;
    [SerializeField] private float slashActiveDuration = 0.12f;
    [SerializeField] private SlashHitbox slashHitbox;

    private float _cooldownTimer;
    private float _slashTimer;
    private GameObject _pendingOwner;
    private Vector2 _pendingAimDirection = Vector2.right;
    private bool _attackArmed;

    public bool CanAttack => _cooldownTimer <= 0f;

    public void Attack(Vector2 aimDirection, GameObject owner)
    {
        if (!CanAttack)
        {
            return;
        }

        _cooldownTimer = cooldown;

        if (!useAnimationEvents)
        {
            _slashTimer = slashActiveDuration;
            if (slashHitbox != null)
            {
                slashHitbox.Activate(owner, aimDirection);
            }
            return;
        }

        _pendingOwner = owner;
        _pendingAimDirection = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : Vector2.right;
        _attackArmed = true;
        slashHitbox?.Deactivate();
    }

    public void AnimationEvent_BeginHitbox()
    {
        if (!useAnimationEvents || !_attackArmed || slashHitbox == null)
        {
            return;
        }

        slashHitbox.Activate(_pendingOwner, _pendingAimDirection);
    }

    public void AnimationEvent_EndHitbox()
    {
        if (slashHitbox == null)
        {
            return;
        }

        slashHitbox.Deactivate();
        _attackArmed = false;
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
        _attackArmed = false;
    }
}
