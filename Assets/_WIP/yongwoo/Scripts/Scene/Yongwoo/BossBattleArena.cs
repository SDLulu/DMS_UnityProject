using UnityEngine;

// 역할:
// - Cuphead식 고정 카메라 / 고정 스테이지 보스 아레나.
// - 입장 트리거가 EnterBattle()을 호출하면 카메라 고정, 카메라 화면 크기만큼 플레이어 클램프, 보스 텔포 앵커 연결, 패턴 시작.
// - 트리거·카메라앵커·텔포앵커 위치는 씬에서 수동 배치.

[DisallowMultipleComponent]
public class BossBattleArena : MonoBehaviour
{
    [Header("Arena")]
    [Tooltip("고정 카메라 중심. 플레이어 이동 범위도 이 위치 + 카메라 ortho size로 계산합니다.")]
    [SerializeField] private Transform cameraAnchor;
    [Tooltip("보스 텔포 후보. 비우면 자식 '텔포앵커/Anchor_*'를 자동 수집합니다.")]
    [SerializeField] private Transform[] teleportAnchors;
    [Tooltip("0이면 현재 카메라 orthographic size 유지.")]
    [SerializeField, Min(0f)] private float cameraOrthoSize;
    [Tooltip("카메라 화면 가장자리에서 안쪽으로 줄일 여백(월드 u).")]
    [SerializeField, Min(0f)] private float boundsPadding = 0.15f;

    [Header("Battle")]
    [SerializeField] private BossTeleporter bossTeleporter;
    [SerializeField] private BossPatternRunner patternRunner;

    [Header("Hybrid Arena Visuals")]
    [SerializeField] private bool useArenaHybridFrame = true;
    [SerializeField, Min(0)] private int scanlineCount = 8;
    [SerializeField, Min(0f)] private float arenaPulseSpeed = 5.2f;
    [SerializeField] private Color arenaFrameColor = new Color(0f, 0.92f, 1f, 0.34f);
    [SerializeField] private Color arenaWarningColor = new Color(1f, 0.18f, 0.42f, 0.28f);

    [Header("Battle UI")]
    [SerializeField] private BossHealthBarUI bossHealthBar;

    [Header("References")]
    [SerializeField] private SimpleCameraFollow cameraFollow;
    [SerializeField] private SimplePlayerController playerController;
    [SerializeField] private P_PlayerController pPlayerController;
    [SerializeField] private ScreenGlitchOverlay glitchOverlay;

    private bool _isActive;
    private Bounds _cameraWorldBounds;
    private Transform _arenaFxRoot;
    private SpriteRenderer[] _arenaBorders;
    private SpriteRenderer[] _arenaScanlines;
    private SpriteRenderer[] _arenaCorners;

    public bool IsActive => _isActive;
    public Bounds ArenaBounds => _cameraWorldBounds.size.sqrMagnitude > 0f ? _cameraWorldBounds : ComputeCameraWorldBounds();

    public Vector3 ClampToArenaBounds(Vector3 worldPosition)
    {
        Bounds bounds = ArenaBounds;
        worldPosition.x = Mathf.Clamp(worldPosition.x, bounds.min.x, bounds.max.x);
        worldPosition.y = Mathf.Clamp(worldPosition.y, bounds.min.y, bounds.max.y);
        return worldPosition;
    }

    public bool IsInsideArena(Vector3 worldPosition)
    {
        Bounds bounds = ArenaBounds;
        return worldPosition.x >= bounds.min.x && worldPosition.x <= bounds.max.x
            && worldPosition.y >= bounds.min.y && worldPosition.y <= bounds.max.y;
    }

    private void Reset()
    {
        AutoWire();
    }

