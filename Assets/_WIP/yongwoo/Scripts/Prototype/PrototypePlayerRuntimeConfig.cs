using UnityEngine;

[DisallowMultipleComponent]
public class PrototypePlayerRuntimeConfig : MonoBehaviour
{
    [SerializeField, HideInInspector] private PrototypePlayerConfig _config = new PrototypePlayerConfig();

    private SimplePlayerController _controller;
    private SimplePlayerCombat _combat;
    private PrototypeHealth _health;
    private BoxCollider2D _bodyCollider;
    private SimpleCameraFollow _cameraFollow;
    private Vector3 _spawnPosition;

    public void Initialize(
        SimplePlayerController controller,
        SimplePlayerCombat combat,
        PrototypeHealth health,
        BoxCollider2D bodyCollider,
        SimpleCameraFollow cameraFollow)
    {
        _controller = controller;
        _combat = combat;
        _health = health;
        _bodyCollider = bodyCollider;
        _cameraFollow = cameraFollow;
        _spawnPosition = transform.position;

        _config = PrototypePlayerConfigLoader.Sanitize(PrototypePlayerConfigLoader.DeepClone(_config));
        RefreshRuntimeConfig();
    }

    public void RefreshRuntimeConfig()
    {
        _config = PrototypePlayerConfigLoader.Sanitize(PrototypePlayerConfigLoader.DeepClone(_config));

        _controller ??= GetComponent<SimplePlayerController>();
        _combat ??= GetComponent<SimplePlayerCombat>();
        _health ??= GetComponent<PrototypeHealth>();
        _bodyCollider ??= GetComponent<BoxCollider2D>();

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
        _health?.ApplyPlayerConfig(_config.health);

        if (_health != null)
        {
            MonoBehaviour[] disableTargets = { _controller, _combat };
            _health.ConfigureRespawn(_spawnPosition, _config.health.respawnDelay, disableTargets);
            _health.SetRespawnEnabled(true);
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

    public void SetSerializedConfig(PrototypePlayerConfig config)
    {
        _config = PrototypePlayerConfigLoader.Sanitize(PrototypePlayerConfigLoader.DeepClone(config));
    }

    public PrototypePlayerConfig CreateConfigSnapshot()
    {
        PrototypePlayerConfig snapshot = new PrototypePlayerConfig();

        if (_controller != null)
        {
            snapshot.movement = _controller.CreateConfigSnapshot();
        }

        if (_combat != null)
        {
            snapshot.attack = _combat.CreateConfigSnapshot();
        }

        if (_health != null)
        {
            snapshot.health = _health.CreatePlayerConfigSnapshot();
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

        return PrototypePlayerConfigLoader.Sanitize(snapshot);
    }

    private void OnValidate()
    {
        _config = PrototypePlayerConfigLoader.Sanitize(PrototypePlayerConfigLoader.DeepClone(_config));

        if (!Application.isPlaying)
        {
            return;
        }

        RefreshRuntimeConfig();
    }
}
