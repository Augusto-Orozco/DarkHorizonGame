using UnityEngine;

public class ShurkurMovement : MonoBehaviour
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
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private float losePlayerRange = 28f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 6f;

    [Header("Ataque")]
    [SerializeField] private GameObject spitPrefab;
    [SerializeField] private Transform spitSpawnPoint;
    [SerializeField] private float spitProjectileSpeed = 12f;
    [SerializeField] private float spitProjectileLifetime = 5f;
    [SerializeField] private float spitDistance = 8f;
    [SerializeField] private float spitDistanceTolerance = 0.75f;
    [SerializeField] private float spitCooldown = 2.5f;
    [SerializeField] private float spitAnimationDuration = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float spitSpawnTime = 0.35f;

    [Header("Animaciones")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleAnimation = "Rig_Shurkur|Idle_Hover";
    [SerializeField] private string moveAnimation = "Rig_Shurkur|Fly";
    [SerializeField] private string spitAnimation = "Rig_Shurkur|Split";

    private ShurkurState state;
    private Transform player;
    private Vector3 patrolTarget;
    private float nextSpitTime;
    private float spitTimer;
    private bool hasPatrolTarget;
    private bool spitProjectileSpawned;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (patrolCenter == null)
            patrolCenter = transform;

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
        nextSpitTime = Time.time + spitCooldown;
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

        Vector3 direction = player.position - spitSpawnPoint.position;
        if (direction.sqrMagnitude <= 0.001f)
            return;

        GameObject projectile = Instantiate(
            spitPrefab,
            spitSpawnPoint.position,
            Quaternion.LookRotation(direction.normalized, Vector3.up));

        ShurkurSpitProjectile spit = projectile.GetComponent<ShurkurSpitProjectile>();
        if (spit == null)
        {
            spit = projectile.AddComponent<ShurkurSpitProjectile>();
        }

        spit.Initialize(direction, spitProjectileSpeed, spitProjectileLifetime);
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
            Debug.LogError($"ShurkurMovement: el tag '{playerTag}' no existe.", this);
            Debug.LogException(exception, this);
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
}

public class ShurkurSpitProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float lifetime;

    public void Initialize(Vector3 launchDirection, float launchSpeed, float duration)
    {
        direction = launchDirection.normalized;
        speed = launchSpeed;
        lifetime = duration;
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
        lifetime -= Time.deltaTime;

        if (lifetime <= 0f)
            Destroy(gameObject);
    }
}
