using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);
    [SerializeField] private float followSpeed = 7.5f;
    [SerializeField] private float horizontalLookAhead = 1.45f;
    [SerializeField] private float lookAheadSmoothing = 8f;
    [SerializeField] private float verticalFollowSpeed = 5.5f;

    private Rigidbody2D _targetBody;
    private float _currentLookAhead;

    public PlayerCameraConfig CreateConfigSnapshot()
    {
        return new PlayerCameraConfig
        {
            offset = new SerializableVector3(offset.x, offset.y, offset.z),
            followSpeed = followSpeed,
            horizontalLookAhead = horizontalLookAhead,
            lookAheadSmoothing = lookAheadSmoothing,
            verticalFollowSpeed = verticalFollowSpeed
        };
    }

    public void ApplyConfig(PlayerCameraConfig config)
    {
        config = PrototypePlayerConfigLoader.Sanitize(new PrototypePlayerConfig { camera = config }).camera;
        offset = config.offset.ToVector3();
        followSpeed = config.followSpeed;
        horizontalLookAhead = config.horizontalLookAhead;
        lookAheadSmoothing = config.lookAheadSmoothing;
        verticalFollowSpeed = config.verticalFollowSpeed;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        _targetBody = target != null ? target.GetComponent<Rigidbody2D>() : null;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                return;
            }

            target = playerObject.transform;
            _targetBody = target.GetComponent<Rigidbody2D>();
        }

        if (_targetBody == null && target != null)
        {
            _targetBody = target.GetComponent<Rigidbody2D>();
        }

        float horizontalVelocity = _targetBody != null ? _targetBody.linearVelocity.x : 0f;
        float lookAheadTarget = Mathf.Abs(horizontalVelocity) > 0.1f ? Mathf.Sign(horizontalVelocity) * horizontalLookAhead : 0f;
        _currentLookAhead = Mathf.Lerp(_currentLookAhead, lookAheadTarget, lookAheadSmoothing * Time.deltaTime);

        Vector3 desiredPosition = target.position + offset + new Vector3(_currentLookAhead, 0f, 0f);
        Vector3 currentPosition = transform.position;
        currentPosition.x = Mathf.Lerp(currentPosition.x, desiredPosition.x, followSpeed * Time.deltaTime);
        currentPosition.y = Mathf.Lerp(currentPosition.y, desiredPosition.y, verticalFollowSpeed * Time.deltaTime);
        currentPosition.z = desiredPosition.z;
        transform.position = currentPosition;
    }
}
