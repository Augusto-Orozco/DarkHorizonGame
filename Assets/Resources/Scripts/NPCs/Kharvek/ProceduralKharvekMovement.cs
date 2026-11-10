using UnityEngine;

public class ProceduralKharvekMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 25f;
    [SerializeField] private float acceleration = 0.8f;
    [SerializeField] private float deceleration = 1f;
    [SerializeField] private float rotationSpeed = 0.8f;

    [Header("Persecution")]
    [SerializeField] private float detectionRange = 30f;
    [SerializeField] private float losePlayerRange = 120f;

    [Header("Walls")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private Collider wallCollider;

    [Header("Stop")]
    [SerializeField] private float stopDistance = 0f;

    [Header("Leash")]
    [SerializeField] private float leashRadius = 200f;

    [Header("Climbing")]
    [SerializeField] private float maxClimbHeight = 4f;
    [SerializeField] private float climbProbeDistance = 6f;

    [Header("Ground")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float bodyProbeHeight = 15f;
    [SerializeField] private float bodyProbeDistance = 40f;
    [SerializeField] private float bodyHeight = 5f;
    [SerializeField] private bool autoCalculateBodyHeight = true;
    [SerializeField] private float bodyLowering = 0f;
    [SerializeField] private float groundFollowSpeed = 1.2f;
    [SerializeField, Range(0f, 80f)] private float maxGroundSlope = 45f;
    [SerializeField] private float bodyBobAmount = 0.5f;
    [SerializeField] private float bodyBobSpeed = 0.005f;

    [Header("Leg IK Targets")]
    [SerializeField] private Transform[] legTargets = new Transform[4];
    [SerializeField] private float footProbeHeight = 15f;
    [SerializeField] private float footProbeDistance = 40f;
    [SerializeField] private float footOffset = 0.2f;
    [SerializeField] private float footFollowSpeed = 1.8f;
    [SerializeField] private float stepThreshold = 4.5f;
    [SerializeField] private float stepHeight = 2.5f;
    [SerializeField] private float stepDuration = 0.01f;
    [SerializeField] private float stepDistance = 5.5f;
    [SerializeField] private float stepForwardOffset = 5f;
    [SerializeField] private float stepLeadFromSpeed = 0.05f;
    [SerializeField] private float gaitInterval = 2.5f;

    public float SpeedMultiplier { get; set; } = 1f;

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
    private EnemyTargeting targeting;
    private Transform chaseTarget;
    private Vector3 lastNetworkPosition;
    private Vector3 spawnHome;
    private bool bodyHeightInitialized;

    private void Awake()
    {
        if (wallCollider == null)
            wallCollider = GetComponent<BoxCollider>();

        targeting = GetComponent<EnemyTargeting>();
        if (targeting == null)
            targeting = gameObject.AddComponent<EnemyTargeting>();

        if (!autoCalculateBodyHeight)
            bodyHeightInitialized = true;

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

        if (stopDistance <= 0f && wallCollider != null)
        {
            Bounds b = wallCollider.bounds;
            stopDistance = Mathf.Max(b.extents.x, b.extents.z) * 0.8f;
        }

        lastNetworkPosition = transform.position;
        spawnHome = transform.position;
    }

    private void CalculateBodyHeightFromStartPosition()
    {
        Vector3 origin = transform.position + Vector3.up * bodyProbeHeight;

        if (!TryGetGround(origin, bodyProbeDistance, out RaycastHit hit))
            return;

        bodyHeight = Mathf.Max(0f, transform.position.y - hit.point.y);
        bodyHeightInitialized = true;
    }

    private void Update()
    {
        if (NetworkEnemy.IsAuthority)
            MoveBody();
        else
            FollowNetworkMovement();
    }

    private void LateUpdate()
    {
        if (NetworkEnemy.IsAuthority)
        {
            if (autoCalculateBodyHeight && !bodyHeightInitialized)
                CalculateBodyHeightFromStartPosition();

            FitBodyToGround();
        }

        UpdateLegTargets();
    }

    private void FollowNetworkMovement()
    {
        Vector3 delta = transform.position - lastNetworkPosition;
        delta.y = 0f;
        lastNetworkPosition = transform.position;

        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        float measuredSpeed = delta.magnitude / dt;
        currentSpeed = Mathf.Lerp(currentSpeed, measuredSpeed, 10f * dt);
        isMoving = currentSpeed > moveSpeed * 0.05f;

        if (delta.sqrMagnitude > 0.000001f)
            lastMoveDirection = delta.normalized;

        distanceSinceStep += delta.magnitude;
        bodyBobPhase += delta.magnitude * bodyBobSpeed;
    }

    private void MoveBody()
    {
        bool isChasing = TryGetChaseDirection(out Vector3 chaseDirection);

        EnemyAudio enemyAudio = GetComponentInParent<EnemyAudio>();
        if (enemyAudio != null)
            enemyAudio.SetChasing(isChasing);
        if (isChasing)
        {
            lastMoveDirection = chaseDirection;

            Vector3 facingDirection = Vector3.ProjectOnPlane(chaseDirection, Vector3.up);
            if (facingDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(facingDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime);
            }
        }

        float speed = moveSpeed * SpeedMultiplier;
        TryApplyClimbStep();

        bool closeEnough = isChasing && chaseTarget != null && HorizontalDistance(chaseTarget.position) <= stopDistance;
        float targetSpeed = (isChasing && !closeEnough) ? speed : 0f;
        float speedChange = (isChasing ? acceleration : deceleration) * speed * Time.deltaTime;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChange);
        isMoving = currentSpeed > moveSpeed * 0.05f;

        LayerMask wallMask = obstacleMask & ~groundMask;
        EnemyMover.Move(transform, wallCollider, lastMoveDirection * (currentSpeed * Time.deltaTime), wallMask);
        distanceSinceStep += currentSpeed * Time.deltaTime;
        bodyBobPhase += currentSpeed * Time.deltaTime * bodyBobSpeed;
    }

    private bool TryGetChaseDirection(out Vector3 chaseDirection)
    {
        chaseDirection = Vector3.zero;

        Transform closestPlayer = targeting.FindClosestPlayer(detectionRange);
        if (closestPlayer != null)
        {
            chaseTarget = closestPlayer;
        }
        else if (chaseTarget != null)
        {
            PlayerHealth targetHealth = chaseTarget.GetComponentInParent<PlayerHealth>();
            bool targetDead = targetHealth != null && targetHealth.IsDead;

            if (targetDead || HorizontalDistance(chaseTarget.position) > losePlayerRange)
                chaseTarget = null;
        }

        if (chaseTarget != null && leashRadius > 0f && DistanceFromHome(chaseTarget.position) > leashRadius)
            chaseTarget = null;

        if (chaseTarget == null)
        {
            if (leashRadius > 0f)
            {
                Vector3 toHome = spawnHome - transform.position;
                toHome.y = 0f;
                if (toHome.magnitude > Mathf.Max(stopDistance, 5f))
                {
                    chaseDirection = toHome.normalized;
                    return true;
                }
            }

            return false;
        }

        Vector3 chaseOffset = chaseTarget.position - transform.position;
        chaseOffset.y = 0f;
        chaseDirection = chaseOffset.normalized;
        return true;
    }

    private void TryApplyClimbStep()
    {
        if (maxClimbHeight <= 0f || climbProbeDistance <= 0f || currentSpeed <= 0.1f)
            return;

        Vector3 forward = Vector3.ProjectOnPlane(lastMoveDirection, Vector3.up).normalized;
        if (!TryGetGround(transform.position + Vector3.up * bodyProbeHeight, bodyProbeDistance, out RaycastHit currentGround))
            return;

        Vector3 probePosition = transform.position + forward * climbProbeDistance + Vector3.up * bodyProbeHeight;
        if (!TryGetGround(probePosition, bodyProbeDistance, out RaycastHit aheadGround))
            return;

        float climb = aheadGround.point.y - currentGround.point.y;
        if (climb > 0f && climb <= maxClimbHeight)
            transform.position += Vector3.up * Mathf.Min(climb, currentSpeed * Time.deltaTime * 0.75f);
    }

    private bool TryGetGround(Vector3 origin, float distance, out RaycastHit groundHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance, groundMask, QueryTriggerInteraction.Ignore);
        groundHit = default;

        float closestDistance = float.PositiveInfinity;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform))
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                groundHit = hit;
            }
        }

        return !float.IsInfinity(closestDistance);
    }

    private float HorizontalDistance(Vector3 target)
    {
        Vector3 offset = target - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private float DistanceFromHome(Vector3 point)
    {
        Vector3 offset = point - spawnHome;
        offset.y = 0f;
        return offset.magnitude;
    }

    private void FitBodyToGround()
    {
        Vector3 origin = transform.position + Vector3.up * bodyProbeHeight;

        if (!TryGetGround(origin, bodyProbeDistance, out RaycastHit hit))
            return;

        Vector3 targetPosition = transform.position;
        float bob = isMoving ? Mathf.Sin(bodyBobPhase) * bodyBobAmount : 0f;
        targetPosition.y = hit.point.y + bodyHeight - bodyLowering + bob;
        transform.position = Vector3.Lerp(transform.position, targetPosition, groundFollowSpeed * Time.deltaTime);

        groundNormal = Vector3.Slerp(groundNormal, hit.normal, groundFollowSpeed * Time.deltaTime);
        groundNormal.y = Mathf.Max(groundNormal.y, Mathf.Cos(maxGroundSlope * Mathf.Deg2Rad));
        groundNormal.Normalize();

        Vector3 horizontalForward = Vector3.ProjectOnPlane(lastMoveDirection, Vector3.up);
        if (horizontalForward.sqrMagnitude < 0.001f)
            horizontalForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

        if (horizontalForward.sqrMagnitude > 0.001f)
        {
            Quaternion yawRotation = Quaternion.LookRotation(horizontalForward.normalized, Vector3.up);
            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, groundNormal) * yawRotation;
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

            if (!TryGetGround(rayOrigin, footProbeDistance, out RaycastHit hit))
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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnHome : transform.position, leashRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

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