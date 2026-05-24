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

    [Header("VFX")]
    [SerializeField] private Color departRingColor = new Color(0.45f, 0.85f, 1f, 0.55f);
    [SerializeField] private Color arriveRingColor = new Color(1f, 0.25f, 0.2f, 0.65f);
    [SerializeField, Min(0.1f)] private float ringDiameter = 1.6f;

    private bool _isHopping;
    private bool _arenaAnchorsOnly;
    private bool _hasArenaBounds;
    private Bounds _arenaBounds;

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

    public void SetAnchors(Transform[] newAnchors, bool arenaAnchorsOnly = false)
    {
        anchors = newAnchors;
        _arenaAnchorsOnly = arenaAnchorsOnly;
    }

    public void SetArenaBounds(Bounds bounds)
    {
        _arenaBounds = bounds;
        _hasArenaBounds = true;
    }

    public Vector3 ClampToArena(Vector3 worldPosition)
    {
        return ClampDestination(worldPosition);
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
            Transform fallback = null;
            int safety = anchors.Length * 4;
            while (safety-- > 0)
            {
                Transform candidate = anchors[Random.Range(0, anchors.Length)];
                if (candidate == null)
                {
                    continue;
                }

                fallback ??= candidate;

                if (Vector3.SqrMagnitude(candidate.position - transform.position) < 0.01f)
                {
                    continue;
                }

                return ClampDestination(candidate.position);
            }

            if (fallback != null)
            {
                return ClampDestination(fallback.position);
            }

            if (_arenaAnchorsOnly)
            {
                return transform.position;
            }
        }

        if (_arenaAnchorsOnly || offsetRange.sqrMagnitude <= 0.0001f)
        {
            return transform.position;
        }

        Vector3 here = transform.position;
        float dx = Random.Range(-offsetRange.x, offsetRange.x);
        float dy = Random.Range(-offsetRange.y, offsetRange.y);
        return ClampDestination(here + new Vector3(dx, dy, 0f));
    }

    public void PatternBlinkTo(Vector3 destination)
    {
        Vector3 origin = transform.position;
        BossVfxUtility.SpawnRingBurst(origin, departRingColor, ringDiameter);
        BossVfxUtility.SpawnFlashDisc(origin, new Color(departRingColor.r, departRingColor.g, departRingColor.b, 0.35f), ringDiameter * 0.55f);
        YongwooAudioManager.Play(YongwooSfxId.BossTeleportOut, 0.62f, 0.04f);

        interaction?.SetTeleportInvulnerable(true);
        SetVisualsVisible(false);

        transform.position = ClampDestination(destination);

        SetVisualsVisible(true);
        BossVfxUtility.SpawnRingBurst(transform.position, arriveRingColor, ringDiameter * 1.1f);
        BossVfxUtility.SpawnFlashDisc(transform.position, new Color(arriveRingColor.r, arriveRingColor.g, arriveRingColor.b, 0.42f), ringDiameter * 0.65f);
        YongwooAudioManager.Play(YongwooSfxId.BossTeleportIn, 0.66f, 0.04f);
        interaction?.SetTeleportInvulnerable(false);
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

        Vector3 origin = transform.position;
        BossVfxUtility.SpawnRingBurst(origin, departRingColor, ringDiameter);
        BossVfxUtility.SpawnFlashDisc(origin, new Color(departRingColor.r, departRingColor.g, departRingColor.b, 0.35f), ringDiameter * 0.55f);
        YongwooAudioManager.Play(YongwooSfxId.BossTeleportOut, 0.62f, 0.04f);

        interaction?.SetTeleportInvulnerable(true);
        SetVisualsVisible(false);

        yield return new WaitForSeconds(invulnerableHopDuration);

        transform.position = ClampDestination(destination);
        BossVfxUtility.SpawnRingBurst(transform.position, arriveRingColor, ringDiameter * 1.1f);
        BossVfxUtility.SpawnFlashDisc(transform.position, new Color(arriveRingColor.r, arriveRingColor.g, arriveRingColor.b, 0.42f), ringDiameter * 0.65f);
        YongwooAudioManager.Play(YongwooSfxId.BossTeleportIn, 0.66f, 0.04f);
        SetVisualsVisible(true);
        interaction?.SetTeleportInvulnerable(false);

        _isHopping = false;
    }

    private Vector3 ClampDestination(Vector3 destination)
    {
        if (!_hasArenaBounds)
        {
            return destination;
        }

        destination.x = Mathf.Clamp(destination.x, _arenaBounds.min.x, _arenaBounds.max.x);
        destination.y = Mathf.Clamp(destination.y, _arenaBounds.min.y, _arenaBounds.max.y);
        return destination;
    }

    private void SetVisualsVisible(bool visible)
    {
        SpriteRenderer[] currentVisuals = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        if (currentVisuals != null && currentVisuals.Length > 0)
        {
            visualsToHide = currentVisuals;
        }

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
