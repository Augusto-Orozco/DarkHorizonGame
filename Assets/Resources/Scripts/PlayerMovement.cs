using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = -20f;

    [Header("Animadores del personaje")]
    public Animator[] animators;

    [Header("Nombres de animaciones")]
    public string idleAnimation = "Idle";
    public string preRunAnimation = "PreRun";
    public string runAnimation = "Run";
    public string postRunAnimation = "PostRun";

    [Header("Duraciones")]
    public float preRunDuration = 0.35f;
    public float postRunDuration = 0.35f;

    private CharacterController controller;
    private Vector3 verticalVelocity;

    private bool isMoving = false;
    private MovementState state = MovementState.Idle;
    private float animationTimer = 0f;

    private enum MovementState
    {
        Idle,
        PreRun,
        Run,
        PostRun
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();

        PlayAnimation(idleAnimation);
    }

    void Update()
    {
        HandleMovement();
        HandleGravity();
        HandleAnimations();
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

        // Solo W y S cuentan como movimiento para las animaciones
        isMoving = Mathf.Abs(forwardInput) > 0.01f;

        if (Keyboard.current.aKey.isPressed)
            turnInput -= 1f;

        if (Keyboard.current.dKey.isPressed)
            turnInput += 1f;

        // Girar mientras A o D esten presionadas.
        if (turnInput != 0f)
        {
            transform.Rotate(
                0f,
                turnInput * rotationSpeed * Time.deltaTime,
                0f
            );
        }

        // Avanzar / retroceder
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

                if (!isMoving)
                {
                    state = MovementState.PostRun;
                    animationTimer = 0f;

                    PlayAnimation(postRunAnimation);

                    break;
                }

                if (animationTimer >= preRunDuration)
                {
                    state = MovementState.Run;

                    PlayAnimation(runAnimation);
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

                if (isMoving)
                {
                    state = MovementState.PreRun;
                    animationTimer = 0f;

                    PlayAnimation(preRunAnimation);

                    break;
                }

                if (animationTimer >= postRunDuration)
                {
                    state = MovementState.Idle;

                    PlayAnimation(idleAnimation);
                }

                break;
        }
    }

    void PlayAnimation(string animationName)
    {
        foreach (Animator animator in animators)
        {
            if (animator != null)
            {
                animator.applyRootMotion = false;

                animator.CrossFade(
                    animationName,
                    0.1f
                );
            }
        }
    }
}