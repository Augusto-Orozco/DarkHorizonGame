using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float ExternalSpeedMultiplier { get; set; } = 1f;

    public float gravity = -20f;

    [Header("Input System")]
    [SerializeField] private PlayerInput playerInput;

    [SerializeField] private float gamepadLookSensitivity = 180f;

    private InputAction moveAction;
    private InputAction lookAction;

    [Header("Pasos")]
    [SerializeField] private AudioClip normalFootstepClip;
    [SerializeField] private AudioClip metalFootstepClip;
    [SerializeField] private AudioSource footstepAudioSource;

    [Header("Volumen de pasos")]
    [SerializeField, Range(0f, 1f)]
    private float normalFootstepVolume = 0.3f;

    [SerializeField, Range(0f, 1f)]
    private float metalFootstepVolume = 0.2f;

    private AudioClip currentFootstepClip;

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
    private TraitorPlayer traitor;
    private float footstepTimer;

    private enum MovementState
    {
        Idle,
        PreRun,
        Run,
        PostRun,
        Shooting
    }

    private void InitializeInput()
    {
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (playerInput == null)
        {
            Debug.LogError("PlayerMovement necesita PlayerInput.", this);
            return;
        }

        moveAction = playerInput.actions.FindAction("Player/Move");
        lookAction = playerInput.actions.FindAction("Player/Look");
    }

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        InitializeInput();
        traitor = GetComponent<TraitorPlayer>();
        TraitorGameManager.Instance?.RegisterPlayer(this);

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

    private void Update()
    {
        if (readInput)
        {
            if (PauseMenuController.IsPaused)
            {
                isMoving = false; // sin input ni pasos en pausa
            }
            else
            {
                HandleMouseLook();
                HandleMovement();
            }

            // La gravedad (controller.Move) debe correr SIEMPRE: el prefab tiene un Rigidbody
            // junto al CharacterController y, sin este Move por frame, se desliza solo.
            HandleGravity();
        }

        HandleFootsteps();
        HandleAnimations();
        UpdateBodyAnimatorState();
    }

    private void HandleFootsteps()
    {
        if (footstepAudioSource == null)
        {
            footstepAudioSource = GetComponent<AudioSource>();

            if (footstepAudioSource == null)
            {
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
                footstepAudioSource.spatialBlend = 1f;
                footstepAudioSource.playOnAwake = false;
            }
        }

        if (controller == null || !isMoving)
        {
            footstepAudioSource.Stop();
            return;
        }

        AudioClip selectedClip = normalFootstepClip;
        float selectedVolume = normalFootstepVolume;

        // Detectar el suelo debajo del jugador
        if (Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            out RaycastHit hit,
            2f))
        {
            if (hit.collider.CompareTag("Nave"))
            {
                selectedClip = metalFootstepClip;
                selectedVolume = metalFootstepVolume;
            }
        }

        // Cambiar sonido si cambia la superficie
        if (currentFootstepClip != selectedClip)
        {
            currentFootstepClip = selectedClip;

            footstepAudioSource.Stop();
            footstepAudioSource.clip = currentFootstepClip;
        }

        footstepAudioSource.volume = selectedVolume;
        footstepAudioSource.loop = true;

        if (!footstepAudioSource.isPlaying && currentFootstepClip != null)
        {
            footstepAudioSource.Play();
        }
    }
    
    private void HandleMouseLook()
    {
        if (lookAction == null)
            return;

        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        if (lookInput.sqrMagnitude < 0.0001f)
            return;

        bool usingGamepad =
            lookAction.activeControl != null &&
            lookAction.activeControl.device is Gamepad;

        float sensitivity = usingGamepad
            ? gamepadLookSensitivity * GameSettings.Sensitivity * Time.deltaTime
            : mouseSensitivity * GameSettings.Sensitivity * Time.deltaTime;

        Vector2 lookDelta = lookInput * sensitivity;

        // Rotación horizontal del personaje.
        transform.Rotate(Vector3.up * lookDelta.x);

        // Rotación vertical de la cámara.
        if (cameraTransform != null && !cameraPitchHandledByThirdPersonCamera)
        {
            cameraPitch -= lookDelta.y * GameSettings.PitchSign;

            cameraPitch = Mathf.Clamp(
                cameraPitch,
                minPitch,
                maxPitch
            );

            cameraTransform.localRotation =
                Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        if (moveAction == null)
        {
            isMoving = false;
            return;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();

        float forwardInput = input.y;
        float strafeInput = input.x;

        isMoving = input.sqrMagnitude > 0.01f;

        if (!isMoving)
            return;

        Vector3 moveDirection =
            (transform.forward * forwardInput +
            transform.right * strafeInput).normalized;

        if (traitor == null)
            traitor = GetComponent<TraitorPlayer>();

        float speedMultiplier = traitor != null
            ? traitor.MovementSpeedMultiplier
            : 1f;

        controller.Move(
            moveDirection *
            moveSpeed *
            speedMultiplier *
            ExternalSpeedMultiplier *
            Time.deltaTime
        );
    }
    
    private void OnDisable()
    {
        TraitorGameManager.Instance?.UnregisterPlayer(this);
    }

    private void OnEnable()
    {
        TraitorGameManager.Instance?.RegisterPlayer(this);
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

        if (armAnimators == null)
            return;

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