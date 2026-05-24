using System;
using UnityEngine;

// 역할:
// - 공격 입력과 무기 전환을 읽어 활성 무기에 공격을 위임합니다.
// - 공격 방향은 플레이어의 현재 좌우 바라보기를 기준으로 정하고, 판정은 각 무기 스크립트가 담당합니다.
//
// 구조 포인트:
// - 이동은 SimplePlayerController, 판정은 Weapon 스크립트와 분리된 전투 입력 허브입니다.

[DisallowMultipleComponent]
public class SimplePlayerCombat : MonoBehaviour
{
    private const float PointerFacingDeadZone = 0.05f;

    public event Action AttackPerformed;

    [Header("Weapons")]
    [SerializeField] private PlayerWeaponType defaultWeapon = PlayerWeaponType.Sword;
    [SerializeField] private SwordWeapon swordWeapon;
    [SerializeField] private GunWeapon gunWeapon;

    [Header("Animation")]
    [SerializeField] private float attackAnimationDuration = 0.18f;

    private PlayerWeaponType _currentWeapon;
    private PlayerWeaponType _lastAttackWeapon;
    private SimplePlayerController _controller;
    private Camera _mainCamera;

    public PlayerWeaponType CurrentWeapon => _currentWeapon;
    public PlayerWeaponType LastAttackWeapon => _lastAttackWeapon;
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
        _lastAttackWeapon = _currentWeapon;
        attackAnimationDuration = config.attackAnimationDuration;
        ApplyWeaponVisibility();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        ResolveWeaponReferences();
        _currentWeapon = defaultWeapon;
        _lastAttackWeapon = _currentWeapon;
        ApplyWeaponVisibility();
    }

    private void Awake()
    {
        _controller = GetComponent<SimplePlayerController>();
        _mainCamera = Camera.main;
        ResolveWeaponReferences();
        _currentWeapon = defaultWeapon;
        _lastAttackWeapon = _currentWeapon;
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
            YongwooAudioManager.Play(YongwooSfxId.WeaponSwap, 0.52f, 0.04f);
        }

        UpdatePointerFacing();

        if (_controller != null && _controller.IsActionLocked)
        {
            return;
        }

        bool canAttack = _currentWeapon == PlayerWeaponType.Sword
            ? swordWeapon != null && swordWeapon.CanAttack
            : gunWeapon != null && gunWeapon.CanAttack;

        if (canAttack && GameInput.Instance.AttackPressed)
        {
            Vector2 aim = _currentWeapon == PlayerWeaponType.Gun
                ? GetPointerAimDirection()
                : GetFallbackAimDirection();
            _lastAttackWeapon = _currentWeapon;

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

    private Vector2 GetPointerAimDirection()
    {
        if (!TryGetPointerWorldPosition(out Vector2 pointerWorld))
        {
            return GetFallbackAimDirection();
        }

        Vector2 origin = gunWeapon != null && gunWeapon.MuzzlePoint != null
            ? gunWeapon.MuzzlePoint.position
            : transform.position;
        Vector2 direction = pointerWorld - origin;
        return direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : GetFallbackAimDirection();
    }

    private void UpdatePointerFacing()
    {
        if (_controller == null)
        {
            return;
        }

        if (_currentWeapon != PlayerWeaponType.Gun)
        {
            _controller.SetExternalFacing(0f, false);
            return;
        }

        if (!TryGetPointerWorldPosition(out Vector2 pointerWorld))
        {
            _controller.SetExternalFacing(_controller.FacingDirection, true);
            return;
        }

        float horizontalDelta = pointerWorld.x - transform.position.x;
        if (Mathf.Abs(horizontalDelta) <= PointerFacingDeadZone)
        {
            _controller.SetExternalFacing(_controller.FacingDirection, true);
            return;
        }

        _controller.SetExternalFacing(horizontalDelta, true);
    }

    private bool TryGetPointerWorldPosition(out Vector2 pointerWorld)
    {
        pointerWorld = Vector2.zero;

        if (!GameInput.Instance.TryGetPointerScreenPosition(out Vector2 screenPosition))
        {
            return false;
        }

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera == null)
        {
            return false;
        }

        Vector3 world = _mainCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, -_mainCamera.transform.position.z));
        pointerWorld = world;
        return true;
    }

    private void ApplyWeaponVisibility()
    {
        if (gunWeapon != null)
        {
            gunWeapon.gameObject.SetActive(_currentWeapon == PlayerWeaponType.Gun);
        }
    }

    public void AnimationEvent_BeginSwordHitbox()
    {
        swordWeapon?.AnimationEvent_BeginHitbox();
    }

    public void AnimationEvent_EndSwordHitbox()
    {
        swordWeapon?.AnimationEvent_EndHitbox();
    }

    private void ResolveWeaponReferences()
    {
        swordWeapon ??= GetComponentInChildren<SwordWeapon>(true);
        gunWeapon ??= GetComponentInChildren<GunWeapon>(true);
    }
}
