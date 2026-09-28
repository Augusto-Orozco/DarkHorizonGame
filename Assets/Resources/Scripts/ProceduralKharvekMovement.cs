using UnityEngine;
using UnityEngine.InputSystem;

public class ProceduralKharvekMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 250f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private Camera movementCamera;

    [Header("Ground")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float bodyProbeHeight = 150f;
    [SerializeField] private float bodyProbeDistance = 400f;
    [SerializeField] private float bodyHeight = 50f;
    [SerializeField] private bool autoCalculateBodyHeight = true;
    [SerializeField] private float bodyLowering = 0f;
    [SerializeField] private float groundFollowSpeed = 12f;
    [SerializeField] private float bodyBobAmount = 5f;
    [SerializeField] private float bodyBobSpeed = 0.05f;

    [Header("Leg IK Targets")]
    [SerializeField] private Transform[] legTargets = new Transform[4];
    [SerializeField] private float footProbeHeight = 150f;
    [SerializeField] private float footProbeDistance = 400f;
    [SerializeField] private float footOffset = 2f;
    [SerializeField] private float footFollowSpeed = 18f;
    [SerializeField] private float stepThreshold = 45f;
    [SerializeField] private float stepHeight = 25f;
    [SerializeField] private float stepDuration = 0.1f;
    [SerializeField] private float stepDistance = 55f;
    [SerializeField] private float stepForwardOffset = 50f;
    [SerializeField] private float stepLeadFromSpeed = 0.5f;
    [SerializeField] private float gaitInterval = 25f;

    private Vector3[] legHomePositions;
    private Vector3[] stepStartPositions;
    private Vector3[] stepTargetPositions;
    private Quaternion[] stepStartRotations;
    private Quaternion[] stepTargetRotations;
    private float[] stepProgress;
    private bool[] stepping;
    private Vector3 groundNormal = Vector3.up;
    private readonly int[] gaitOrder = { 0, 3, 1, 2 };
    private int nextGaitLeg;
    private int activeStepLeg = -1;
    private float distanceSinceStep;
    private bool isMoving;
    private float bodyBobPhase;
    private float currentSpeed;
    private Vector3 lastMoveDirection = Vector3.forward;

    private void Awake()
    {
        if (movementCamera == null)
            movementCamera = Camera.main;

        if (autoCalculateBodyHeight)
            CalculateBodyHeightFromStartPosition();

        int legCount = legTargets == null ? 0 : legTargets.Length;
        legHomePositions = new Vector3[legCount];
        stepStartPositions = new Vector3[legCount];
        stepTargetPositions = new Vector3[legCount];
        stepStartRotations = new Quaternion[legCount];
        stepTargetRotations = new Quaternion[legCount];
        stepProgress = new float[legCount];
        stepping = new bool[legCount];

        for (int i = 0; i < legCount; i++)
        {
            if (legTargets[i] == null)
                continue;

            legHomePositions[i] = transform.InverseTransformPoint(legTargets[i].position);
            stepStartPositions[i] = legTargets[i].position;
            stepTargetPositions[i] = legTargets[i].position;
            stepStartRotations[i] = legTargets[i].rotation;
            stepTargetRotations[i] = legTargets[i].rotation;
        }
    }

    private void CalculateBodyHeightFromStartPosition()
    {
        Vector3 origin = transform.position + Vector3.up * bodyProbeHeight;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, bodyProbeDistance, groundMask, QueryTriggerInteraction.Ignore))
            bodyHeight = Mathf.Max(0f, transform.position.y - hit.point.y);
    }

    private void Update()
    {
        MoveBody();
    }

    private void LateUpdate()
    {
        FitBodyToGround();
        UpdateLegTargets();
    }

    private void MoveBody()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        }

        bool hasInput = input.sqrMagnitude > 0.001f;
        float targetSpeed = hasInput ? moveSpeed : 0f;
        float speedChange = (hasInput ? acceleration : deceleration) * moveSpeed * Time.deltaTime;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChange);
        isMoving = currentSpeed > moveSpeed * 0.05f;

        if (hasInput)
        {
            input.Normalize();

            Vector3 forward = movementCamera == null ? Vector3.forward : movementCamera.transform.forward;
            Vector3 right = movementCamera == null ? Vector3.right : movementCamera.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            lastMoveDirection = (forward * input.y + right * input.x).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(lastMoveDirection, transform.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        transform.position += lastMoveDirection * (currentSpeed * Time.deltaTime);
        distanceSinceStep += currentSpeed * Time.deltaTime;
        bodyBobPhase += currentSpeed * Time.deltaTime * bodyBobSpeed;
    }

    private void FitBodyToGround()
    {
        Vector3 origin = transform.position + Vector3.up * bodyProbeHeight;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, bodyProbeDistance, groundMask, QueryTriggerInteraction.Ignore))
            return;

        Vector3 targetPosition = transform.position;
        float bob = isMoving ? Mathf.Sin(bodyBobPhase) * bodyBobAmount : 0f;
        targetPosition.y = hit.point.y + bodyHeight - bodyLowering + bob;
        transform.position = Vector3.Lerp(transform.position, targetPosition, groundFollowSpeed * Time.deltaTime);

        groundNormal = Vector3.Slerp(groundNormal, hit.normal, groundFollowSpeed * Time.deltaTime);
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, groundNormal);

        if (forward.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, groundNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, groundFollowSpeed * Time.deltaTime);
        }
    }

    private void UpdateLegTargets()
    {
        for (int i = 0; i < legTargets.Length; i++)
        {
            if (legTargets[i] == null)
                continue;

            Vector3 homePosition = transform.TransformPoint(legHomePositions[i]);
            Vector3 rayOrigin = homePosition + Vector3.up * footProbeHeight;

            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, footProbeDistance, groundMask, QueryTriggerInteraction.Ignore))
                continue;

            Vector3 desiredPosition = hit.point + hit.normal * footOffset;
            Vector3 desiredForward = Vector3.ProjectOnPlane(transform.forward, hit.normal);
            Quaternion desiredRotation = desiredForward.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(desiredForward.normalized, hit.normal)
                : legTargets[i].rotation;

            bool gaitStep = isMoving && distanceSinceStep >= gaitInterval && i == gaitOrder[nextGaitLeg];
            bool terrainStep = Vector3.Distance(legTargets[i].position, desiredPosition) > stepThreshold;

            if (!stepping[i] && CanStartStep(i) && (gaitStep || terrainStep))
            {
                stepping[i] = true;
                activeStepLeg = i;
                stepProgress[i] = 0f;
                stepStartPositions[i] = legTargets[i].position;
                float speedBasedLead = currentSpeed * stepDuration * stepLeadFromSpeed;
                float stepLead = Mathf.Max(stepDistance, stepForwardOffset, speedBasedLead);
                stepTargetPositions[i] = desiredPosition + (gaitStep ? transform.forward * stepLead : Vector3.zero);
                stepStartRotations[i] = legTargets[i].rotation;
                stepTargetRotations[i] = desiredRotation;

                if (gaitStep)
                {
                    distanceSinceStep = 0f;
                    nextGaitLeg = (nextGaitLeg + 1) % gaitOrder.Length;
                }
            }

            if (stepping[i])
            {
                float speedRatio = currentSpeed / Mathf.Max(1f, moveSpeed);
                float adjustedStepDuration = stepDuration / Mathf.Max(0.35f, speedRatio);
                stepProgress[i] += Time.deltaTime / Mathf.Max(0.01f, adjustedStepDuration);
                float progress = Mathf.Clamp01(stepProgress[i]);
                float smoothProgress = progress * progress * (3f - 2f * progress);
                Vector3 position = Vector3.Lerp(stepStartPositions[i], stepTargetPositions[i], smoothProgress);
                position += Vector3.up * (Mathf.Sin(progress * Mathf.PI) * stepHeight);
                legTargets[i].position = position;
                legTargets[i].rotation = Quaternion.Slerp(stepStartRotations[i], stepTargetRotations[i], smoothProgress);

                if (progress >= 1f)
                {
                    stepping[i] = false;
                    if (activeStepLeg == i)
                        activeStepLeg = -1;
                }
            }
            else
            {
                legTargets[i].position = Vector3.Lerp(legTargets[i].position, desiredPosition, footFollowSpeed * Time.deltaTime);
                legTargets[i].rotation = Quaternion.Slerp(legTargets[i].rotation, desiredRotation, footFollowSpeed * Time.deltaTime);
            }
        }
    }

    private bool CanStartStep(int legIndex)
    {
        for (int i = 0; i < stepping.Length; i++)
        {
            if (i != legIndex && stepping[i])
                return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position + Vector3.up * bodyProbeHeight, transform.position + Vector3.up * bodyProbeHeight + Vector3.down * bodyProbeDistance);

        if (legTargets == null)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < legTargets.Length; i++)
        {
            if (legTargets[i] == null)
                continue;

            Vector3 origin = legTargets[i].position + Vector3.up * footProbeHeight;
            Gizmos.DrawLine(origin, origin + Vector3.down * footProbeDistance);
        }
    }
}
