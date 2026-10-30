using UnityEngine;

public class MariangulaMovement : MonoBehaviour
{
    private const string IdleStateName = "idle";
    private const string PreMoveStateName = "premov";
    private const string MoveStateName = "movimiento";
    private const string PostMoveStateName = "postmov";
    private const string AttackStateName = "ataque";

    private enum MariangulaState
    {
        Patrolling,
        Chasing,
        Attacking
    }

    private enum AnimationState
    {
        Idle,
        PreMove,
        Move,
        PostMove,
        Attack
    }

    [Header("Patrulla")]
    [SerializeField] private Transform patrolCenter;
    [SerializeField] private Vector2 patrolHalfExtents = new Vector2(15f, 15f);
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolTargetTolerance = 0.75f;

    [Header("Persecución")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private float losePlayerRange = 28f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 6f;

    [Header("Ataque cuerpo a cuerpo")]
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackRangeTolerance = 0.25f;
    [SerializeField] private float attackCooldown = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float attackHitTime = 0.45f;

    [Header("Animador")]
    [SerializeField] private Animator animator;

    private MariangulaState state;
    private AnimationState animationState = (AnimationState)(-1);
    private Transform player;
    private Vector3 patrolTarget;
    private float nextAttackTime;
    private float attackTimer;
    private float animationTimer;
    private float preMoveDuration = 0.35f;
    private float postMoveDuration = 0.35f;
    private float attackAnimationDuration = 1f;
    private string idleAnimation;
    private string preMoveAnimation;
    private string moveAnimation;
    private string postMoveAnimation;
    private string attackAnimation;
    private bool hasPatrolTarget;
    private bool hasProcessedAttack;
    private bool playerTagErrorLogged;
    private bool hasAnimationSetup;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (patrolCenter == null)
            patrolCenter = transform;

        if (animator != null)
            animator.SetLayerWeight(0, 1f);

        DetectAnimations();
        SelectPatrolTarget();
        SetAnimationState(AnimationState.Idle);
    }

    private void Update()
    {
        if (state == MariangulaState.Attacking)
        {
            UpdateAttacking();
            return;
        }

        Transform visiblePlayer = FindClosestPlayer();
        if (visiblePlayer != null)
        {
            player = visiblePlayer;
            state = MariangulaState.Chasing;
        }
        else if (player != null && DistanceTo(player) > losePlayerRange)
        {
            player = null;
            state = MariangulaState.Patrolling;
        }

        if (state == MariangulaState.Chasing)
            UpdateChasing();
        else
            UpdatePatrolling();
    }

    private void UpdatePatrolling()
    {
        if (!hasPatrolTarget || IsOutsidePatrolArea() ||
            DistanceTo(patrolTarget) <= patrolTargetTolerance)
        {
            SelectPatrolTarget();
        }

        MoveTowards(patrolTarget, patrolSpeed, true);
        UpdateMovementAnimation(true);
    }

    private void UpdateChasing()
    {
        if (player == null)
        {
            state = MariangulaState.Patrolling;
            return;
        }

        PlayerHealth playerHealth = player.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null && playerHealth.IsDead)
        {
            player = null;
            state = MariangulaState.Patrolling;
            return;
        }

        float distance = DistanceTo(player);
        if (distance > losePlayerRange)
        {
            player = null;
            state = MariangulaState.Patrolling;
            return;
        }

