using UnityEngine;

public class ShurkurMovement : MonoBehaviour, IPatrolArea
{
    private enum ShurkurState
    {
        Patrolling,
        Chasing,
        Spitting
    }

    [Header("Patrulla")]
    [SerializeField] private Transform patrolCenter;
    [SerializeField] private Vector2 patrolHalfExtents = new Vector2(15f, 15f);
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolTargetTolerance = 0.75f;

    [Header("Persecución")]
    [Tooltip("Capas que lo frenan (paredes, piso, props). Quita aquí la capa de los enemigos si se atoran entre ellos.")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private float losePlayerRange = 28f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 6f;

    [Header("Ataque")]
    [Tooltip("Altura sobre los pies del jugador a la que apunta (pecho)")]
    [SerializeField] private float aimHeight = 1.6f;
    [SerializeField] private GameObject spitPrefab;
    [SerializeField] private Transform spitSpawnPoint;
    [SerializeField] private float spitProjectileSpeed = 12f;
    [SerializeField] private float spitProjectileLifetime = 5f;
    [SerializeField] private float spitProjectileDamage = 15f;

    [SerializeField] private float spitDistance = 8f;
    [SerializeField] private float spitDistanceTolerance = 0.75f;
    [SerializeField] private float spitCooldown = 2.5f;
    [SerializeField] private float spitAnimationDuration = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float spitSpawnTime = 0.35f;

    [Header("Reacción a la luz (multiplica el cooldown)")]
    [Tooltip("A oscuras: menor = escupe más seguido (0.7 = 30% más rápido)")]
    [SerializeField] private float darkCooldownMultiplier = 0.7f;
    [Tooltip("Iluminado por un jugador solo")]
    [SerializeField] private float soloLightCooldownMultiplier = 1f;
    [Tooltip("Iluminado por 2 jugadores juntos")]
    [SerializeField] private float pairLightCooldownMultiplier = 1.5f;
    [Tooltip("Iluminado por 3 o más jugadores juntos")]
    [SerializeField] private float groupLightCooldownMultiplier = 2f;

    [Header("Animaciones")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleAnimation = "Rig_Shurkur|Idle_Hover";
    [SerializeField] private string moveAnimation = "Rig_Shurkur|Fly";
    [SerializeField] private string spitAnimation = "Rig_Shurkur|Split";

    private ShurkurState state;
    private Transform player;
    private EnemyTargeting targeting;
    private LightExposure lightExposure;
    private Collider bodyCollider;
    private Vector3 patrolTarget;
    private float nextSpitTime;
    private float spitTimer;
    private bool hasPatrolTarget;
    private bool spitProjectileSpawned;
    private bool ownsPatrolCenter;

    private void Awake()
    {
        bodyCollider = GetComponent<Collider>();

        targeting = GetComponent<EnemyTargeting>();
        if (targeting == null)
            targeting = gameObject.AddComponent<EnemyTargeting>();

        lightExposure = GetComponent<LightExposure>();
        if (lightExposure == null)
            lightExposure = gameObject.AddComponent<LightExposure>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (patrolCenter == null)
        {

            patrolCenter = new GameObject($"{name}_PatrolCenter").transform;
            patrolCenter.position = transform.position;
            ownsPatrolCenter = true;
        }

        SelectPatrolTarget();
        PlayAnimation(idleAnimation);
    }

    private void Update()
    {
        if (state == ShurkurState.Spitting)
        {
            UpdateSpitting();
            return;
        }

        Transform visiblePlayer = FindClosestPlayer();
        if (visiblePlayer != null)
        {
            player = visiblePlayer;
            state = ShurkurState.Chasing;
        }
        else if (player != null && DistanceTo(player) > losePlayerRange)
        {
            player = null;
            state = ShurkurState.Patrolling;
        }

        if (state == ShurkurState.Chasing)
            UpdateChasing();
        else
            UpdatePatrolling();
    }



    private float GetCooldownMultiplier()
    {
        int level = lightExposure.LightLevel;

        if (level <= 0)
            return darkCooldownMultiplier;
        if (level == 1)
            return soloLightCooldownMultiplier;
        if (level == 2)
            return pairLightCooldownMultiplier;
        return groupLightCooldownMultiplier;
    }



    private void UpdatePatrolling()
    {
        if (!hasPatrolTarget || IsOutsidePatrolArea() ||
            DistanceTo(patrolTarget) <= patrolTargetTolerance)
        {
            SelectPatrolTarget();
        }

        MoveTowards(patrolTarget, patrolSpeed, true);
        PlayAnimation(moveAnimation);
    }

    private void UpdateChasing()
    {
        if (player == null)
        {
            state = ShurkurState.Patrolling;
            return;
        }

        float distance = DistanceTo(player);
        if (distance > losePlayerRange)
        {
            player = null;
            state = ShurkurState.Patrolling;
            return;
        }

        float maximumDistance = spitDistance + spitDistanceTolerance;

        if (distance > maximumDistance)
        {
            MoveTowards(player.position, chaseSpeed, false);
            PlayAnimation(moveAnimation);
        }
        else
        {
            FaceTarget(player.position);
            PlayAnimation(idleAnimation);

            if (Time.time >= nextSpitTime)
                BeginSpitting();
        }
    }

    private void BeginSpitting()
    {
        state = ShurkurState.Spitting;
        spitTimer = 0f;
        spitProjectileSpawned = false;
        nextSpitTime = Time.time + spitCooldown * GetCooldownMultiplier();

        Debug.Log($"[{name}] Escupe. Luz = {lightExposure.LightLevel}, siguiente en {spitCooldown * GetCooldownMultiplier():0.00} s");

        FaceTarget(player.position);
        PlayAnimation(spitAnimation);
    }

    private void UpdateSpitting()
    {
        if (player == null)
        {
            state = ShurkurState.Patrolling;
            return;
        }

        FaceTarget(player.position);
        spitTimer += Time.deltaTime;

        if (!spitProjectileSpawned &&
            spitTimer >= spitAnimationDuration * spitSpawnTime)
        {
            Spit();
        }

        if (spitTimer >= spitAnimationDuration)
            state = ShurkurState.Chasing;
    }

    public void Spit()
    {
        if (spitProjectileSpawned)
            return;

        spitProjectileSpawned = true;

        if (spitPrefab == null || spitSpawnPoint == null || player == null)
        {
            Debug.LogWarning(
                "ShurkurMovement necesita Spit Prefab, Spit Spawn Point y un Player asignados para escupir.",
                this);
            return;
        }

        Vector3 aimPoint = player.position + Vector3.up * aimHeight;
        Vector3 direction = aimPoint - spitSpawnPoint.position;
        if (direction.sqrMagnitude <= 0.001f)
            return;

        GameObject projectile = Instantiate(
            spitPrefab,
            spitSpawnPoint.position,
            Quaternion.LookRotation(direction.normalized, Vector3.up));

        ShurkurSpitProjectile spit = projectile.GetComponent<ShurkurSpitProjectile>();
        if (spit == null)
            spit = projectile.AddComponent<ShurkurSpitProjectile>();

        spit.Initialize(direction,
            spitProjectileSpeed,
            spitProjectileLifetime,
            spitProjectileDamage,
            gameObject);
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

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
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

    private void PlayAnimation(string animationName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(animationName))
            return;

        int stateHash = Animator.StringToHash(animationName);
        if (!animator.HasState(0, stateHash))
        {
            if (animationName == spitAnimation)
            {
                Debug.LogWarning(
                    $"ShurkurMovement: el Animator no tiene el estado '{animationName}'. " +
                    "Agrega el clip Split como estado en la capa Base Layer.",
                    animator);
            }

            return;
        }

        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
            animator.Play(stateHash, 0, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        Transform centerTransform = patrolCenter == null ? transform : patrolCenter;
        Vector3 center = centerTransform.position;
        Vector3 size = new Vector3(patrolHalfExtents.x * 2f, 0.1f, patrolHalfExtents.y * 2f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spitDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }


    public void SetPatrolArea(Transform center, Vector2 halfExtents)
    {

        if (ownsPatrolCenter && patrolCenter != null)
            Destroy(patrolCenter.gameObject);

        ownsPatrolCenter = false;
        patrolCenter = center;
        patrolHalfExtents = halfExtents;
        hasPatrolTarget = false;  
    }
    private void OnDestroy()
    {
        if (ownsPatrolCenter && patrolCenter != null)
            Destroy(patrolCenter.gameObject);
    }
}