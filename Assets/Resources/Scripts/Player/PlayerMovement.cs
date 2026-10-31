using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float gravity = -20f;

    [Header("Cámara y Mouse")]
    [Tooltip("Asigna la Cámara (o el Pivot de la Cámara) que es hijo de este personaje.")]
    public Transform cameraTransform;
    public float mouseSensitivity = 15f;
    public float minPitch = -35f;
    public float maxPitch = 60f;

    [Header("Control")]
    [Tooltip("Si es false, no lee teclado ni mueve; otro script le dice si se está moviendo (ej. jugadores en red).")]
    public bool readInput = true;

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
    [Min(0.01f)] public float animationSpeed = 0.5f;

    private CharacterController controller;
    private Vector3 verticalVelocity;
    private float cameraPitch = 0f;
    private bool cameraPitchHandledByThirdPersonCamera;

    private bool isMoving = false;
    private MovementState state = MovementState.Idle;
    private float animationTimer = 0f;
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

        // Ocultar y bloquear el cursor en el centro de la pantalla
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        cameraPitchHandledByThirdPersonCamera =
            cameraTransform != null &&
            cameraTransform.GetComponentInParent<ThirdPersonCamera>() != null;

        preRunDuration = GetAnimationDuration(preRunAnimation, preRunDuration);
        postRunDuration = GetAnimationDuration(postRunAnimation, postRunDuration);
        shootingDuration = GetAnimationDuration(shootingAnimation, shootingDuration);

        PlayAnimation(idleAnimation);
    }

    void Update()
    {
        if (readInput)
        {
            HandleMouseLook();
            HandleMovement();
            HandleGravity();
        }

        HandleAnimations();
        UpdateBodyAnimatorState();
    }

    void HandleMouseLook()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity * Time.deltaTime;

        // 1. Girar el cuerpo entero del personaje horizontalmente con el mouse
        transform.Rotate(Vector3.up * mouseDelta.x);

        // 2. Inclinar la cámara verticalmente (Pitch)
        if (cameraTransform != null && !cameraPitchHandledByThirdPersonCamera)
        {
            cameraPitch -= mouseDelta.y;
            cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    void HandleMovement()
    {
        if (Keyboard.current == null)
            return;

        float forwardInput = 0f;
        float strafeInput = 0f;

        if (Keyboard.current.wKey.isPressed)
            forwardInput += 1f;
        if (Keyboard.current.sKey.isPressed)
            forwardInput -= 1f;

        if (Keyboard.current.aKey.isPressed)
            strafeInput -= 1f;
        if (Keyboard.current.dKey.isPressed)
            strafeInput += 1f;

        isMoving = (Mathf.Abs(forwardInput) > 0.01f || Mathf.Abs(strafeInput) > 0.01f);

        if (isMoving)
        {
            // Movimiento relativo a la orientación local del personaje
            Vector3 moveDirection = (transform.forward * forwardInput + transform.right * strafeInput).normalized;
            controller.Move(moveDirection * moveSpeed * Time.deltaTime);
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

        bodyAnimator.speed = animationSpeed;
        bodyAnimator.Play(animationName, 0, 0f);
        bodyAnimationState = state;
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

    public void SetMovingExternally(bool moving)
    {
        isMoving = moving;
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
            bodyAnimator.speed = animationSpeed;
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

        animator.speed = animationSpeed;
        animator.applyRootMotion = false;
        animator.CrossFade(animationName, 0.1f);
    }
}