    private void Awake()
    {
        AutoWire();
        CacheTeleportAnchorsIfEmpty();

        if (patternRunner != null)
        {
            patternRunner.enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (!_isActive)
        {
            return;
        }

        ClampPlayerToCameraBounds();
        UpdateArenaHybridVisuals();
    }

    public void EnterBattle()
    {
        if (_isActive)
        {
            return;
        }

        AutoWire();
        RefreshTeleportAnchorsFromHierarchy();

        _isActive = true;
        YongwooAudioManager.Play(YongwooSfxId.BossArenaEnter, 0.68f, 0.02f);

        if (cameraFollow != null && cameraAnchor != null)
        {
            Vector3 anchor = cameraAnchor.position;
            anchor.z = cameraFollow.transform.position.z;
            cameraFollow.LockToArenaPosition(anchor);
        }

        if (cameraOrthoSize > 0f && Camera.main != null)
        {
            Camera.main.orthographicSize = cameraOrthoSize;
        }

        _cameraWorldBounds = ComputeCameraWorldBounds();
        ValidateAndFitArenaContents();

        if (bossTeleporter != null)
        {
            bossTeleporter.SetArenaBounds(_cameraWorldBounds);

            if (teleportAnchors != null && teleportAnchors.Length > 0)
            {
                bossTeleporter.SetAnchors(teleportAnchors, arenaAnchorsOnly: true);
            }
        }

        if (patternRunner != null)
        {
            patternRunner.enabled = true;
        }

        EnsureArenaHybridVisuals();
        UpdateArenaHybridVisuals();
        PulseScreenGlitch(0.28f, 0.14f);
        BindBossHealthBar();
    }

    private void BindBossHealthBar()
    {
        bossHealthBar ??= FindFirstObjectByType<BossHealthBarUI>();
        if (bossHealthBar == null)
        {
            GameObject barHost = new GameObject("BossHealthBarUI");
            barHost.transform.SetParent(transform, false);
            bossHealthBar = barHost.AddComponent<BossHealthBarUI>();
        }

        BossPhaseController rootBoss = FindRootBossPhaseController();
        if (rootBoss != null)
        {
            bossHealthBar.Bind(rootBoss);
        }
    }

    private static BossPhaseController FindRootBossPhaseController()
    {
        BossPhaseController[] controllers = FindObjectsByType<BossPhaseController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            BossPhaseController controller = controllers[i];
            if (controller != null && controller.IsRootController)
            {
                return controller;
            }
        }

        return null;
    }

    private Bounds ComputeCameraWorldBounds()
    {
        Camera cam = Camera.main;
        Vector3 center = cameraAnchor != null
            ? cameraAnchor.position
            : cam != null
                ? cam.transform.position
                : transform.position;

        float halfHeight = cam != null ? cam.orthographicSize : 5f;
        float halfWidth = halfHeight * (cam != null ? cam.aspect : 16f / 9f);
        float pad = boundsPadding;
        return new Bounds(
            center,
            new Vector3(Mathf.Max(0.1f, halfWidth * 2f - pad * 2f), Mathf.Max(0.1f, halfHeight * 2f - pad * 2f), 0f));
    }

    private void ClampPlayerToCameraBounds()
    {
        playerController ??= FindFirstObjectByType<SimplePlayerController>();
        pPlayerController ??= FindFirstObjectByType<P_PlayerController>();

        Transform playerTransform = playerController != null
            ? playerController.transform
            : pPlayerController != null
                ? pPlayerController.transform
                : null;
        if (playerTransform == null)
        {
            return;
        }

        Bounds bounds = _cameraWorldBounds;
        Rigidbody2D body = playerTransform.GetComponent<Rigidbody2D>();

        Vector3 pos = playerTransform.position;
        pos.x = Mathf.Clamp(pos.x, bounds.min.x, bounds.max.x);
        pos.y = Mathf.Clamp(pos.y, bounds.min.y, bounds.max.y);
        playerTransform.position = pos;

        if (body != null)
        {
            Vector2 bodyPos = body.position;
            bodyPos.x = Mathf.Clamp(bodyPos.x, bounds.min.x, bounds.max.x);
            bodyPos.y = Mathf.Clamp(bodyPos.y, bounds.min.y, bounds.max.y);
            body.position = bodyPos;
        }
    }

    private void EnsureArenaHybridVisuals()
    {
        if (!useArenaHybridFrame)
        {
            return;
        }

        if (_arenaFxRoot == null)
        {
            Transform existing = transform.Find("Boss_ArenaFX");
            if (existing != null)
            {
                _arenaFxRoot = existing;
            }
            else
            {
                GameObject root = new GameObject("Boss_ArenaFX");
                root.transform.SetParent(transform, false);
                _arenaFxRoot = root.transform;
            }
        }

        _arenaBorders ??= new SpriteRenderer[4];
        _arenaCorners ??= new SpriteRenderer[4];
        if (_arenaScanlines == null || _arenaScanlines.Length != scanlineCount)
        {
            _arenaScanlines = new SpriteRenderer[Mathf.Max(0, scanlineCount)];
        }

        for (int i = 0; i < _arenaBorders.Length; i++)
        {
            _arenaBorders[i] = EnsureArenaFxRenderer($"Arena_Border_{i:00}", 14 + i);
        }

        for (int i = 0; i < _arenaCorners.Length; i++)
        {
            _arenaCorners[i] = EnsureArenaFxRenderer($"Arena_Corner_{i:00}", 20 + i);
        }

        for (int i = 0; i < _arenaScanlines.Length; i++)
        {
            _arenaScanlines[i] = EnsureArenaFxRenderer($"Arena_Scanline_{i:00}", 10 + i);
        }
    }

