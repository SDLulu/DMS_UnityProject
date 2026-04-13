using System;
using UnityEngine;

// 역할:
// - 공격 입력과 무기 전환을 읽어 활성 무기에 공격을 위임합니다.
// - 조준 방향은 PlayerHand에서 받고, 판정은 각 무기 스크립트가 담당합니다.
//
// 구조 포인트:
// - 이동은 SimplePlayerController, 조준은 PlayerHand, 판정은 Weapon 스크립트와 분리된 전투 입력 허브입니다.

[DisallowMultipleComponent]
public class SimplePlayerCombat : MonoBehaviour
{
    public event Action AttackPerformed;

    [Header("Weapons")]
    [SerializeField] private PlayerWeaponType defaultWeapon = PlayerWeaponType.Sword;
    [SerializeField] private SwordWeapon swordWeapon;
    [SerializeField] private GunWeapon gunWeapon;

    [Header("Animation")]
    [SerializeField] private float attackAnimationDuration = 0.18f;

    private PlayerWeaponType _currentWeapon;
    private SimplePlayerController _controller;
    private PlayerHand _hand;

    public PlayerWeaponType CurrentWeapon => _currentWeapon;
    public float AttackAnimationDuration => attackAnimationDuration;

    public PlayerAttackConfig CreateConfigSnapshot()
    {
        return new PlayerAttackConfig
        {
            defaultWeapon = _currentWeapon,
            attackAnimationDuration = attackAnimationDuration
        };
    }

    public void ApplyConfig(PlayerAttackConfig config)
    {
        if (config == null)
        {
            return;
        }

        _currentWeapon = config.defaultWeapon;
        attackAnimationDuration = config.attackAnimationDuration;
        ApplyWeaponVisibility();
    }

    private void Awake()
    {
        _controller = GetComponent<SimplePlayerController>();
        _hand = GetComponentInChildren<PlayerHand>();
        _currentWeapon = defaultWeapon;
        ApplyWeaponVisibility();
    }

    private void Update()
    {
        if (GameInput.Instance.WeaponSwapPressed)
        {
            _currentWeapon = _currentWeapon == PlayerWeaponType.Sword
                ? PlayerWeaponType.Gun
                : PlayerWeaponType.Sword;
            ApplyWeaponVisibility();
        }

        if (_controller != null && _controller.IsActionLocked)
        {
            return;
        }

        bool canAttack = _currentWeapon == PlayerWeaponType.Sword
            ? swordWeapon != null && swordWeapon.CanAttack
            : gunWeapon != null && gunWeapon.CanAttack;

        if (canAttack && GameInput.Instance.AttackPressed)
        {
            Vector2 aim = _hand != null ? _hand.AimDirection : GetFallbackAimDirection();

            if (_currentWeapon == PlayerWeaponType.Sword && swordWeapon != null)
            {
                swordWeapon.Attack(aim, gameObject);
            }
            else if (gunWeapon != null)
            {
                gunWeapon.Attack(aim, gameObject);
            }

            AttackPerformed?.Invoke();
        }
    }

    private Vector2 GetFallbackAimDirection()
    {
        return _controller != null && _controller.FacingDirection < 0f
            ? Vector2.left
            : Vector2.right;
    }

    private void ApplyWeaponVisibility()
    {
        if (swordWeapon != null)
        {
            swordWeapon.gameObject.SetActive(_currentWeapon == PlayerWeaponType.Sword);
        }

        if (gunWeapon != null)
        {
            gunWeapon.gameObject.SetActive(_currentWeapon == PlayerWeaponType.Gun);
        }
    }
}
