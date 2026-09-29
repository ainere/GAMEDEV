using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float moveSpeed = 6f;
    [SerializeField, Min(0.1f)] private float jumpHeight = 2.2f;
    [SerializeField] private float gravity = -24f;
    [SerializeField] private Transform view;
    [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.18f;
    [SerializeField, Range(-89f, 0f)] private float minimumPitch = -80f;
    [SerializeField, Range(0f, 89f)] private float maximumPitch = 80f;

    private CharacterController controller;
    private MovingPlatform platformContact;
    private MovingPlatform supportPlatform;
    private Vector3 lastSupportPosition;
    private Quaternion lastSupportRotation;
    private float verticalSpeed;
    private float pitch;
    private float initialPitch;
    private bool movementEnabled = true;
    private bool lookCaptured;

    public MovingPlatform CurrentSupport => supportPlatform;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (view == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            view = childCamera != null ? childCamera.transform : null;
        }
        initialPitch = view != null ? Mathf.DeltaAngle(0f, view.localEulerAngles.x) : 0f;
        pitch = initialPitch;
    }

    private void Start()
    {
        CaptureCursor();
    }

    private void OnDisable()
    {
        ReleaseCursor();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            ReleaseCursor();
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            ReleaseCursor();
        }
        else if (movementEnabled && mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            CaptureCursor();
        }

        if (!movementEnabled)
        {
            return;
        }

        if (lookCaptured && mouse != null && view != null)
        {
            Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(0f, delta.x, 0f, Space.World);
            pitch = Mathf.Clamp(pitch - delta.y, minimumPitch, maximumPitch);
            view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        Vector3 direction = Vector3.zero;
        if (keyboard != null)
        {
            float sideways = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float forward = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            direction = transform.right * sideways + transform.forward * forward;
            direction = Vector3.ClampMagnitude(direction, 1f);
        }

        bool grounded = controller.isGrounded || supportPlatform != null ||
                        (verticalSpeed <= 0f && ProbeGround(out _));
        bool jumped = keyboard != null && keyboard.spaceKey.wasPressedThisFrame && grounded;
        if (!jumped)
        {
            ApplySupportDisplacement();
        }
        if (grounded && verticalSpeed < 0f)
        {
            verticalSpeed = -2f;
        }

        if (jumped)
        {
            ClearSupport();
            verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalSpeed += gravity * Time.deltaTime;
        platformContact = null;
        CollisionFlags collisions = controller.Move((direction * moveSpeed + Vector3.up * verticalSpeed) * Time.deltaTime);

        bool nearGround = ProbeGround(out MovingPlatform nearbyPlatform);
        MovingPlatform support = platformContact != null ? platformContact : nearbyPlatform;
        if (!jumped && verticalSpeed <= 0f && support != null)
        {
            verticalSpeed = -2f;
            supportPlatform = support;
            lastSupportPosition = support.transform.position;
            lastSupportRotation = support.transform.rotation;
        }
        else if ((collisions & CollisionFlags.Below) != 0 || nearGround)
        {
            if (verticalSpeed < 0f)
            {
                verticalSpeed = -2f;
            }
            ClearSupport();
        }
        else
        {
            ClearSupport();
        }

    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y > 0.55f)
        {
            platformContact = hit.collider.GetComponentInParent<MovingPlatform>();
        }
    }

    private bool ProbeGround(out MovingPlatform support)
    {
        support = null;
        Vector3 origin = transform.position + Vector3.up * 0.3f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 0.65f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.55f)
        {
            support = hit.collider.GetComponentInParent<MovingPlatform>();
            return true;
        }
        return false;
    }

    private void ApplySupportDisplacement()
    {
        if (supportPlatform == null)
        {
            return;
        }

        Vector3 previousLocalPoint = Quaternion.Inverse(lastSupportRotation) *
                                     (transform.position - lastSupportPosition);
        Vector3 targetPosition = supportPlatform.transform.position +
                                 supportPlatform.transform.rotation * previousLocalPoint;
        controller.Move(targetPosition - transform.position);

    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        if (!enabled)
        {
            ClearSupport();
            ReleaseCursor();
        }
    }

    public void ConfigureView(Transform firstPersonView)
    {
        view = firstPersonView;
    }

    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        ClearSupport();
        controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        controller.enabled = true;
        verticalSpeed = 0f;
        platformContact = null;
        pitch = initialPitch;
        if (view != null)
        {
            view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void ClearSupport()
    {
        supportPlatform = null;
    }

    private void CaptureCursor()
    {
        lookCaptured = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ReleaseCursor()
    {
        lookCaptured = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
