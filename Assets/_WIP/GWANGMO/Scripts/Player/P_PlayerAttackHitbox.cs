using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class P_PlayerAttackHitbox : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackUpForce = 2.5f;
    [SerializeField] private LayerMask hitLayers;

    private readonly HashSet<MonoBehaviour> alreadyHit = new();
    private readonly Collider2D[] overlapResults = new Collider2D[16];

    private Collider2D hitbox;
    private GameObject owner;
    private Vector2 hitDirection = Vector2.right;
    private bool isActive;

    private void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        hitbox.isTrigger = true;

        if (hitLayers.value == 0)
        {
            hitLayers = LayerMask.GetMask("Enemy");
        }

        hitbox.enabled = false;
    }

    public void BeginHitbox(GameObject attackOwner, Vector2 direction)
    {
        owner = attackOwner;
        hitDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        alreadyHit.Clear();
        isActive = true;
        hitbox.enabled = true;
        HitCurrentOverlaps();
    }

    public void EndHitbox()
    {
        isActive = false;
        alreadyHit.Clear();

        if (hitbox != null)
        {
            hitbox.enabled = false;
        }
    }

    private void OnDisable()
    {
        EndHitbox();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void HitCurrentOverlaps()
    {
        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = hitLayers.value != 0,
            layerMask = hitLayers
        };

        int count = hitbox.Overlap(filter, overlapResults);
        for (int i = 0; i < count; i++)
        {
            TryHit(overlapResults[i]);
        }
    }

    private void TryHit(Collider2D other)
    {
        if (!isActive || other == null)
        {
            return;
        }

        if (owner != null && other.transform.IsChildOf(owner.transform))
        {
            return;
        }

        if (hitLayers.value != 0 && ((1 << other.gameObject.layer) & hitLayers.value) == 0)
        {
            return;
        }

        MonoBehaviour target = ResolveDamageReceiver(other);
        if (target == null || alreadyHit.Contains(target) || target is not IDamageReceiver damageReceiver)
        {
            return;
        }

        alreadyHit.Add(target);
        Vector2 knockback = hitDirection * knockbackForce + Vector2.up * knockbackUpForce;
        if (damageReceiver.ReceiveHit(damage, knockback, owner))
        {
            CombatHitFeedback.PlayLightHit();
        }
    }

    private static MonoBehaviour ResolveDamageReceiver(Collider2D hit)
    {
        MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour is IDamageReceiver)
            {
                return behaviour;
            }
        }

        return null;
    }
}
