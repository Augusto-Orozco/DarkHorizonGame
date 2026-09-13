using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;

    [Header("Posicion de la camara")]
    public float distance = 5f;
    public float height = 2f;

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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isChildCamera = transform.IsChildOf(target);

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
        HandleMouse();
        FollowPlayer();
    }

    void HandleMouse()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        pitch -= mouseDelta.y * sensitivityY;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void FollowPlayer()
    {
        if (isChildCamera)
        {
            transform.localPosition = initialLocalPosition;
            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            return;
        }

        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);

        // Posicion alrededor del personaje
        Vector3 cameraOffset =
            cameraRotation * new Vector3(0f, 0f, -distance);

        Vector3 targetPosition =
            target.position +
            Vector3.up * height +
            cameraOffset;

        // Seguir suavemente al personaje
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );

        transform.rotation = cameraRotation;
    }

    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}