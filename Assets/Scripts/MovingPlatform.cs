using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class MovingPlatform : MonoBehaviour
{
    public enum MotionType
    {
        Rotation,
        Vertical,
        Horizontal,
        HorizontalAndRotation
    }

    [SerializeField] private MotionType motion = MotionType.Vertical;
    [SerializeField, Min(0f)] private float distance = 1.25f;
    [SerializeField, Min(0.1f)] private float period = 4f;
    [SerializeField] private float degreesPerSecond = 35f;
    [SerializeField] private Vector3 horizontalDirection = Vector3.right;
    [SerializeField] private float phaseOffset;

    private Rigidbody body;
    private Vector3 originPosition;
    private Quaternion originRotation;
    private float elapsedTime;

    public MotionType Type => motion;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.None;
        originPosition = transform.position;
        originRotation = transform.rotation;
        elapsedTime = 0f;
    }

    private void FixedUpdate()
    {
        elapsedTime += Time.fixedDeltaTime;
        float wave = Mathf.Sin(elapsedTime * Mathf.PI * 2f / period + phaseOffset);
        switch (motion)
        {
            case MotionType.Vertical:
                body.MovePosition(originPosition + Vector3.up * (distance * wave));
                break;
            case MotionType.Horizontal:
            case MotionType.HorizontalAndRotation:
                Vector3 axis = horizontalDirection.sqrMagnitude > 0.001f
                    ? horizontalDirection.normalized
                    : Vector3.right;
                body.MovePosition(originPosition + axis * (distance * wave));
                break;
        }

        if (motion == MotionType.Rotation || motion == MotionType.HorizontalAndRotation)
        {
            body.MoveRotation(originRotation * Quaternion.Euler(0f, degreesPerSecond * elapsedTime, 0f));
        }
    }

    public void Configure(MotionType type, float travelDistance, float cycleSeconds, float rotationSpeed, float phase = 0f)
    {
        motion = type;
        distance = travelDistance;
        period = cycleSeconds;
        degreesPerSecond = rotationSpeed;
        phaseOffset = phase;
    }
}
