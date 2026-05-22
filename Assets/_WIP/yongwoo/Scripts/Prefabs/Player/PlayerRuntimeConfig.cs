using UnityEngine;

// 역할:
// - PlayerConfig를 실제 플레이어 컴포넌트 묶음에 적용하는 브리지입니다.
// - 씬에서 조정한 값과 프리팹 저장 흐름 사이를 이어주는 조정 허브 역할을 합니다.
//
// 구조 포인트:
// - 개별 컴포넌트는 자기 책임만 가지고, 튜닝 묶음 조립은 이 파일이 맡습니다.

[DisallowMultipleComponent]
public class PlayerRuntimeConfig : MonoBehaviour
{
    [SerializeField, HideInInspector] private PlayerConfig _config = new PlayerConfig();
    [SerializeField, HideInInspector] private bool _spawnPositionInitialized;

    private SimplePlayerController _controller;
    private SimplePlayerCombat _combat;
    private PlayerInteraction _interaction;
    private CapsuleCollider2D _bodyCollider;
    private SimpleCameraFollow _cameraFollow;
    private Vector3 _spawnPosition;

    private void Awake()
    {
        AutoInitializeFromScene();
    }

    public void Initialize(
        SimplePlayerController controller,
        SimplePlayerCombat combat,
        PlayerInteraction interaction,
        CapsuleCollider2D bodyCollider,
        SimpleCameraFollow cameraFollow)
    {
        _controller = controller;
        _combat = combat;
        _interaction = interaction;
        _bodyCollider = bodyCollider;
        _cameraFollow = cameraFollow;
        _spawnPosition = transform.position;
        _spawnPositionInitialized = true;

        _config = PlayerConfigLoader.Sanitize(PlayerConfigLoader.DeepClone(_config));
        RefreshRuntimeConfig();
    }

    private void AutoInitializeFromScene()
    {
        _controller ??= GetComponent<SimplePlayerController>();
        _combat ??= GetComponent<SimplePlayerCombat>();
        _interaction ??= GetComponent<PlayerInteraction>();
        _bodyCollider ??= GetComponent<CapsuleCollider2D>();

        if (_cameraFollow == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                _cameraFollow = mainCamera.GetComponent<SimpleCameraFollow>();
            }
        }

        if (!_spawnPositionInitialized)
        {
            _spawnPosition = transform.position;
            _spawnPositionInitialized = true;
        }

        CaptureCurrentColliderConfig();
        CaptureCurrentHealthConfig();
        _config = PlayerConfigLoader.Sanitize(PlayerConfigLoader.DeepClone(_config));
        RefreshRuntimeConfig();
    }

    public void RefreshRuntimeConfig()
    {
        _config = PlayerConfigLoader.Sanitize(PlayerConfigLoader.DeepClone(_config));

        _controller ??= GetComponent<SimplePlayerController>();
        _combat ??= GetComponent<SimplePlayerCombat>();
        _interaction ??= GetComponent<PlayerInteraction>();
        _bodyCollider ??= GetComponent<CapsuleCollider2D>();

        if (_cameraFollow == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                _cameraFollow = mainCamera.GetComponent<SimpleCameraFollow>();
            }
        }

        _controller?.ApplyConfig(_config.movement);
        _combat?.ApplyConfig(_config.attack);
        _interaction?.ApplyHealthConfig(_config.health);

        if (_interaction != null)
        {
            MonoBehaviour[] disableTargets = { _controller, _combat };
            _interaction.ConfigureRespawn(_spawnPosition, _config.health.respawnDelay, disableTargets);
            _interaction.SetRespawnEnabled(true);
        }

        if (_bodyCollider != null)
        {
            _bodyCollider.size = new Vector2(_config.collider.width, _config.collider.height);
            _bodyCollider.offset = new Vector2(_config.collider.offsetX, _config.collider.offsetY);
            _bodyCollider.isTrigger = _config.collider.isTrigger;
        }

        if (_cameraFollow != null)
        {
            _cameraFollow.SetTarget(transform);
            _cameraFollow.ApplyConfig(_config.camera);
        }
    }

    public void SetSerializedConfig(PlayerConfig config)
    {
        _config = PlayerConfigLoader.Sanitize(PlayerConfigLoader.DeepClone(config));
    }

    public PlayerConfig CreateConfigSnapshot()
    {
        PlayerConfig snapshot = new PlayerConfig();

        if (_controller != null)
        {
            snapshot.movement = _controller.CreateConfigSnapshot();
        }

        if (_combat != null)
        {
            snapshot.attack = _combat.CreateConfigSnapshot();
        }

        if (_interaction != null)
        {
            snapshot.health = _interaction.CreateHealthConfigSnapshot();
        }

        if (_bodyCollider != null)
        {
            snapshot.collider = new PlayerColliderConfig
            {
                width = _bodyCollider.size.x,
                height = _bodyCollider.size.y,
                offsetX = _bodyCollider.offset.x,
                offsetY = _bodyCollider.offset.y,
                isTrigger = _bodyCollider.isTrigger
            };
        }

        if (_cameraFollow != null)
        {
            snapshot.camera = _cameraFollow.CreateConfigSnapshot();
        }

        return PlayerConfigLoader.Sanitize(snapshot);
    }

    private void CaptureCurrentColliderConfig()
    {
        if (_bodyCollider == null)
        {
            return;
        }

        _config ??= new PlayerConfig();
        _config.collider ??= new PlayerColliderConfig();
        _config.collider.width = _bodyCollider.size.x;
        _config.collider.height = _bodyCollider.size.y;
        _config.collider.offsetX = _bodyCollider.offset.x;
        _config.collider.offsetY = _bodyCollider.offset.y;
        _config.collider.isTrigger = _bodyCollider.isTrigger;
    }

    private void CaptureCurrentHealthConfig()
    {
        if (_interaction == null)
        {
            return;
        }

        _config ??= new PlayerConfig();
        _config.health = _interaction.CreateHealthConfigSnapshot();
    }

    private void OnValidate()
    {
        _bodyCollider ??= GetComponent<CapsuleCollider2D>();
        _interaction ??= GetComponent<PlayerInteraction>();
        CaptureCurrentColliderConfig();
        CaptureCurrentHealthConfig();
        _config = PlayerConfigLoader.Sanitize(PlayerConfigLoader.DeepClone(_config));

        if (!Application.isPlaying)
        {
            return;
        }

        RefreshRuntimeConfig();
    }
}
