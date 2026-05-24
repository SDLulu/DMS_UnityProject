using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 역할:
// - P1 본체 사망 → P2 2분열 → P3 3분열 전환을 보스 객체 안에서 관리합니다.
// - 분열체는 같은 보스 프리팹을 복제하되, 역할별 패턴 슬롯과 HP만 다르게 씁니다.

[DisallowMultipleComponent]
public class BossPhaseController : MonoBehaviour
{
    private enum Phase
    {
        P1,
        P2,
        P3,
        Complete,
    }

    private enum Role
    {
        Main,
        A,
        B,
        C,
    }

    [Header("Health")]
    [SerializeField, Min(1)] private int p1Health = 5;
    [SerializeField, Min(1)] private int p2CloneHealth = 4;
    [SerializeField, Min(1)] private int p3CloneHealth = 3;

    [Header("Transition Timing")]
    [SerializeField, Min(0f)] private float p2StartupDelay = 0.7f;
    [SerializeField, Min(0f)] private float p3StartupDelay = 0.8f;

    [Header("Phase Transition Glitch")]
    [SerializeField, Min(0f)] private float phaseGlitchFadeIn = 0.22f;
    [SerializeField, Min(0f)] private float phaseGlitchHold = 0.2f;
    [SerializeField, Min(0f)] private float phaseGlitchFadeOut = 0.4f;
    [SerializeField, Range(0f, 1f)] private float phaseGlitchPeak = 0.4f;

    [Header("Final Defeat")]
    [SerializeField, Min(0.1f)] private float finaleMergeDuration = 0.58f;
    [SerializeField, Min(0f)] private float finaleGlitchHold = 0.32f;
    [SerializeField, Min(1)] private int finaleSoulMoteCount = 72;

    [Header("Narrative Sequences")]
    [SerializeField] private SceneEventSequence p1ToP2Sequence;
    [SerializeField] private SceneEventSequence p2ToP3Sequence;
    [SerializeField] private SceneEventSequence finalDefeatSequence;

    [Header("Sprites")]
    [SerializeField] private Sprite p1Sprite;
    [SerializeField] private Sprite cloneASprite;
    [SerializeField] private Sprite cloneBSprite;
    [SerializeField] private Sprite cloneCSprite;

    [Header("Hybrid Visuals")]
    [SerializeField] private bool useHybridVisuals = true;
    [SerializeField] private bool useIdleFrameSprites = true;
    [SerializeField, Min(0.04f)] private float idleFrameDuration = 0.14f;
    [SerializeField, Min(0f)] private float idleBobAmplitude = 0.035f;
    [SerializeField, Min(0f)] private float idleScalePulse = 0.018f;
    [SerializeField, Min(0.05f)] private float visualPulseSpeed = 4.6f;
    [SerializeField, Min(0.02f)] private float glitchInterval = 0.08f;
    [SerializeField, Min(0f)] private float damageShakeStrength = 0.08f;
    [SerializeField, Min(0.01f)] private float damageShakeDuration = 0.08f;
    [SerializeField, Min(0f)] private float splitShakeStrength = 0.16f;
    [SerializeField, Min(0.01f)] private float splitShakeDuration = 0.16f;
    [SerializeField] private Sprite[] p1IdleFrames;
    [SerializeField] private Sprite[] cloneAIdleFrames;
    [SerializeField] private Sprite[] cloneBIdleFrames;
    [SerializeField] private Sprite[] cloneCIdleFrames;

    [Header("References")]
    [SerializeField] private BossInteraction interaction;
    [SerializeField] private BossTeleporter teleporter;
    [SerializeField] private BossPatternRunner runner;
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private ScreenGlitchOverlay glitchOverlay;

    private List<BossPhaseController> _members = new();
    private Transform[] _arenaAnchors;
    private Bounds _arenaBounds;
    private BossPhaseController _owner;
    private Phase _phase = Phase.P1;
    private Role _role = Role.Main;
    private bool _reportedDead;
    private bool _defeated;
    private bool _transitioning;
    private bool _subscribed;
    private Coroutine _transitionRoutine;
    private bool _rootHealthInitialized;
    private bool _ownsTransitionFreeze;
    private PlayerSlowMotion _transitionSlowMotion;
    private bool _hybridVisualsReady;
    private Vector3 _visualBaseLocalPosition;
    private Vector3 _visualBaseLocalScale = Vector3.one;
    private float _visualClockOffset;
    private float _glitchTimer;
    private int _idleFrameIndex = -1;
    private Color _rolePrimary = new(0f, 0.88f, 1f, 1f);
    private Color _roleSecondary = new(1f, 0.12f, 0.6f, 1f);
    private SpriteRenderer _coreRenderer;
    private SpriteRenderer _haloRenderer;
    private SpriteRenderer _verticalLineRenderer;
    private readonly List<SpriteRenderer> _glitchBars = new();
    private readonly List<SpriteRenderer> _fadeRenderers = new();
    private readonly List<Color> _fadeStartColors = new();

