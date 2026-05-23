using System.Collections;
using UnityEngine;

// 역할:
// - 보스의 텔레포트 이동을 담당합니다.
// - 사이클: 선딜(예고 신호) → 사라짐 → 텔포 무적 0.4s → 목적지에서 출현.
// - 사라진 동안 BossInteraction.SetTeleportInvulnerable(true)로 피격을 막습니다.

[DisallowMultipleComponent]
public class BossTeleporter : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("사라지기 전 예고(선딜) 시간입니다. 0이면 즉시 사라집니다.")]
    [SerializeField, Min(0f)] private float telegraphDuration = 0.1f;
    [Tooltip("사라진 상태로 무적인 시간입니다. 확정 가정 = 0.4s.")]
    [SerializeField, Min(0f)] private float invulnerableHopDuration = 0.4f;

    [Header("Anchors")]
    [Tooltip("도착 가능한 후보 지점들. 비워두면 현재 위치에서 ±offsetRange 안 랜덤.")]
    [SerializeField] private Transform[] anchors;
    [Tooltip("anchors가 비었을 때 사용할 랜덤 오프셋 범위(월드 단위).")]
    [SerializeField] private Vector2 offsetRange = new Vector2(4f, 0f);

    [Header("References")]
    [SerializeField] private BossInteraction interaction;
    [SerializeField] private SpriteRenderer[] visualsToHide;

    private bool _isHopping;

    public bool IsHopping => _isHopping;

    private void Reset()
    {
        interaction = GetComponent<BossInteraction>();
        visualsToHide = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        interaction ??= GetComponent<BossInteraction>();
        if (visualsToHide == null || visualsToHide.Length == 0)
        {
            visualsToHide = GetComponentsInChildren<SpriteRenderer>();
        }
    }

    public Coroutine HopToRandom()
    {
        return StartCoroutine(HopRoutine(PickDestination()));
    }

    public Coroutine HopTo(Vector3 destination)
    {
        return StartCoroutine(HopRoutine(destination));
    }

    private Vector3 PickDestination()
    {
        if (anchors != null && anchors.Length > 0)
        {
            int safety = 8;
            Transform pick = null;
            while (safety-- > 0)
            {
                Transform candidate = anchors[Random.Range(0, anchors.Length)];
                if (candidate == null)
                {
                    continue;
                }

                if (Vector3.SqrMagnitude(candidate.position - transform.position) < 0.01f)
                {
                    continue;
                }

                pick = candidate;
                break;
            }

            if (pick != null)
            {
                return pick.position;
            }
        }

        Vector3 here = transform.position;
        float dx = Random.Range(-offsetRange.x, offsetRange.x);
        float dy = Random.Range(-offsetRange.y, offsetRange.y);
        return here + new Vector3(dx, dy, 0f);
    }

    private IEnumerator HopRoutine(Vector3 destination)
    {
        if (_isHopping)
        {
            yield break;
        }

        _isHopping = true;

        if (telegraphDuration > 0f)
        {
            yield return new WaitForSeconds(telegraphDuration);
        }

        interaction?.SetTeleportInvulnerable(true);
        SetVisualsVisible(false);

        yield return new WaitForSeconds(invulnerableHopDuration);

        transform.position = destination;
        SetVisualsVisible(true);
        interaction?.SetTeleportInvulnerable(false);

        _isHopping = false;
    }

    private void SetVisualsVisible(bool visible)
    {
        if (visualsToHide == null)
        {
            return;
        }

        for (int i = 0; i < visualsToHide.Length; i++)
        {
            if (visualsToHide[i] != null)
            {
                visualsToHide[i].enabled = visible;
            }
        }
    }
}
