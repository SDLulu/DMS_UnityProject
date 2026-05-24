using System.Collections.Generic;
using UnityEngine;

// 역할:
// - 보스 본체의 패턴 사이클을 돌립니다.
// - P1은 슬롯 6개(단발 / 연사 / 확산 / 대시베기 / 장판 / 예측탄)를 순환합니다. 보스전로직.md 기준.
// - 실제 발사체/장판은 각 패턴 컴포넌트가 들고 있고, 러너는 순서·간격·텔포 트리거만 담당합니다.

[DisallowMultipleComponent]
public class BossPatternRunner : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private BossInteraction interaction;
    [SerializeField] private BossTeleporter teleporter;
    [SerializeField] private Transform player;

    [Header("Patterns (P1)")]
    [Tooltip("순환할 패턴 슬롯. 보스전로직 P1 표 = 단발/연사/확산/대시베기/장판/예측탄 6개.")]
    [SerializeField] private MonoBehaviour[] patternSlots;

    [Header("Timing")]
    [Tooltip("패턴이 끝난 뒤 다음 패턴까지의 간격(초). 텔포 사이클 안에 흡수됩니다.")]
    [SerializeField, Min(0f)] private float interPatternDelay = 0.2f;
    [Tooltip("패턴이 끝난 뒤 텔포로 위치를 옮길지 여부.")]
    [SerializeField] private bool teleportBetweenPatterns = true;
    [Tooltip("시작 시 0.5s 뒤 첫 패턴 자동 시작.")]
    [SerializeField] private bool autoStart = true;
    [SerializeField, Min(0f)] private float startupDelay = 0.5f;

    private int _slotIndex;
    private float _stateTimer;
    private RunnerState _state = RunnerState.Idle;
    private IBossPattern _currentPattern;
    private bool _allowAutoPatternMerge = true;

    private enum RunnerState
    {
        Idle,
        Startup,
        WaitingPattern,
        BetweenPatterns,
        Teleporting,
    }

    public IBossPattern CurrentPattern => _currentPattern;

    public void SetPatternSlots(MonoBehaviour[] slots, bool allowAutoMerge = false)
    {
        patternSlots = slots;
        _slotIndex = 0;
        _allowAutoPatternMerge = allowAutoMerge;
    }

    public void RestartPatternLoop(float delay)
    {
        if (_currentPattern != null && _currentPattern.IsActive)
        {
            _currentPattern.EndPattern();
        }

        _currentPattern = null;
        _slotIndex = 0;
        if (!enabled)
        {
            enabled = true;
        }
        _state = RunnerState.Startup;
        _stateTimer = Mathf.Max(0f, delay);
    }

    private void Reset()
    {
        interaction = GetComponent<BossInteraction>();
        teleporter = GetComponent<BossTeleporter>();
    }

    private void Awake()
    {
        interaction ??= GetComponent<BossInteraction>();
        teleporter ??= GetComponent<BossTeleporter>();
        ResolvePlayer();
        EnsurePatternSlots();
    }

    private void OnEnable()
    {
        if (interaction != null)
        {
            interaction.Died += HandleBossDied;
        }

        if (autoStart)
        {
            _state = RunnerState.Startup;
            _stateTimer = startupDelay;
        }
    }

    private void OnDisable()
    {
        if (interaction != null)
        {
            interaction.Died -= HandleBossDied;
        }

        if (_currentPattern != null && _currentPattern.IsActive)
        {
            _currentPattern.EndPattern();
        }
        _currentPattern = null;
    }

    private void HandleBossDied()
    {
        _state = RunnerState.Idle;
        if (_currentPattern != null && _currentPattern.IsActive)
        {
            _currentPattern.EndPattern();
        }
        _currentPattern = null;
        enabled = false;
    }

    private void Update()
    {
        if (interaction != null && interaction.IsDead)
        {
            return;
        }

        switch (_state)
        {
            case RunnerState.Idle:
                return;

            case RunnerState.Startup:
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    StartNextPattern();
                }
                break;

            case RunnerState.WaitingPattern:
                _currentPattern?.TickPattern(Time.deltaTime);
                if (_currentPattern == null || !_currentPattern.IsActive)
                {
                    _state = RunnerState.BetweenPatterns;
                    _stateTimer = interPatternDelay;
                }
                break;

            case RunnerState.BetweenPatterns:
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                {
                    if (teleportBetweenPatterns && teleporter != null)
                    {
                        teleporter.HopToRandom();
                        _state = RunnerState.Teleporting;
                    }
                    else
                    {
                        StartNextPattern();
                    }
                }
                break;

            case RunnerState.Teleporting:
                if (teleporter == null || !teleporter.IsHopping)
                {
                    StartNextPattern();
                }
                break;
        }
    }

    private void StartNextPattern()
    {
        ResolvePlayer();
        EnsurePatternSlots();

        IBossPattern next = PickNextPattern();
        if (next == null)
        {
            _state = RunnerState.Idle;
            return;
        }

        _currentPattern = next;
        _currentPattern.BeginPattern(new BossPatternContext
        {
            boss = transform,
            player = player,
            interaction = interaction,
            teleporter = teleporter,
        });

        _state = RunnerState.WaitingPattern;
    }

    private IBossPattern PickNextPattern()
    {
        if (patternSlots == null || patternSlots.Length == 0)
        {
            return null;
        }

        int safety = patternSlots.Length;
        while (safety-- > 0)
        {
            MonoBehaviour candidate = patternSlots[_slotIndex];
            _slotIndex = (_slotIndex + 1) % patternSlots.Length;

            if (candidate is IBossPattern pattern)
            {
                return pattern;
            }
        }

        return null;
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        player = FindFirstObjectByType<PlayerInteraction>()?.transform;
    }

    private void EnsurePatternSlots()
    {
        if (!_allowAutoPatternMerge)
        {
            return;
        }

        MonoBehaviour[] attached = GetComponents<MonoBehaviour>();
        List<MonoBehaviour> merged = patternSlots != null
            ? new List<MonoBehaviour>(patternSlots)
            : new List<MonoBehaviour>();

        bool changed = false;
        for (int i = 0; i < attached.Length; i++)
        {
            MonoBehaviour candidate = attached[i];
            if (candidate == null || candidate == this || candidate is not IBossPattern)
            {
                continue;
            }

            if (merged.Contains(candidate))
            {
                continue;
            }

            merged.Add(candidate);
            changed = true;
        }

        if (changed || patternSlots == null)
        {
            patternSlots = merged.ToArray();
        }
    }
}
