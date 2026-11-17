using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;

    [Header("Posicion de la camara")]
    public float distance = 5f;
    public float height = 2f;
    public float shoulderOffset = 0.6f;

    [Header("Cambio de hombro")]
    public float leftShoulderX = 0f;
    public float shoulderSwapSpeed = 6f;
    public Key shoulderSwapKey = Key.V;

    [Header("Colision con paredes")]
    public LayerMask collisionMask = (1 << 0) | (1 << 6) | (1 << 7);
    public float collisionRadius = 0.25f;
    public float collisionPadding = 0.1f;
    public float pivotHeight = 0.6f;
    public float collisionReturnSpeed = 6f;

    [Header("Mouse")]
    public float sensitivityX = 0.15f;
    public float sensitivityY = 0.12f;

    [Header("Limites")]
    public float minPitch = -20f;
    public float maxPitch = 50f;

    [Header("Suavizado")]
    public float followSpeed = 15f;

    private float yaw;
    private float pitch = 15f;
    private bool isChildCamera;
    private Vector3 initialLocalPosition;
    private float currentSide = 1f;
    private PhotonView targetView;
    private CharacterController targetController;
    private float collisionFraction = 1f;

    [Header("Input System")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private float gamepadLookSensitivity = 120f;

    private InputAction lookAction;

    private float TargetSide => GameSettings.ShoulderLeft ? -1f : 1f;

    private bool IsLocal => !PhotonNetwork.InRoom || targetView == null || targetView.IsMine;

    void Start()
    {
        if (target == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();

            if (player != null)
                target = player.transform;
        }

        if (target == null)
        {
            Debug.LogError("ThirdPersonCamera necesita un target con PlayerMovement.");
            enabled = false;
            return;
        }

        targetView = target.GetComponent<PhotonView>();
        targetController = target.GetComponent<CharacterController>();

        if (playerInput == null)
            playerInput = target.GetComponent<PlayerInput>();

        if (playerInput == null)
        {
            Debug.LogError("ThirdPersonCamera necesita PlayerInput en el jugador.", this);
            enabled = false;
            return;
        }

        InputActionMap playerMap =
            playerInput.actions.FindActionMap("Player", true);

        lookAction = playerMap.FindAction("Look", true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isChildCamera = transform.IsChildOf(target);
        currentSide = TargetSide;

        if (isChildCamera)
        {
            initialLocalPosition = transform.localPosition;
            Vector3 localAngles = transform.localEulerAngles;

            yaw = localAngles.y;
            pitch = NormalizeAngle(localAngles.x);
        }
        else
        {
            yaw = target.eulerAngles.y;
        }
    }

    void LateUpdate()
    {
        HandleShoulderSwap();
        HandleMouse();
        FollowPlayer();
    }

    void HandleShoulderSwap()
    {
        if (IsLocal && !PauseMenuController.IsPaused && SwapPressed())
        {
            GameSettings.SetShoulderLeft(!GameSettings.ShoulderLeft);
            GameSettings.Save();
        }

        currentSide = Mathf.MoveTowards(currentSide, TargetSide, shoulderSwapSpeed * Time.deltaTime);
    }

    bool SwapPressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[shoulderSwapKey].wasPressedThisFrame)
            return true;

        Gamepad gamepad = Gamepad.current;
        return gamepad != null && gamepad.leftStickButton.wasPressedThisFrame;
    }

    void HandleMouse()
    {
        if (PauseMenuController.IsPaused || lookAction == null)
            return;

        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        if (Mathf.Abs(lookInput.y) < 0.01f)
            return;

        bool usingGamepad =
            lookAction.activeControl != null &&
            lookAction.activeControl.device is Gamepad;

        float lookDelta;

        if (usingGamepad)
        {
            lookDelta = lookInput.y *
                        gamepadLookSensitivity *
                        GameSettings.Sensitivity *
                        Time.deltaTime;
        }
        else
        {
            lookDelta = lookInput.y *
                        sensitivityY *
                        GameSettings.Sensitivity;
        }

        pitch -= lookDelta * GameSettings.PitchSign;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void FollowPlayer()
    {
        if (isChildCamera)
        {
            Vector3 localPosition = initialLocalPosition;
            localPosition.x = Mathf.Lerp(leftShoulderX, initialLocalPosition.x, (currentSide + 1f) * 0.5f);

            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            ApplyCollision();
            return;
        }

        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 cameraOffset =
            cameraRotation * new Vector3(shoulderOffset * currentSide, 0f, -distance);

        Vector3 targetPosition =
            target.position +
            Vector3.up * height +
            cameraOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );

        transform.rotation = cameraRotation;
        ApplyCollision();
    }

    void ApplyCollision()
    {
        Vector3 desired = transform.position;
        Vector3 origin = GetPivot();
        Vector3 offset = desired - origin;
        float fullDistance = offset.magnitude;

        if (fullDistance <= 0.001f)
            return;

        Vector3 direction = offset / fullDistance;
        float allowed = fullDistance;

        if (Physics.SphereCast(origin, collisionRadius, direction, out RaycastHit hit, fullDistance, collisionMask, QueryTriggerInteraction.Ignore))
            allowed = Mathf.Max(0f, hit.distance - collisionPadding);

        float targetFraction = allowed / fullDistance;

        if (targetFraction < collisionFraction)
            collisionFraction = targetFraction;
        else
            collisionFraction = Mathf.MoveTowards(collisionFraction, targetFraction, collisionReturnSpeed * Time.deltaTime);

        transform.position = origin + direction * (fullDistance * collisionFraction);
    }

    Vector3 GetPivot()
    {
        if (targetController != null)
        {
            Bounds bounds = targetController.bounds;
            return bounds.center + Vector3.up * (bounds.extents.y * pivotHeight);
        }

        return target.position + Vector3.up * height;
    }

    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}