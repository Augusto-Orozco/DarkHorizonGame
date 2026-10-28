using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = -20f;

    [Header("Animadores del personaje")]
    [Tooltip("Animator del cuerpo principal.")]
    public Animator bodyAnimator;

    [Tooltip("Animators independientes de los brazos.")]
    [FormerlySerializedAs("animators")]
    public Animator[] armAnimators;

    [Header("Nombres de animaciones")]
    public string idleAnimation = "Idle";
    public string preRunAnimation = "PreRun";
    public string runAnimation = "Run";
    public string postRunAnimation = "PostRun";
    public string shootingAnimation = "Shooting";

    [Header("Duraciones")]
    public float preRunDuration = 0.35f;
    public float postRunDuration = 0.35f;
    public float shootingDuration = 0.5f;

    [Header("Pies procedurales")]
    public Transform[] footTargets = new Transform[2];
    public LayerMask groundMask = ~0;
    public float footProbeHeight = 1.5f;
    public float footProbeDistance = 4f;
    public float footOffset = 0.03f;
    public float footFollowSpeed = 15f;
    public float stepThreshold = 0.35f;
    public float stepHeight = 0.2f;
    public float stepDuration = 0.18f;
    public float stepDistance = 0.45f;

    private CharacterController controller;
    private Vector3 verticalVelocity;

    private bool isMoving = false;
    private MovementState state = MovementState.Idle;
    private float animationTimer = 0f;
    private Vector3[] footHomePositions;
    private Vector3[] stepStartPositions;
    private Vector3[] stepTargetPositions;
    private float[] stepProgress;
    private bool[] stepping;
    private int nextFoot;
    private float distanceSinceStep;
    private bool warnedMissingGround;
    private float movementDirection = 1f;
    private MovementState bodyAnimationState = (MovementState)(-1);
    private bool hasBubbleGun;
    private bool shootingActive;

    private enum MovementState
    {
        Idle,
        PreRun,
        Run,
        PostRun,
        Shooting
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        InitializeFootTargets();

        if (footTargets == null || footTargets.Length < 2 || footTargets[0] == null || footTargets[1] == null)
            Debug.LogWarning("PlayerMovement necesita dos Foot Targets asignados para mover los pies proceduralmente.", this);

        preRunDuration = GetAnimationDuration(preRunAnimation, preRunDuration);
        postRunDuration = GetAnimationDuration(postRunAnimation, postRunDuration);
        shootingDuration = GetAnimationDuration(shootingAnimation, shootingDuration);

        PlayAnimation(idleAnimation);
    }

    void Update()
    {
        HandleMovement();
        HandleGravity();
        HandleAnimations();
        UpdateBodyAnimatorState();
    }

    void LateUpdate()
    {
        UpdateProceduralFeet();
    }

    void InitializeFootTargets()
    {
        int footCount = footTargets == null ? 0 : footTargets.Length;
        footHomePositions = new Vector3[footCount];
        stepStartPositions = new Vector3[footCount];
        stepTargetPositions = new Vector3[footCount];
        stepProgress = new float[footCount];
        stepping = new bool[footCount];

        for (int i = 0; i < footCount; i++)
        {
            if (footTargets[i] == null)
                continue;

            footHomePositions[i] = transform.InverseTransformPoint(footTargets[i].position);
            stepStartPositions[i] = footTargets[i].position;
            stepTargetPositions[i] = footTargets[i].position;
        }
    }

    void HandleMovement()
    {
        float forwardInput = 0f;
        float turnInput = 0f;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.wKey.isPressed)
            forwardInput = 1f;
        else if (Keyboard.current.sKey.isPressed)
            forwardInput = -1f;

        if (Keyboard.current.aKey.isPressed)
            turnInput -= 1f;

        if (Keyboard.current.dKey.isPressed)
            turnInput += 1f;

        // Solo avanzar o retroceder mueve los pies; A/D unicamente gira al personaje.
        isMoving = Mathf.Abs(forwardInput) > 0.01f;
        if (isMoving)
            movementDirection = Mathf.Sign(forwardInput);

        // Girar mientras A o D esten presionadas.
        if (turnInput != 0f)
        {
            transform.Rotate(
                0f,
                turnInput * rotationSpeed * Time.deltaTime,
                0f
            );
        }

        // Avanzar / retroceder. La rotacion lateral sigue siendo procedural.
        if (forwardInput != 0f)
        {
            Vector3 movement =
                transform.forward *
                forwardInput *
                moveSpeed *
                Time.deltaTime;

            controller.Move(movement);
            distanceSinceStep += movement.magnitude;
        }
    }
    void HandleGravity()
    {
        if (controller.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        verticalVelocity.y += gravity * Time.deltaTime;

        controller.Move(
            verticalVelocity * Time.deltaTime
        );
    }

    void UpdateBodyAnimatorState()
    {
        if (bodyAnimator == null)
            return;

        if (bodyAnimationState == state)
            return;

        string animationName = idleAnimation;

        switch (state)
        {
            case MovementState.PreRun:
                animationName = preRunAnimation;
                break;

            case MovementState.Run:
                animationName = runAnimation;
                break;

            case MovementState.PostRun:
                animationName = postRunAnimation;
                break;
        }

        bodyAnimator.speed = 1f;
        bodyAnimator.Play(animationName, 0, 0f);
        bodyAnimationState = state;
    }

    void UpdateProceduralFeet()
    {
        if (footTargets == null || footTargets.Length == 0 || footHomePositions == null)
            return;

        for (int i = 0; i < footTargets.Length; i++)
        {
            Transform footTarget = footTargets[i];
            if (footTarget == null)
                continue;

            Vector3 homePosition = transform.TransformPoint(footHomePositions[i]);
            Vector3 rayOrigin = homePosition + Vector3.up * footProbeHeight;

            Vector3 desiredPosition = homePosition;
            Quaternion desiredRotation = footTarget.rotation;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, footProbeDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                desiredPosition = hit.point + hit.normal * footOffset;
                Vector3 desiredForward = Vector3.ProjectOnPlane(transform.forward, hit.normal);
                if (desiredForward.sqrMagnitude > 0.001f)
                    desiredRotation = Quaternion.LookRotation(desiredForward.normalized, hit.normal);
            }
            else if (!warnedMissingGround)
            {
                Debug.LogWarning("PlayerMovement no encuentra el suelo. Revisa Ground Mask, Foot Probe Height y Foot Probe Distance.", this);
                warnedMissingGround = true;
            }

            float currentStepDistance = GetCurrentStepDistance();
            bool needsStep = isMoving && distanceSinceStep >= currentStepDistance
                && !stepping[i] && CanStartFootStep(i);

            if (needsStep)
            {
                stepping[i] = true;
                stepProgress[i] = 0f;
                stepStartPositions[i] = footTarget.position;
                stepTargetPositions[i] = desiredPosition + transform.forward * (currentStepDistance * movementDirection);
                distanceSinceStep = 0f;
                nextFoot = (i + 1) % footTargets.Length;
            }

            if (stepping[i])
            {
                stepProgress[i] += Time.deltaTime / Mathf.Max(0.01f, stepDuration);
                float progress = Mathf.Clamp01(stepProgress[i]);
                float smoothProgress = progress * progress * (3f - 2f * progress);
                Vector3 position = Vector3.Lerp(stepStartPositions[i], stepTargetPositions[i], smoothProgress);
                position += Vector3.up * (Mathf.Sin(progress * Mathf.PI) * stepHeight);
                footTarget.position = position;
                footTarget.rotation = Quaternion.Slerp(footTarget.rotation, desiredRotation, footFollowSpeed * Time.deltaTime);

                if (progress >= 1f)
                    stepping[i] = false;
            }
            else if (!isMoving)
            {
                footTarget.position = Vector3.Lerp(footTarget.position, desiredPosition, footFollowSpeed * Time.deltaTime);
                footTarget.rotation = Quaternion.Slerp(footTarget.rotation, desiredRotation, footFollowSpeed * Time.deltaTime);
            }
        }
    }

    float GetCurrentStepDistance()
    {
        float speedBasedDistance = moveSpeed * stepDuration;
        return Mathf.Max(0.05f, Mathf.Max(stepDistance, speedBasedDistance));
    }

    bool CanStartFootStep(int footIndex)
    {
        if (footIndex != nextFoot)
            return false;

        for (int i = 0; i < stepping.Length; i++)
        {
            if (i != footIndex && stepping[i])
                return false;
        }

        return true;
    }

    void HandleAnimations()
    {
        switch (state)
        {
            case MovementState.Idle:

                if (isMoving)
                {
                    state = MovementState.PreRun;
                    animationTimer = 0f;

                    PlayAnimation(preRunAnimation);
                }

                break;

            case MovementState.PreRun:

                animationTimer += Time.deltaTime;

                if (animationTimer >= preRunDuration)
                {
                    state = isMoving ? MovementState.Run : MovementState.PostRun;
                    animationTimer = 0f;

                    PlayAnimation(state == MovementState.Run ? runAnimation : postRunAnimation);
                }

                break;

            case MovementState.Run:

                if (!isMoving)
                {
                    state = MovementState.PostRun;
                    animationTimer = 0f;

                    PlayAnimation(postRunAnimation);
                }

                break;

            case MovementState.PostRun:

                animationTimer += Time.deltaTime;

                if (animationTimer >= postRunDuration)
                {
                    state = isMoving ? MovementState.PreRun : MovementState.Idle;
                    animationTimer = 0f;

                    PlayAnimation(state == MovementState.PreRun ? preRunAnimation : idleAnimation);
                }

                break;

            case MovementState.Shooting:

                if (shootingActive)
                    break;

                animationTimer += Time.deltaTime;
                if (animationTimer >= shootingDuration)
                {
                    state = MovementState.Idle;
                    PlayAnimation(idleAnimation);
                }

            break;
        }
    }

    public void EquipBubbleGun()
    {
        hasBubbleGun = true;
    }

    public bool HasBubbleGun()
    {
        return hasBubbleGun;
    }

    public void StartShooting()
    {
        if (!hasBubbleGun || state == MovementState.Shooting)
            return;

        state = MovementState.Shooting;
        animationTimer = 0f;
        PlayAnimation(shootingAnimation);
    }

    public void SetShootingActive(bool active)
    {
        shootingActive = active;

        if (active)
        {
            if (state != MovementState.Shooting)
            {
                state = MovementState.Shooting;
                animationTimer = 0f;
                PlayAnimation(shootingAnimation);
            }
        }
    }

    float GetAnimationDuration(string animationName, float fallbackDuration)
    {
        if (string.IsNullOrEmpty(animationName))
            return fallbackDuration;

        Animator[] sourceAnimators = armAnimators;
        if (bodyAnimator != null)
        {
            sourceAnimators = new Animator[1] { bodyAnimator };
        }

        if (sourceAnimators == null)
            return fallbackDuration;

        foreach (Animator animator in sourceAnimators)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                continue;

            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip != null && clip.name == animationName)
                    return clip.length;
            }
        }

        return fallbackDuration;
    }

    void PlayAnimation(string animationName)
    {
        if (bodyAnimator != null && !isMoving)
        {
            bodyAnimator.speed = 1f;
            bodyAnimator.Play(idleAnimation, 0, 0f);
        }

        if (bodyAnimator == null && (armAnimators == null || armAnimators.Length == 0))
        {
            return;
        }

        foreach (Animator animator in armAnimators)
        {
            PlayAnimationOnAnimator(animator, animationName);
        }
    }

    void PlayAnimationOnAnimator(Animator animator, string animationName)
    {
        if (animator == null || string.IsNullOrEmpty(animationName))
            return;

        animator.applyRootMotion = false;
        animator.CrossFade(animationName, 0.1f);
    }
}