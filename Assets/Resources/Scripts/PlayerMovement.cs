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
    [Min(0.01f)] public float animationSpeed = 0.5f;

    private CharacterController controller;
    private Vector3 verticalVelocity;

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

        isMoving = Mathf.Abs(forwardInput) > 0.01f;

        // Girar mientras A o D esten presionadas.
        if (turnInput != 0f)
        {
            transform.Rotate(
                0f,
                turnInput * rotationSpeed * Time.deltaTime,
                0f
            );
        }

        if (forwardInput != 0f)
        {
            Vector3 movement =
                transform.forward *
                forwardInput *
                moveSpeed *
                Time.deltaTime;

            controller.Move(movement);
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