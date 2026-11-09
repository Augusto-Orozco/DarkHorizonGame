
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BubbleProjectile : MonoBehaviour
{
    [SerializeField] private float damage = 10f;

    private GameObject owner;
    private Rigidbody rb;
    private float lifetime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;
    }

    public void Initialize(
        Vector3 direction,
        float speed,
        float playerSpeed,
        float duration,
        float projectileDamage,
        GameObject attacker)
    {
        owner = attacker;
        damage = projectileDamage;
        lifetime = duration;

        // La dirección depende exclusivamente de la mira y dispersión.
        Vector3 finalDirection = direction.normalized;

        // La velocidad actual del jugador funciona como multiplicador.
        float finalSpeed = speed * (1f + playerSpeed);

        rb.linearVelocity = finalDirection * finalSpeed;
    }

    private void Update()
    {
        lifetime -= Time.deltaTime;

        if (lifetime <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner == null)
            return;

        if (other.transform.root == owner.transform.root)
            return;

        Debug.Log($"[BUBBLE] Impacto detectado: {other.name}");

        bool hit = DamageResolver.ApplyDamage(
            other.gameObject,
            damage,
            owner
        );

        if (hit)
        {
            Debug.Log($"[BUBBLE] Daño aplicado: {damage}");

            EnemyHealth enemyHealth = DamageResolver.FindDamageable(other.gameObject) as EnemyHealth;
            if (enemyHealth != null)
                enemyHealth.GetComponent<EnemyHitFlash>()?.Flash();

            Destroy(gameObject);
        }
    }
}
