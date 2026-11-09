
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

    [Header("Colision de camara")]
    public LayerMask collisionLayers;
    public float collisionRadius = 0.25f;
    public float collisionOffset = 0.15f;
    public float collisionReturnSpeed = 10f;


    public float collisionSmoothTime = 0.12f;
    public float sideCheckOffset = 0.25f;

    private float distanceVelocity;

    private float yaw;
    private float pitch = 15f;
    private bool isChildCamera;
    private Vector3 initialLocalPosition;

    private float currentDistance;

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

        currentDistance = Vector3.Distance(
            target.TransformPoint(new Vector3(0f, height, 0f)),
            target.TransformPoint(initialLocalPosition)
        );
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
            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);

            Vector3 originalWorldPosition =
                target.TransformPoint(initialLocalPosition);

            Vector3 pivotPosition =
                target.TransformPoint(new Vector3(0f, height, 0f));

            Vector3 direction =
                originalWorldPosition - pivotPosition;

            float originalDistance = direction.magnitude;

            if (originalDistance > 0.001f)
            {
                direction.Normalize();

                float targetDistance = originalDistance;

                // Comprobar el centro y ambos laterales
                Vector3 right = target.right;

                Vector3[] origins =
                {
                    pivotPosition,
                    pivotPosition + right * sideCheckOffset,
                    pivotPosition - right * sideCheckOffset
                };

                foreach (Vector3 origin in origins)
                {
                    if (Physics.SphereCast(
                        origin,
                        collisionRadius,
                        direction,
                        out RaycastHit hit,
                        originalDistance,
                        collisionLayers,
                        QueryTriggerInteraction.Ignore))
                    {
                        targetDistance = Mathf.Min(
                            targetDistance,
                            hit.distance - collisionOffset
                        );

                        Debug.DrawLine(origin, hit.point, Color.red);
                    }
                }

                targetDistance = Mathf.Max(targetDistance, 0.1f);

                // Evitar que la camara atraviese una pared.
                // La entrada en colision es inmediata por seguridad.
                if (targetDistance < currentDistance)
                {
                    currentDistance = targetDistance;
                    distanceVelocity = 0f;
                }
                else
                {
                    // Regresar suavemente a la posicion original
                    currentDistance = Mathf.SmoothDamp(
                        currentDistance,
                        originalDistance,
                        ref distanceVelocity,
                        collisionSmoothTime
                    );
                }

                Vector3 finalWorldPosition =
                    pivotPosition + direction * currentDistance;

                transform.position = finalWorldPosition;
            }

            return;
        }

        // Comportamiento original para camara independiente
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 cameraOffset =
            cameraRotation * new Vector3(0f, 0f, -distance);

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
    }


    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}
