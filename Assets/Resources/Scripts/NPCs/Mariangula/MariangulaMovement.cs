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
    [Tooltip("Capas que la frenan (paredes, piso, props). Quita aquí la capa de los enemigos si se atoran entre ellos.")]
    [SerializeField] private LayerMask obstacleMask = ~0;
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
    [SerializeField] private float attackDamage = 20f;

    [Header("Reacción a la luz")]
    [Tooltip("Segundos que retrocede cuando la luz la toca")]
    [SerializeField] private float fleeDuration = 0.8f;
    [SerializeField] private float fleeSpeed = 10f;
    [Tooltip("Segundos después de retroceder en los que la luz ya no la hace huir (solo la debilita)")]
    [SerializeField] private float fleeCooldown = 3f;
    [Tooltip("Daño extra si la ilumina un jugador solo (1.2 = +20%)")]
    [SerializeField] private float soloLightDamageMultiplier = 1.2f;
    [Tooltip("Daño extra si la ilumina una linterna de 2+ jugadores juntos (1.5 = +50%)")]
    [SerializeField] private float groupLightDamageMultiplier = 1.5f;

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
    private bool hasAnimationSetup;

    private EnemyTargeting targeting;
    private Collider bodyCollider;
    private LightExposure lightExposure;
    private EnemyHealth health;
    private float fleeUntilTime;
    private float nextFleeAllowedTime;
    private int lastLightLevel;
    private bool ownsPatrolCenter;

    private void Awake()
    {
        bodyCollider = GetComponent<Collider>();
        health = GetComponent<EnemyHealth>();

        lightExposure = GetComponent<LightExposure>();
        if (lightExposure == null)
            lightExposure = gameObject.AddComponent<LightExposure>();

        targeting = GetComponent<EnemyTargeting>();
        if (targeting == null)
            targeting = gameObject.AddComponent<EnemyTargeting>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (patrolCenter == null)
        {

            patrolCenter = new GameObject($"{name}_PatrolCenter").transform;
            patrolCenter.position = transform.position;
            ownsPatrolCenter = true;
        }

        if (animator != null)
            animator.SetLayerWeight(0, 1f);

        DetectAnimations();
        SelectPatrolTarget();
        SetAnimationState(AnimationState.Idle);
    }

    private void OnEnable()
    {
        lightExposure.OnLightLevelChanged += HandleLightLevelChanged;
    }

    private void OnDisable()
    {
        if (lightExposure != null)
            lightExposure.OnLightLevelChanged -= HandleLightLevelChanged;
    }

    private void Update()
    {
        if (state == MariangulaState.Attacking)
        {
            UpdateAttacking();
            return;
        }


        if (Time.time < fleeUntilTime)
        {
            UpdateFleeing();
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

   
    private void OnAnimatorMove() { }

    private void HandleLightLevelChanged(int level)
    {
        if (health != null)
        {
            if (level <= 0)
                health.DamageTakenMultiplier = 1f;
            else if (level == 1)
                health.DamageTakenMultiplier = soloLightDamageMultiplier;
            else
                health.DamageTakenMultiplier = groupLightDamageMultiplier;
        }

   
        bool justGotLit = lastLightLevel == 0 && level > 0;
        if (justGotLit && Time.time >= nextFleeAllowedTime)
        {
            fleeUntilTime = Time.time + fleeDuration;
            nextFleeAllowedTime = fleeUntilTime + fleeCooldown;
        }

        lastLightLevel = level;
    }

    private void UpdateFleeing()
    {
        Vector3 away = transform.position - lightExposure.LightSourcePosition;
        away.y = 0f;
        if (away.sqrMagnitude < 0.001f)
            away = -transform.forward;

        MoveTowards(transform.position + away.normalized * 10f, fleeSpeed, false);
        UpdateMovementAnimation(true);
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
            SetAnimationState(AnimationState.Idle);
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
            SetAnimationState(AnimationState.Idle);  
        }
    }

    public void Attack()
    {
        if (hasProcessedAttack)
            return;

        hasProcessedAttack = true;

        if (player == null)
            return;

 
        if (DistanceTo(player) > attackRange + attackRangeTolerance)
            return;

        DamageResolver.ApplyDamage(player.gameObject, attackDamage, gameObject);
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

        if (!HasAnimatorState(idleAnimation) ||
            !HasAnimatorState(preMoveAnimation) ||
            !HasAnimatorState(moveAnimation) ||
            !HasAnimatorState(postMoveAnimation) ||
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

        if (!HasAnimatorState(animationName))
        {
            Debug.LogWarning(
                $"MariangulaMovement: el estado '{animationName}' no existe en el Animator.",
                animator);
            return;
        }

        animator.speed = 1f;
        animator.applyRootMotion = true;  
        animator.Play(GetAnimatorStateHash(animationName), 0, 0f);
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
        return targeting.FindClosestPlayer(detectionRange);
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
        if (constrainToPatrolArea)
            nextPosition = ClampToPatrolArea(nextPosition);


        EnemyMover.Move(transform, bodyCollider, nextPosition - transform.position, obstacleMask);
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
        position.x = Mathf.Clamp(position.x, center.x - patrolHalfExtents.x, center.x + patrolHalfExtents.x);
        position.z = Mathf.Clamp(position.z, center.z - patrolHalfExtents.y, center.z + patrolHalfExtents.y);
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

    private void OnDestroy()
    {
        if (ownsPatrolCenter && patrolCenter != null)
            Destroy(patrolCenter.gameObject);
    }
}