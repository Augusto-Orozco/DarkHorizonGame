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
        float duration,
        float projectileDamage,
        GameObject attacker)
    {
        owner = attacker;
        damage = projectileDamage;
        lifetime = duration;

        rb.linearVelocity = direction.normalized * speed;
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
            Destroy(gameObject);
        }
    }
}