    private SpriteRenderer EnsureArenaFxRenderer(string objectName, int sortingOrder)
    {
        Transform child = _arenaFxRoot.Find(objectName);
        if (child == null)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(_arenaFxRoot, false);
            child = go.transform;
        }

        if (!child.TryGetComponent(out SpriteRenderer renderer))
        {
            renderer = child.gameObject.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = RuntimeSpriteUtility.WhiteSprite;
        renderer.sortingLayerName = "Effect";
        renderer.sortingOrder = sortingOrder;
        renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
        return renderer;
    }

    private void UpdateArenaHybridVisuals()
    {
        if (!useArenaHybridFrame || !_isActive)
        {
            return;
        }

        EnsureArenaHybridVisuals();
        if (_arenaFxRoot == null)
        {
            return;
        }

        Bounds bounds = ArenaBounds;
        Vector3 center = bounds.center;
        center.z = 0f;
        _arenaFxRoot.position = center;

        float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * arenaPulseSpeed) * 0.5f;
        float width = bounds.size.x;
        float height = bounds.size.y;
        float border = Mathf.Lerp(0.035f, 0.065f, pulse);
        float inset = 0.04f;

        ConfigureArenaBar(_arenaBorders[0], new Vector3(0f, height * 0.5f - inset, 0f), new Vector3(width, border, 1f), arenaFrameColor, Mathf.Lerp(0.2f, 0.44f, pulse));
        ConfigureArenaBar(_arenaBorders[1], new Vector3(0f, -height * 0.5f + inset, 0f), new Vector3(width, border, 1f), arenaFrameColor, Mathf.Lerp(0.18f, 0.36f, 1f - pulse));
        ConfigureArenaBar(_arenaBorders[2], new Vector3(-width * 0.5f + inset, 0f, 0f), new Vector3(border, height, 1f), arenaWarningColor, Mathf.Lerp(0.12f, 0.32f, pulse));
        ConfigureArenaBar(_arenaBorders[3], new Vector3(width * 0.5f - inset, 0f, 0f), new Vector3(border, height, 1f), arenaWarningColor, Mathf.Lerp(0.12f, 0.32f, 1f - pulse));

        for (int i = 0; i < _arenaCorners.Length; i++)
        {
            float x = (i % 2 == 0 ? -1f : 1f) * (width * 0.5f - 0.32f);
            float y = (i < 2 ? 1f : -1f) * (height * 0.5f - 0.32f);
            Color color = i % 2 == 0 ? arenaFrameColor : arenaWarningColor;
            ConfigureArenaBar(_arenaCorners[i], new Vector3(x, y, 0f), new Vector3(0.42f, 0.055f, 1f), color, Mathf.Lerp(0.3f, 0.72f, pulse));
            _arenaCorners[i].transform.localRotation = Quaternion.Euler(0f, 0f, i < 2 ? 0f : 180f);
        }