        float maximumAttackDistance = attackRange + attackRangeTolerance;
        if (distance > maximumAttackDistance)
        {
            MoveTowards(player.position, chaseSpeed, false);
            UpdateMovementAnimation(true);
        }
        else
        {
            FaceTarget(player.position);
            UpdateMovementAnimation(false);

            if (Time.time >= nextAttackTime)
                BeginAttacking();
        }
    }

    private void BeginAttacking()
    {
        state = MariangulaState.Attacking;
        attackTimer = 0f;
        hasProcessedAttack = false;
        nextAttackTime = Time.time + attackCooldown;
        FaceTarget(player.position);
        SetAnimationState(AnimationState.Attack);
    }

    private void UpdateAttacking()
    {
        if (player == null)
        {
            state = MariangulaState.Patrolling;
            return;
        }

        FaceTarget(player.position);
        attackTimer += Time.deltaTime;

        if (!hasProcessedAttack &&
            attackTimer >= attackAnimationDuration * attackHitTime)
        {
            Attack();
        }

        if (attackTimer >= attackAnimationDuration)
        {
            state = MariangulaState.Chasing;
            animationState = (AnimationState)(-1);
        }
    }

    public void Attack()
    {
        if (hasProcessedAttack)
            return;

        hasProcessedAttack = true;
    }

    private void DetectAnimations()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        idleAnimation = IdleStateName;
        preMoveAnimation = PreMoveStateName;
        moveAnimation = MoveStateName;
        postMoveAnimation = PostMoveStateName;
        attackAnimation = AttackStateName;

        if (string.IsNullOrEmpty(idleAnimation) ||
            !HasAnimatorState(idleAnimation) ||
            !HasAnimatorState(preMoveAnimation) ||
            string.IsNullOrEmpty(moveAnimation) ||
            !HasAnimatorState(moveAnimation) ||
            !HasAnimatorState(postMoveAnimation) ||
            string.IsNullOrEmpty(attackAnimation) ||
            !HasAnimatorState(attackAnimation))
        {
            Debug.LogWarning(
                "MariangulaMovement: no se encontraron los estados idle, premov, movimiento, postmov o ataque.",
                animator);
            return;
        }

        preMoveDuration = GetAnimationDuration(preMoveAnimation, preMoveDuration);
        postMoveDuration = GetAnimationDuration(postMoveAnimation, postMoveDuration);
        attackAnimationDuration = GetAnimationDuration(attackAnimation, attackAnimationDuration);
        hasAnimationSetup = true;
    }

    private bool HasAnimatorState(string stateName)
    {
        return GetAnimatorStateHash(stateName) != 0;
    }

    private int GetAnimatorStateHash(string stateName)
    {
        int stateHash = Animator.StringToHash(stateName);
        if (animator.HasState(0, stateHash))
            return stateHash;

        int fullPathHash = Animator.StringToHash("Base Layer." + stateName);
        return animator.HasState(0, fullPathHash) ? fullPathHash : 0;
    }

    private float GetAnimationDuration(string animationName, float fallbackDuration)
    {
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name == animationName)
                return clip.length;
        }

        return fallbackDuration;
    }

    private void SetAnimationState(AnimationState newState)
    {
        if (!hasAnimationSetup || animationState == newState)
            return;

        string animationName = idleAnimation;
        switch (newState)
        {
            case AnimationState.PreMove:
                animationName = preMoveAnimation;
                break;
            case AnimationState.Move:
                animationName = moveAnimation;
                break;
            case AnimationState.PostMove:
                animationName = postMoveAnimation;
                break;
            case AnimationState.Attack:
                animationName = attackAnimation;
                break;
        }

        animator.speed = 1f;
        animator.applyRootMotion = false;
        int stateHash = GetAnimatorStateHash(animationName);
        if (!HasAnimatorState(animationName))
        {
            Debug.LogWarning(
                $"MariangulaMovement: el estado '{animationName}' no existe en el Animator.",
                animator);
            return;
        }

        animator.Play(stateHash, 0, 0f);
        animationState = newState;
    }

    private void UpdateMovementAnimation(bool isMoving)
    {
        if (!hasAnimationSetup)
            return;

        if (isMoving)
        {
            if (animationState == AnimationState.Idle ||
                animationState == AnimationState.PostMove)
            {
                animationTimer = 0f;
                SetAnimationState(AnimationState.PreMove);
            }
            else if (animationState == AnimationState.PreMove)
            {
                animationTimer += Time.deltaTime;
                if (animationTimer >= preMoveDuration)
                {
                    animationTimer = 0f;
                    SetAnimationState(AnimationState.Move);
                }
            }
        }
        else
        {
            if (animationState == AnimationState.Move ||
                animationState == AnimationState.PreMove)
            {
                animationTimer = 0f;
                SetAnimationState(AnimationState.PostMove);
            }
            else if (animationState == AnimationState.PostMove)
            {
                animationTimer += Time.deltaTime;
                if (animationTimer >= postMoveDuration)
                {
                    animationTimer = 0f;
                    SetAnimationState(AnimationState.Idle);
                }
            }
        }
    }

    private Transform FindClosestPlayer()
    {
        if (string.IsNullOrWhiteSpace(playerTag) || detectionRange <= 0f)
            return null;

        GameObject[] players;
        try
        {
            players = GameObject.FindGameObjectsWithTag(playerTag);
        }
        catch (UnityException exception)
        {
            if (!playerTagErrorLogged)
            {
                Debug.LogError($"MariangulaMovement: el tag '{playerTag}' no existe.", this);
                Debug.LogException(exception, this);
                playerTagErrorLogged = true;
            }

            return null;
        }

        Transform closest = null;
        float closestDistanceSqr = detectionRange * detectionRange;

        foreach (GameObject candidate in players)
        {
            Vector3 offset = candidate.transform.position - transform.position;
            offset.y = 0f;
            float distanceSqr = offset.sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closest = candidate.transform;
            }
        }

        return closest;
    }

    private void SelectPatrolTarget()
    {
        Vector3 center = patrolCenter == null ? transform.position : patrolCenter.position;
        patrolTarget = new Vector3(
            Random.Range(center.x - patrolHalfExtents.x, center.x + patrolHalfExtents.x),
            transform.position.y,
            Random.Range(center.z - patrolHalfExtents.y, center.z + patrolHalfExtents.y));
        hasPatrolTarget = true;
    }

    private void MoveTowards(Vector3 target, float speed, bool constrainToPatrolArea)
    {
        if (constrainToPatrolArea)
            target = ClampToPatrolArea(target);

        Vector3 offset = target - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude <= 0.001f)
            return;

        FaceTarget(transform.position + offset);
        Vector3 nextPosition = transform.position + offset.normalized * speed * Time.deltaTime;
        transform.position = constrainToPatrolArea
            ? ClampToPatrolArea(nextPosition)
            : nextPosition;
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction.normalized, Vector3.up) *
            Quaternion.Euler(0f, -90f, 0f);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    private float DistanceTo(Vector3 target)
    {
        Vector3 offset = target - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private float DistanceTo(Transform target)
    {
        return target == null ? float.PositiveInfinity : DistanceTo(target.position);
    }

    private bool IsOutsidePatrolArea()
    {
        if (patrolCenter == null)
            return false;

        Vector3 offset = transform.position - patrolCenter.position;
        return Mathf.Abs(offset.x) > patrolHalfExtents.x ||
            Mathf.Abs(offset.z) > patrolHalfExtents.y;
    }

    private Vector3 ClampToPatrolArea(Vector3 position)
    {
        if (patrolCenter == null)
            return position;

        Vector3 center = patrolCenter.position;
        position.x = Mathf.Clamp(
            position.x,
            center.x - patrolHalfExtents.x,
            center.x + patrolHalfExtents.x);
        position.z = Mathf.Clamp(
            position.z,
            center.z - patrolHalfExtents.y,
            center.z + patrolHalfExtents.y);
        return position;
    }

    private void OnDrawGizmosSelected()
    {
        Transform centerTransform = patrolCenter == null ? transform : patrolCenter;
        Vector3 center = centerTransform.position;
        Vector3 size = new Vector3(patrolHalfExtents.x * 2f, 0.1f, patrolHalfExtents.y * 2f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
