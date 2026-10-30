/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 30-10-2026 | 
|                                                                         |
| BubbleProjectile.cs| Manages and creates, moves and destroys the        |
| bubble projectiles. Register impacts and resolves for damage.           |
|                                                                         |
\*=======================================================================*/
// 


using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BubbleProjectile : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private float damage = 10f;

    private Rigidbody rb;
    private GameObject owner;
    private float lifetime;
    private bool initialized;
    private bubbleSpeed = speed * 1.5f;

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
        float speed //* 1.5f,
        float duration,
        float projectileDamage,
        GameObject attacker)
    {
        owner = attacker;
        lifetime = duration;
        damage = projectileDamage;

        rb.linearVelocity = direction.normalized * speed;

        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        lifetime -= Time.deltaTime;

        if (lifetime <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized || owner == null)
            return;

        // Evitar golpear al jugador que disparó.
        if (other.transform.root == owner.transform.root)
            return;

        bool hit = DamageResolver.ApplyDamage(
            other.gameObject,
            damage,
            owner
        );

        if (hit)
            Destroy(gameObject);
    }
}