        for (int i = 0; i < _arenaScanlines.Length; i++)
        {
            float t = (i + 1f) / (_arenaScanlines.Length + 1f);
            float y = Mathf.Lerp(-height * 0.42f, height * 0.42f, t);
            float wave = 0.5f + Mathf.Sin(Time.unscaledTime * (arenaPulseSpeed * 0.64f) + i * 0.74f) * 0.5f;
            ConfigureArenaBar(
                _arenaScanlines[i],
                new Vector3(Mathf.Sin(Time.unscaledTime * 0.9f + i) * 0.05f, y, 0f),
                new Vector3(width * Mathf.Lerp(0.72f, 0.96f, wave), 0.012f, 1f),
                i % 2 == 0 ? arenaFrameColor : arenaWarningColor,
                Mathf.Lerp(0.035f, 0.12f, wave));
        }
    }

    private static void ConfigureArenaBar(SpriteRenderer renderer, Vector3 localPosition, Vector3 worldSize, Color color, float alpha)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.transform.localPosition = localPosition;
        Vector3 spriteSize = renderer.sprite != null ? renderer.sprite.bounds.size : Vector3.one;
        renderer.transform.localScale = new Vector3(
            worldSize.x / Mathf.Max(0.0001f, spriteSize.x),
            worldSize.y / Mathf.Max(0.0001f, spriteSize.y),
            worldSize.z);
        color.a = Mathf.Clamp01(alpha);
        renderer.color = color;
    }

    private void ValidateAndFitArenaContents()
    {
        if (bossTeleporter != null)
        {
            Transform boss = bossTeleporter.transform;
            if (!IsInsideArena(boss.position))
            {
                Debug.LogWarning(
                    $"[BossBattleArena] 보스 시작 위치가 아레나 밖입니다. 아레나 안으로 맞춥니다: {boss.name}",
                    boss);
                boss.position = ClampToArenaBounds(boss.position);
            }
        }

        if (teleportAnchors == null)
        {
            return;
        }

        for (int i = 0; i < teleportAnchors.Length; i++)
        {
            Transform anchor = teleportAnchors[i];
            if (anchor == null)
            {
                continue;
            }

            if (!IsInsideArena(anchor.position))
            {
                Debug.LogWarning(
                    $"[BossBattleArena] 텔포 앵커가 아레나 밖입니다. Anchor를 카메라앵커 와이어 박스 안에 두세요: {anchor.name}",
                    anchor);
            }
        }
    }

    private void AutoWire()
    {
        cameraFollow ??= FindFirstObjectByType<SimpleCameraFollow>();
        playerController ??= FindFirstObjectByType<SimplePlayerController>();

        if (bossTeleporter == null || patternRunner == null)
        {
            BossPatternRunner[] runners = FindObjectsByType<BossPatternRunner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < runners.Length; i++)
            {
                BossPatternRunner runner = runners[i];
                if (runner == null || runner.transform.root != transform.root)
                {
                    continue;
                }

                patternRunner ??= runner;
                bossTeleporter ??= runner.GetComponent<BossTeleporter>();
            }
        }

        glitchOverlay ??= FindFirstObjectByType<ScreenGlitchOverlay>();
    }

    private void PulseScreenGlitch(float intensity, float duration)
    {
        glitchOverlay ??= FindFirstObjectByType<ScreenGlitchOverlay>();
        if (glitchOverlay != null)
        {
            StartCoroutine(glitchOverlay.Pulse(intensity, duration));
        }
    }

    private void CacheTeleportAnchorsIfEmpty()
    {
        if (teleportAnchors != null && teleportAnchors.Length > 0)
        {
            return;
        }

        RefreshTeleportAnchorsFromHierarchy();
    }

    private void RefreshTeleportAnchorsFromHierarchy()
    {
        Transform anchorRoot = FindTeleportAnchorRoot();
        if (anchorRoot == null)
        {
            return;
        }

        var collected = new System.Collections.Generic.List<Transform>(anchorRoot.childCount);
        for (int i = 0; i < anchorRoot.childCount; i++)
        {
            Transform child = anchorRoot.GetChild(i);
            if (child != null)
            {
                collected.Add(child);
            }
        }

        collected.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        teleportAnchors = collected.ToArray();
    }

    private Transform FindTeleportAnchorRoot()
    {
        Transform named = transform.Find("텔포앵커");
        if (named != null)
        {
            return named;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null)
            {
                continue;
            }

            for (int j = 0; j < child.childCount; j++)
            {
                if (child.GetChild(j).name.StartsWith("Anchor_"))
                {
                    return child;
                }
            }
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (cameraAnchor == null)
        {
            return;
        }

        Vector3 size = ComputeGizmoSize();
        TutorialGizmoDraw.DrawWireBox(
            cameraAnchor.position,
            size,
            new Color(0.2f, 0.85f, 1f, 0.9f),
            "아레나");
    }

    private void OnDrawGizmosSelected()
    {
        if (cameraAnchor == null)
        {
            return;
        }

        Bounds bounds = new Bounds(cameraAnchor.position, ComputeGizmoSize());
        DrawBossFitGizmo(bounds);
    }

    private Vector3 ComputeGizmoSize()
    {
        Camera cam = Camera.main;
        float halfHeight = cameraOrthoSize > 0f
            ? cameraOrthoSize
            : cam != null
                ? cam.orthographicSize
                : 5f;
        float halfWidth = halfHeight * (cam != null ? cam.aspect : 16f / 9f);
        float pad = boundsPadding;
        return new Vector3(
            Mathf.Max(0.1f, halfWidth * 2f - pad * 2f),
            Mathf.Max(0.1f, halfHeight * 2f - pad * 2f),
            0f);
    }

    private void DrawBossFitGizmo(Bounds bounds)
    {
        BossTeleporter teleporter = bossTeleporter;
        if (teleporter == null)
        {
            BossPatternRunner[] runners = FindObjectsByType<BossPatternRunner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < runners.Length; i++)
            {
                if (runners[i] != null && runners[i].transform.root == transform.root)
                {
                    teleporter = runners[i].GetComponent<BossTeleporter>();
                    break;
                }
            }
        }

        if (teleporter == null)
        {
            return;
        }

        bool bossInside = bounds.Contains(teleporter.transform.position);
        Color bossColor = bossInside
            ? new Color(1f, 0.85f, 0.2f, 0.95f)
            : new Color(1f, 0.25f, 0.2f, 0.95f);
        TutorialGizmoDraw.DrawPoint(teleporter.transform.position, 0.55f, bossColor, teleporter.name);
    }
#endif
}