    public bool IsRootController => _owner == null;
    public bool IsDefeated => _defeated || _reportedDead;
    public int AggregateCurrentHealth { get; private set; }
    public int AggregateMaxHealth { get; private set; }
    public event System.Action<int, int> AggregateHealthChanged;

#if UNITY_EDITOR
    private static readonly string[] EditorIdleFramePaths =
    {
        "Assets/_WIP/yongwoo/Art/Boss/Runtime_20260523/AnimationSheets/boss_p1_idle_minimal_idle_4f.png",
        "Assets/_WIP/yongwoo/Art/Boss/Runtime_20260523/AnimationSheets/boss_clone_a_idle_minimal_idle_4f.png",
        "Assets/_WIP/yongwoo/Art/Boss/Runtime_20260523/AnimationSheets/boss_clone_b_idle_minimal_idle_4f.png",
        "Assets/_WIP/yongwoo/Art/Boss/Runtime_20260523/AnimationSheets/boss_clone_c_idle_minimal_idle_4f.png"
    };
#endif

    private void Awake()
    {
        _members ??= new List<BossPhaseController>();
        TryLoadEditorIdleFrames();
        AutoWire();
        Subscribe();
    }

    private void Start()
    {
        if (!IsRootController || _rootHealthInitialized)
        {
            return;
        }

        _rootHealthInitialized = true;
        _members.Clear();
        _members.Add(this);
        interaction?.ResetHealth(p1Health);
        SubscribeMemberHealth(this);
        RefreshAggregateHealth();
    }

    private void Update()
    {
        if (_defeated)
        {
            return;
        }

        UpdateHybridVisuals();
    }

    private void OnDestroy()
    {
        ReleaseTransitionFreeze();
        Unsubscribe();
    }

    private void AutoWire()
    {
        interaction ??= GetComponent<BossInteraction>();
        teleporter ??= GetComponent<BossTeleporter>();
        runner ??= GetComponent<BossPatternRunner>();
        visualRenderer ??= ResolveVisualRenderer();
        glitchOverlay ??= FindFirstObjectByType<ScreenGlitchOverlay>();
        ApplyDarkBodyTint();
        EnsureHybridVisuals();
    }

    private void Subscribe()
    {
        if (_subscribed || interaction == null)
        {
            return;
        }

        interaction.Damaged += HandleDamaged;
        interaction.Died += HandleDied;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || interaction == null)
        {
            return;
        }

