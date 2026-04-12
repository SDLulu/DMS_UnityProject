using UnityEngine;

// 역할:
// - 총 공격의 쿨다운, 총구 화염 On/Off, 총알 프리팹 발사를 관리합니다.
// - 총알은 독립 객체이므로 Instantiate로 생성합니다.
//
// 구조 포인트:
// - PlayerHand의 자식으로 배치되어 마우스 회전을 자동으로 따라갑니다.

[DisallowMultipleComponent]
public class GunWeapon : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float cooldown = 0.18f;

    [Header("Muzzle Flash")]
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private float muzzleFlashDuration = 0.06f;

    [Header("Projectile")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 15f;
    [SerializeField] private float projectileLifetime = 1.2f;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackUpForce = 1.2f;

    private float _cooldownTimer;
    private float _muzzleFlashTimer;

    public bool CanAttack => _cooldownTimer <= 0f;

    public void Attack(Vector2 aimDirection, GameObject owner)
    {
        if (!CanAttack)
        {
            return;
        }

        _cooldownTimer = cooldown;

        if (muzzleFlash != null)
        {
            muzzleFlash.SetActive(true);
            _muzzleFlashTimer = muzzleFlashDuration;
        }

        Vector2 spawnPos = muzzlePoint != null
            ? (Vector2)muzzlePoint.position
            : (Vector2)transform.position;
        Vector2 dir = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : Vector2.right;
        Vector2 knockback = dir * knockbackForce + Vector2.up * knockbackUpForce;

        if (projectilePrefab != null)
        {
            GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            SimplePlayerProjectile projectile = proj.GetComponent<SimplePlayerProjectile>();
            if (projectile != null)
            {
                projectile.Launch(dir, projectileSpeed, projectileLifetime, damage, knockback, owner);
            }
        }
        else
        {
            GameObject proj = new GameObject("PlayerProjectile");
            proj.transform.position = spawnPos;
            SimplePlayerProjectile projectile = proj.AddComponent<SimplePlayerProjectile>();
            projectile.Launch(dir, projectileSpeed, projectileLifetime, damage, knockback, owner);
        }
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
        }

        if (_muzzleFlashTimer > 0f)
        {
            _muzzleFlashTimer -= Time.deltaTime;
            if (_muzzleFlashTimer <= 0f && muzzleFlash != null)
            {
                muzzleFlash.SetActive(false);
            }
        }
    }

    private void OnDisable()
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.SetActive(false);
        }

        _muzzleFlashTimer = 0f;
    }
}