        interaction.Damaged -= HandleDamaged;
        interaction.Died -= HandleDied;
        _subscribed = false;
    }

    private void HandleDamaged(int currentHealth)
    {
        if (_defeated)
        {
            return;
        }

        SpawnImpactBurst(transform.position, _rolePrimary, _roleSecondary, 9, 0.18f, 0.06f);
        SpawnShockwave(transform.position, _rolePrimary, 0.18f, 0.34f, 2.1f, 0.56f);
        ShakeCamera(damageShakeStrength, damageShakeDuration);
        UpdateGlitchBars(1f);
        GetRoot().RefreshAggregateHealth();
    }

    private void HandleDied()
    {
        if (_transitioning)
        {
            return;
        }

        if (_owner != null)
        {
            ApplyDefeatedPresentation();
            GetRoot().RefreshAggregateHealth();
            _owner.ReportMemberDead(this);
            return;
        }

        if (_phase == Phase.P1)
        {
            SpawnImpactBurst(transform.position, _rolePrimary, _roleSecondary, 18, 0.32f, 0.12f);
            SpawnShockwave(transform.position, _roleSecondary, 0.42f, 0.72f, 3.1f, 0.72f);
            ShakeCamera(splitShakeStrength, splitShakeDuration);
            p1ToP2Sequence ??= BossStoryRuntimeSequenceFactory.EnsureP1ToP2Sequence(transform);
            BeginPhaseTransition(EnterP2, p1ToP2Sequence);
            return;
        }

        SpawnImpactBurst(transform.position, _rolePrimary, _roleSecondary, 12, 0.24f, 0.08f);
        SpawnShockwave(transform.position, _rolePrimary, 0.28f, 0.48f, 2.4f, 0.62f);
        ShakeCamera(damageShakeStrength, damageShakeDuration);
        ApplyDefeatedPresentation();
        GetRoot().RefreshAggregateHealth();
        ReportMemberDead(this);
    }

    private void EnterP2()
    {
        CacheArena();
        ClearMembers(keepSelf: true);
        _phase = Phase.P2;
        _role = Role.A;
        _members.Clear();
        _members.Add(this);

        ConfigureMember(this, Phase.P2, Role.A, p2CloneHealth, cloneASprite, PointInArena(0.28f, 0.55f), p2StartupDelay);
        BossPhaseController cloneB = CreateClone("Boss_P2_B", Phase.P2, Role.B, p2CloneHealth, cloneBSprite, PointInArena(0.72f, 0.62f), p2StartupDelay);
        _members.Add(cloneB);
    }

    private void EnterP3()
    {
        CacheArena();
        ClearMembers(keepSelf: true);
        _phase = Phase.P3;
        _role = Role.A;
        _members.Clear();
        _members.Add(this);

        ConfigureMember(this, Phase.P3, Role.A, p3CloneHealth, cloneASprite, PointInArena(0.2f, 0.62f), p3StartupDelay);
        BossPhaseController cloneB = CreateClone("Boss_P3_B", Phase.P3, Role.B, p3CloneHealth, cloneBSprite, PointInArena(0.8f, 0.62f), p3StartupDelay);
        BossPhaseController cloneC = CreateClone("Boss_P3_C", Phase.P3, Role.C, p3CloneHealth, cloneCSprite, PointInArena(0.5f, 0.82f), p3StartupDelay);
        _members.Add(cloneB);
        _members.Add(cloneC);
    }

    private void PlayPhaseRevealBursts()
    {
        for (int i = 0; i < _members.Count; i++)
        {
            BossPhaseController member = _members[i];
            if (member == null)
            {
                continue;
            }

            SpawnSplitArrivalBurst(member.transform.position, member._role);
        }
    }

    private BossPhaseController CreateClone(string cloneName, Phase phase, Role role, int health, Sprite sprite, Vector3 position, float startupDelay)
    {
        BossPhaseController clone = Instantiate(this, transform.parent);
        clone.name = cloneName;
        clone._owner = this;
        clone._members = new List<BossPhaseController>();
        ConfigureMember(clone, phase, role, health, sprite, position, startupDelay);
        return clone;
    }

    private void ConfigureMember(BossPhaseController member, Phase phase, Role role, int health, Sprite sprite, Vector3 position, float startupDelay)
    {
        member.AutoWire();
        member.Subscribe();
        member._phase = phase;
        member._role = role;
        member._reportedDead = false;
        member._defeated = false;
        member._transitioning = false;
        member.transform.position = position;

        member.interaction?.ResetHealth(health);
        RestoreMemberCollidersAndVisuals(member);

        if (member.visualRenderer != null)
        {
            if (sprite != null)
            {
                member.visualRenderer.sprite = sprite;
            }

            member.visualRenderer.color = BossBodyVisual.DarkTint;
        }

        member.ConfigureHybridRoleVisuals(role);

        if (member.teleporter != null)
        {
            member.teleporter.SetArenaBounds(_arenaBounds);
            if (_arenaAnchors != null && _arenaAnchors.Length > 0)
            {
                member.teleporter.SetAnchors(_arenaAnchors, arenaAnchorsOnly: true);
            }
        }

        if (member.runner != null)
        {
            member.runner.SetPatternSlots(member.BuildPatternSlots(role), allowAutoMerge: false);
            member.runner.RestartPatternLoop(startupDelay);
        }

        GetRoot().TrackMember(member);
    }

    private static void RestoreMemberCollidersAndVisuals(BossPhaseController member)
    {
        Collider2D[] colliders = member.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = true;
            }
        }

        SpriteRenderer[] renderers = member.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = true;
            }
        }
    }

    private MonoBehaviour[] BuildPatternSlots(Role role)
    {
        List<MonoBehaviour> slots = new();

        switch (role)
        {
            case Role.A:
                AddSlot<BossPatternDashSlash>(slots);
                AddSlot<BossPatternStraightShot>(slots);
                AddSlot<BossPatternTeleportSlam>(slots);
                break;

            case Role.B:
                AddSlot<BossPatternSpread>(slots);
                AddSlot<BossPatternPredictShot>(slots);
                AddSlot<BossPatternVolley>(slots);
                break;

            case Role.C:
                AddSlot<BossPatternDelayedBlast>(slots);
                AddSlot<BossPatternLaserWall>(slots);
                AddSlot<BossPatternSafeZoneCollapse>(slots);
                break;

            default:
                AddSlot<BossPatternStraightShot>(slots);
                AddSlot<BossPatternVolley>(slots);
                AddSlot<BossPatternSpread>(slots);
                AddSlot<BossPatternDashSlash>(slots);
                AddSlot<BossPatternDelayedBlast>(slots);
                AddSlot<BossPatternPredictShot>(slots);
                break;
        }

        return slots.ToArray();
    }

    private void AddSlot<T>(List<MonoBehaviour> slots) where T : MonoBehaviour, IBossPattern
    {
        T pattern = GetComponent<T>();
        if (pattern != null)
        {
            slots.Add(pattern);
        }
    }

    private void ReportMemberDead(BossPhaseController member)
    {
        if (member._reportedDead)
        {
            return;
        }

        member._reportedDead = true;

        if (HasAlivePhaseMembers())
        {
            return;
        }

        if (_phase == Phase.P2)
        {
            SpawnImpactBurst(transform.position, _rolePrimary, _roleSecondary, 16, 0.28f, 0.1f);
            SpawnShockwave(transform.position, _roleSecondary, 0.46f, 0.78f, 3.2f, 0.7f);
            ShakeCamera(splitShakeStrength, splitShakeDuration);
            p2ToP3Sequence ??= BossStoryRuntimeSequenceFactory.EnsureP2ToP3Sequence(transform);
            BeginPhaseTransition(EnterP3, p2ToP3Sequence);
        }
        else if (_phase == Phase.P3)
        {
            finalDefeatSequence ??= BossStoryRuntimeSequenceFactory.EnsureFinalDefeatSequence(transform, ComputeMergeCenter(CollectPhaseMemberPositions()));
            BeginFinalDefeatSequence();
        }
    }

    private void BeginPhaseTransition(System.Action setupPhase, SceneEventSequence narrativeSequence)
    {
        if (_transitionRoutine != null)
        {
            StopCoroutine(_transitionRoutine);
        }

        YongwooAudioManager.Play(YongwooSfxId.BossPhaseShift, 0.78f, 0.02f);
        _transitionRoutine = StartCoroutine(PlayPhaseTransitionRoutine(setupPhase, narrativeSequence));
    }

    private void BeginFinalDefeatSequence()
    {
        if (_transitionRoutine != null)
        {
            StopCoroutine(_transitionRoutine);
        }

        YongwooAudioManager.Play(YongwooSfxId.BossDeath, 0.85f, 0.01f);
        _transitionRoutine = StartCoroutine(PlayFinalDefeatRoutine());
    }

    private IEnumerator PlayPhaseTransitionRoutine(System.Action setupPhase, SceneEventSequence narrativeSequence)
    {
        _transitioning = true;
        SetAllMemberRunnersEnabled(false);
        SetGameplayControl(false);

        if (narrativeSequence != null)
        {
            narrativeSequence.Play();
            while (narrativeSequence.IsPlaying)
            {
                yield return null;
            }
        }

        SetGameplayControl(false);
        PushTransitionFreeze();
        glitchOverlay ??= FindFirstObjectByType<ScreenGlitchOverlay>();
        if (glitchOverlay != null)
        {
            yield return glitchOverlay.PlayTransitionCover(
                phaseGlitchFadeIn,
                phaseGlitchHold,
                0f,
                phaseGlitchPeak);
        }

        setupPhase?.Invoke();

        yield return new WaitForSecondsRealtime(0.06f);

        if (glitchOverlay != null)
        {
            yield return glitchOverlay.FadeTo(0f, phaseGlitchFadeOut);
        }

        PlayPhaseRevealBursts();

        _transitioning = false;
        _transitionRoutine = null;
        RefreshAggregateHealth();
        ReleaseTransitionFreeze();
        SetGameplayControl(true);
    }

    private IEnumerator PlayFinalDefeatRoutine()
    {
        _transitioning = true;
        SetAllMemberRunnersEnabled(false);
        SetGameplayControl(false);
        PushTransitionFreeze();

        List<Vector3> mergeSources = CollectPhaseMemberPositions();
        Vector3 mergeCenter = ComputeMergeCenter(mergeSources);
        Color primary = RolePrimaryColor(_role);
        Color accent = RoleSecondaryColor(_role);

        SpawnImpactBurst(mergeCenter, primary, accent, 22, 0.42f, 0.14f);
        SpawnShockwave(mergeCenter, accent, 0.62f, 0.92f, 3.8f, 0.78f);
        ShakeCamera(splitShakeStrength * 1.35f, splitShakeDuration * 1.25f);

        SetPhaseMemberVisualsVisible(false);

        if (mergeSources.Count > 0)
        {
            yield return BossFinaleVfx.PlayConvergence(this, mergeSources, mergeCenter, finaleMergeDuration, primary, accent);
        }

        glitchOverlay ??= FindFirstObjectByType<ScreenGlitchOverlay>();
        if (glitchOverlay != null)
        {
            yield return glitchOverlay.FadeTo(phaseGlitchPeak, phaseGlitchFadeIn);
        }

        BossFinaleVfx.SpawnHollowKnightSoulBurst(mergeCenter, primary, accent, finaleSoulMoteCount);
        BossVfxUtility.SpawnRingBurst(mergeCenter, Color.white, 2.4f, 0.55f, 90);
        BossVfxUtility.SpawnFlashDisc(mergeCenter, new Color(1f, 1f, 1f, 0.88f), 2f, 0.38f, 91);

        yield return new WaitForSecondsRealtime(finaleGlitchHold);

        if (glitchOverlay != null)
        {
            yield return glitchOverlay.FadeTo(0f, phaseGlitchFadeOut * 1.35f);
        }

        ReleaseTransitionFreeze();

        _phase = Phase.Complete;

        if (finalDefeatSequence != null)
        {
            finalDefeatSequence.Play();
            while (finalDefeatSequence.IsPlaying)
            {
                yield return null;
            }
        }

        _transitioning = false;
        _transitionRoutine = null;
    }

    private static void SetGameplayControl(bool enabled)
    {
        if (enabled)
        {
            GameInput.Instance.EnableGameplay();
        }
        else
        {
            GameInput.Instance.DisableAllGameplayInput();
        }

        PlayerInteraction player = FindFirstObjectByType<PlayerInteraction>();
        player?.SetGameplayControlEnabled(enabled, clearVelocity: !enabled);
    }

    private void PushTransitionFreeze()
    {
        if (_ownsTransitionFreeze)
        {
            return;
        }

        _transitionSlowMotion ??= FindFirstObjectByType<PlayerSlowMotion>();
        _transitionSlowMotion?.PushExternalFreeze();
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
        _ownsTransitionFreeze = true;
    }

    private void ReleaseTransitionFreeze()
    {
        if (!_ownsTransitionFreeze)
        {
            return;
        }

        _ownsTransitionFreeze = false;
        _transitionSlowMotion?.PopExternalFreeze();
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    private List<Vector3> CollectPhaseMemberPositions()
    {
        List<Vector3> points = new();
        BossPhaseController[] controllers = FindObjectsByType<BossPhaseController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            BossPhaseController member = controllers[i];
            if (member == null || member._phase != _phase)
            {
                continue;
            }

            if (member != this && member._owner != this)
            {
                continue;
            }

            points.Add(member.transform.position);
        }

        return points;
    }

    private Vector3 ComputeMergeCenter(List<Vector3> points)
    {
        if (points == null || points.Count == 0)
        {
            return transform.position;
        }

        Vector3 sum = Vector3.zero;
        for (int i = 0; i < points.Count; i++)
        {
            sum += points[i];
        }

        return sum / points.Count;
    }

    private void SetPhaseMemberVisualsVisible(bool visible)
    {
        BossPhaseController[] controllers = FindObjectsByType<BossPhaseController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            BossPhaseController member = controllers[i];
            if (member == null || member._phase != _phase)
            {
                continue;
            }

            if (member != this && member._owner != this)
            {
                continue;
            }

            SpriteRenderer[] renderers = member.GetComponentsInChildren<SpriteRenderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                if (renderers[r] != null)
                {
                    renderers[r].enabled = visible;
                }
            }
        }
    }

    private void SetAllMemberRunnersEnabled(bool enabled)
    {
        BossPhaseController[] controllers = FindObjectsByType<BossPhaseController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            BossPhaseController member = controllers[i];
            if (member == null || member._phase != _phase)
            {
                continue;
            }

            if (member != this && member._owner != this)
            {
                continue;
            }

            if (member.runner != null)
            {
                member.runner.enabled = enabled;
            }
        }
    }

    private void ClearMembers(bool keepSelf)
    {
        for (int i = _members.Count - 1; i >= 0; i--)
        {
            BossPhaseController member = _members[i];
            if (member == null || (keepSelf && member == this))
            {
                continue;
            }

            Destroy(member.gameObject);
        }

        BossPhaseController[] ownedClones = FindObjectsByType<BossPhaseController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < ownedClones.Length; i++)
        {
            BossPhaseController clone = ownedClones[i];
            if (clone == null || clone == this || clone._owner != this)
            {
                continue;
            }

            clone.gameObject.SetActive(false);
            Destroy(clone.gameObject);
        }
    }

    private bool HasAlivePhaseMembers()
    {
        BossPhaseController[] controllers = FindObjectsByType<BossPhaseController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            BossPhaseController candidate = controllers[i];
            if (candidate == null || candidate._phase != _phase)
            {
                continue;
            }

            bool ownedByThis = candidate == this || candidate._owner == this;
            if (ownedByThis && !candidate._reportedDead)
            {
                return true;
            }
        }

        return false;
    }

    private BossPhaseController GetRoot()
    {
        BossPhaseController root = this;
        while (root._owner != null)
        {
            root = root._owner;
        }

        return root;
    }

    private void TrackMember(BossPhaseController member)
    {
        if (!IsRootController || member == null)
        {
            return;
        }

        if (!_members.Contains(member))
        {
            _members.Add(member);
        }

        SubscribeMemberHealth(member);
    }

    private void SubscribeMemberHealth(BossPhaseController member)
    {
        if (!IsRootController || member?.interaction == null)
        {
            return;
        }

        member.interaction.Damaged -= OnMemberHealthChanged;
        member.interaction.Died -= OnMemberHealthChanged;
        member.interaction.Damaged += OnMemberHealthChanged;
        member.interaction.Died += OnMemberHealthChanged;
    }

    private void OnMemberHealthChanged(int _)
    {
        RefreshAggregateHealth();
    }

    private void OnMemberHealthChanged()
    {
        RefreshAggregateHealth();
    }

    public void RefreshAggregateHealth()
    {
        if (!IsRootController)
        {
            _owner?.RefreshAggregateHealth();
            return;
        }

        int current = 0;
        int max = 0;
        BossPhaseController[] controllers = FindObjectsByType<BossPhaseController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            BossPhaseController member = controllers[i];
            if (member == null || member._phase != _phase)
            {
                continue;
            }

            if (member != this && member._owner != this)
            {
                continue;
            }

            if (member.interaction == null)
            {
                continue;
            }

            max += member.interaction.MaxHealth;
            current += member.IsDefeated ? 0 : member.interaction.CurrentHealth;
        }

        AggregateCurrentHealth = current;
        AggregateMaxHealth = max;
        AggregateHealthChanged?.Invoke(current, max);
    }

    private void ApplyDefeatedPresentation()
    {
        if (_defeated)
        {
            return;
        }

        _defeated = true;

        if (runner != null)
        {
            runner.enabled = false;
        }

        if (teleporter != null)
        {
            teleporter.enabled = false;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        StartCoroutine(FadeOutVisualsRoutine(0.45f));
    }

    private IEnumerator FadeOutVisualsRoutine(float duration)
    {
        _fadeRenderers.Clear();
        _fadeStartColors.Clear();

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            _fadeRenderers.Add(renderer);
            _fadeStartColors.Add(renderer.color);
        }

        float age = 0f;
        while (age < duration)
        {
            age += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(age / duration);
            for (int i = 0; i < _fadeRenderers.Count; i++)
            {
                SpriteRenderer renderer = _fadeRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Color color = _fadeStartColors[i];
                color.a = _fadeStartColors[i].a * alpha;
                renderer.color = color;
            }

            yield return null;
        }

        for (int i = 0; i < _fadeRenderers.Count; i++)
        {
            SpriteRenderer renderer = _fadeRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            Color color = _fadeStartColors[i];
            color.a = 0f;
            renderer.color = color;
            renderer.enabled = false;
        }
    }

    private void CacheArena()
    {
        BossBattleArena arena = FindFirstObjectByType<BossBattleArena>();
        _arenaBounds = arena != null ? arena.ArenaBounds : new Bounds(transform.position, new Vector3(16f, 9f, 0f));

        List<Transform> anchors = new();
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null && candidate.name.StartsWith("Anchor_"))
            {
                anchors.Add(candidate);
            }
        }

        anchors.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        _arenaAnchors = anchors.ToArray();
    }

    private Vector3 PointInArena(float normalizedX, float normalizedY)
    {
        Vector3 min = _arenaBounds.min;
        Vector3 max = _arenaBounds.max;
        return new Vector3(
            Mathf.Lerp(min.x, max.x, normalizedX),
            Mathf.Lerp(min.y, max.y, normalizedY),
            transform.position.z);
    }

    private void ApplyDarkBodyTint()
    {
        if (visualRenderer == null)
        {
            return;
        }

        visualRenderer.color = BossBodyVisual.DarkTint;
        if (interaction != null)
        {
            interaction.SetBaseVisualColor(BossBodyVisual.DarkTint);
        }
    }

    private SpriteRenderer ResolveVisualRenderer()
    {
        Transform visual = transform.Find("Visual");
        if (visual != null && visual.TryGetComponent(out SpriteRenderer renderer))
        {
            return renderer;
        }

        return GetComponentInChildren<SpriteRenderer>();
    }

    private void ConfigureHybridRoleVisuals(Role role)
    {
        _rolePrimary = RolePrimaryColor(role);
        _roleSecondary = RoleSecondaryColor(role);
        _visualClockOffset = role switch
        {
            Role.A => 0.33f,
            Role.B => 0.66f,
            Role.C => 0.99f,
            _ => 0f,
        };
        _idleFrameIndex = -1;
        _glitchTimer = 0f;
        EnsureHybridVisuals();
        ApplyHybridPalette(1f);
    }

    private void EnsureHybridVisuals()
    {
        if (!useHybridVisuals || visualRenderer == null || _hybridVisualsReady)
        {
            return;
        }

        Transform visual = visualRenderer.transform;
        _visualBaseLocalPosition = visual.localPosition;
        _visualBaseLocalScale = visual.localScale;

        _coreRenderer = EnsureEffectRenderer("Hybrid_Core", RuntimeSpriteUtility.CircleSprite, visual, 9);
        _haloRenderer = EnsureEffectRenderer("Hybrid_Halo", RuntimeSpriteUtility.RingSprite, visual, 8);
        _verticalLineRenderer = EnsureEffectRenderer("Hybrid_VerticalLine", RuntimeSpriteUtility.WhiteSprite, visual, 7);

        _coreRenderer.transform.localPosition = new Vector3(0f, -0.02f, 0f);
        _coreRenderer.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
        _haloRenderer.transform.localPosition = new Vector3(0f, 0.46f, 0f);
        _haloRenderer.transform.localScale = new Vector3(0.78f, 0.22f, 1f);
        _verticalLineRenderer.transform.localPosition = new Vector3(0f, -0.04f, 0f);
        _verticalLineRenderer.transform.localScale = new Vector3(0.025f, 0.95f, 1f);

        _glitchBars.Clear();
        for (int i = 0; i < 7; i++)
        {
            SpriteRenderer bar = EnsureEffectRenderer($"Hybrid_GlitchBar_{i:00}", RuntimeSpriteUtility.WhiteSprite, visual, 10 + i);
            _glitchBars.Add(bar);
        }

        _hybridVisualsReady = true;
        ConfigureHybridRoleVisuals(_role);
    }

    private SpriteRenderer EnsureEffectRenderer(string objectName, Sprite sprite, Transform parent, int sortingOrderOffset)
    {
        Transform child = parent.Find(objectName);
        if (child == null)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            child = go.transform;
        }

        if (!child.TryGetComponent(out SpriteRenderer renderer))
        {
            renderer = child.gameObject.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = sprite;
        renderer.sortingLayerID = visualRenderer.sortingLayerID;
        renderer.sortingLayerName = visualRenderer.sortingLayerName;
        renderer.sortingOrder = visualRenderer.sortingOrder + sortingOrderOffset;
        renderer.sharedMaterial = visualRenderer.sharedMaterial != null
            ? visualRenderer.sharedMaterial
            : RuntimeSpriteUtility.UnlitSpriteMaterial;
        return renderer;
    }

    private void UpdateHybridVisuals()
    {
        if (!useHybridVisuals || visualRenderer == null)
        {
            return;
        }

        EnsureHybridVisuals();

        float time = Time.time + _visualClockOffset;
        float pulse = 0.5f + Mathf.Sin(time * visualPulseSpeed) * 0.5f;
        Transform visual = visualRenderer.transform;
        visual.localPosition = _visualBaseLocalPosition + new Vector3(0f, Mathf.Sin(time * visualPulseSpeed * 0.7f) * idleBobAmplitude, 0f);
        visual.localScale = new Vector3(
            _visualBaseLocalScale.x * (1f + idleScalePulse * pulse),
            _visualBaseLocalScale.y * (1f + idleScalePulse * (1f - pulse)),
            _visualBaseLocalScale.z);

        UpdateIdleFrame(time);
        ApplyHybridPalette(pulse);

        _glitchTimer -= Time.deltaTime;
        if (_glitchTimer <= 0f)
        {
            UpdateGlitchBars(pulse);
            _glitchTimer = glitchInterval;
        }
    }

    private void UpdateIdleFrame(float time)
    {
        if (!useIdleFrameSprites || visualRenderer == null)
        {
            return;
        }

        Sprite[] frames = ResolveIdleFrames();
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        int frame = Mathf.FloorToInt(time / idleFrameDuration) % frames.Length;
        if (frame == _idleFrameIndex || frames[frame] == null)
        {
            return;
        }

        _idleFrameIndex = frame;
        visualRenderer.sprite = frames[frame];
    }

    private Sprite[] ResolveIdleFrames()
    {
        return _role switch
        {
            Role.A => cloneAIdleFrames,
            Role.B => cloneBIdleFrames,
            Role.C => cloneCIdleFrames,
            _ => p1IdleFrames,
        };
    }

    private void ApplyHybridPalette(float pulse)
    {
        SetRendererColor(_coreRenderer, Color.Lerp(_roleSecondary, Color.white, 0.18f), Mathf.Lerp(0.42f, 0.86f, pulse));
        SetRendererColor(_haloRenderer, _rolePrimary, Mathf.Lerp(0.28f, 0.68f, pulse));
        SetRendererColor(_verticalLineRenderer, _roleSecondary, Mathf.Lerp(0.28f, 0.58f, 1f - pulse));

        if (_coreRenderer != null)
        {
            float coreScale = Mathf.Lerp(0.24f, 0.34f, pulse);
            _coreRenderer.transform.localScale = new Vector3(coreScale, coreScale, 1f);
        }
        if (_haloRenderer != null)
        {
            _haloRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin((Time.time + _visualClockOffset) * 1.7f) * 4f);
            _haloRenderer.transform.localScale = new Vector3(Mathf.Lerp(0.74f, 0.86f, pulse), Mathf.Lerp(0.18f, 0.25f, pulse), 1f);
        }
    }

    private void UpdateGlitchBars(float pulse)
    {
        if (_glitchBars.Count == 0)
        {
            return;
        }

        UnityEngine.Random.State randomState = UnityEngine.Random.state;
        unchecked
        {
            int seed = gameObject.GetInstanceID() ^ Mathf.FloorToInt(Time.time / Mathf.Max(0.01f, glitchInterval));
            UnityEngine.Random.InitState(seed);
        }

        for (int i = 0; i < _glitchBars.Count; i++)
        {
            SpriteRenderer bar = _glitchBars[i];
            if (bar == null)
            {
                continue;
            }

            bool visible = UnityEngine.Random.value > 0.18f;
            bar.enabled = visible;
            if (!visible)
            {
                continue;
            }

            float side = UnityEngine.Random.value > 0.5f ? 1f : -1f;
            float y = UnityEngine.Random.Range(-0.48f, 0.5f);
            float x = side * UnityEngine.Random.Range(0.18f, 0.42f);
            float width = UnityEngine.Random.Range(0.12f, 0.34f);
            float height = UnityEngine.Random.Range(0.006f, 0.018f);
            bar.transform.localPosition = new Vector3(x, y, 0f);
            bar.transform.localScale = new Vector3(width, height, 1f);
            Color color = UnityEngine.Random.value > 0.5f ? _rolePrimary : _roleSecondary;
            SetRendererColor(bar, color, UnityEngine.Random.Range(0.18f, 0.5f) + pulse * 0.15f);
        }
        UnityEngine.Random.state = randomState;
    }

    private void SpawnSplitArrivalBurst(Vector3 position, Role role)
    {
        SpawnImpactBurst(position, RolePrimaryColor(role), RoleSecondaryColor(role), 14, 0.26f, 0.1f);
        SpawnShockwave(position, RolePrimaryColor(role), 0.28f, 0.46f, 2.6f, 0.66f);
    }

    private void SpawnImpactBurst(Vector3 position, Color primary, Color secondary, int count, float lifetime, float radius)
    {
        if (!useHybridVisuals)
        {
            return;
        }

        UnityEngine.Random.State randomState = UnityEngine.Random.state;
        unchecked
        {
            UnityEngine.Random.InitState(gameObject.GetInstanceID() ^ Mathf.RoundToInt(Time.time * 1000f) ^ count);
        }

        for (int i = 0; i < count; i++)
        {
            GameObject spark = new GameObject("Boss_ImpactSpark");
            spark.transform.position = position + new Vector3(UnityEngine.Random.Range(-radius, radius), UnityEngine.Random.Range(-radius, radius), 0f);
            spark.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            spark.transform.localScale = new Vector3(UnityEngine.Random.Range(0.08f, 0.28f), UnityEngine.Random.Range(0.006f, 0.018f), 1f);

            SpriteRenderer renderer = spark.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteUtility.WhiteSprite;
            renderer.sortingLayerName = "Effect";
            renderer.sortingOrder = 70 + i;
            renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
            Color color = UnityEngine.Random.value > 0.45f ? primary : secondary;
            color.a = UnityEngine.Random.Range(0.42f, 0.88f);
            renderer.color = color;

            BossEffectFade fade = spark.AddComponent<BossEffectFade>();
            fade.Begin(lifetime * UnityEngine.Random.Range(0.75f, 1.15f), shrinkOverLifetime: true);
        }

        UnityEngine.Random.state = randomState;
    }

    private void SpawnShockwave(Vector3 position, Color color, float startRadius, float lifetime, float expandMultiplier, float alpha)
    {
        if (!useHybridVisuals)
        {
            return;
        }

        GameObject shockwave = new GameObject("Boss_ShockwaveRing");
        shockwave.transform.position = position;

        SpriteRenderer renderer = shockwave.AddComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteUtility.RingSprite;
        renderer.sortingLayerName = "Effect";
        renderer.sortingOrder = 68;
        renderer.sharedMaterial = RuntimeSpriteUtility.UnlitSpriteMaterial;
        color.a = Mathf.Clamp01(alpha);
        renderer.color = color;

        float diameter = Mathf.Max(0.05f, startRadius * 2f);
        Vector3 spriteSize = renderer.sprite != null ? renderer.sprite.bounds.size : Vector3.one;
        shockwave.transform.localScale = new Vector3(
            diameter / Mathf.Max(0.0001f, spriteSize.x),
            diameter / Mathf.Max(0.0001f, spriteSize.y),
            1f);

        BossEffectFade fade = shockwave.AddComponent<BossEffectFade>();
        fade.Begin(lifetime, expandMultiplier);
    }

    private static void ShakeCamera(float strength, float duration)
    {
        SimpleCameraFollow cameraFollow = Camera.main != null ? Camera.main.GetComponent<SimpleCameraFollow>() : null;
        cameraFollow?.AddShake(strength, duration);
    }

    private void PulseScreenGlitch(float intensity, float duration)
    {
        glitchOverlay ??= FindFirstObjectByType<ScreenGlitchOverlay>();
        if (glitchOverlay != null)
        {
            StartCoroutine(glitchOverlay.Pulse(intensity, duration));
        }
    }

    private static void SetRendererColor(SpriteRenderer renderer, Color color, float alpha)
    {
        if (renderer == null)
        {
            return;
        }

        color.a = Mathf.Clamp01(alpha);
        renderer.color = color;
    }

    private static Color RolePrimaryColor(Role role)
    {
        return role switch
        {
            Role.A => new Color(1f, 0.36f, 0.08f, 1f),
            Role.B => new Color(0f, 0.86f, 1f, 1f),
            Role.C => new Color(0.72f, 0.28f, 1f, 1f),
            _ => new Color(0f, 0.88f, 1f, 1f),
        };
    }

    private static Color RoleSecondaryColor(Role role)
    {
        return role switch
        {
            Role.A => new Color(1f, 0.82f, 0.18f, 1f),
            Role.B => new Color(0.36f, 0.46f, 1f, 1f),
            Role.C => new Color(1f, 0.84f, 0.16f, 1f),
            _ => new Color(1f, 0.12f, 0.6f, 1f),
        };
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void TryLoadEditorIdleFrames()
    {
#if UNITY_EDITOR
        if (p1IdleFrames == null || p1IdleFrames.Length == 0)
        {
            p1IdleFrames = LoadEditorSpriteFrames(EditorIdleFramePaths[0]);
        }
        if (cloneAIdleFrames == null || cloneAIdleFrames.Length == 0)
        {
            cloneAIdleFrames = LoadEditorSpriteFrames(EditorIdleFramePaths[1]);
        }
        if (cloneBIdleFrames == null || cloneBIdleFrames.Length == 0)
        {
            cloneBIdleFrames = LoadEditorSpriteFrames(EditorIdleFramePaths[2]);
        }
        if (cloneCIdleFrames == null || cloneCIdleFrames.Length == 0)
        {
            cloneCIdleFrames = LoadEditorSpriteFrames(EditorIdleFramePaths[3]);
        }
#endif
    }

#if UNITY_EDITOR
    private static Sprite[] LoadEditorSpriteFrames(string path)
    {
        UnityEngine.Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        List<Sprite> sprites = new();
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
            {
                sprites.Add(sprite);
            }
        }

        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites.ToArray();
    }
#endif
}

public static class BossBodyVisual
{
    // 보스 PNG 자체에 검은 실루엣과 역할별 네온색을 굽기 때문에 런타임 틴트는 색을 죽이지 않는다.
    public static readonly Color DarkTint = Color.white;
}
