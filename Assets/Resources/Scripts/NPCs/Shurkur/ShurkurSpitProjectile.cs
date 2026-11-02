
using UnityEngine;

public class ShurkurSpitProjectile : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private float projectileDamage = 10f;

    private GameObject owner;

    private Vector3 projectileDirection;
    private float projectileSpeed;
    private float projectileLifetime;

    public void Initialize(
        Vector3 projectileDirection,
        float projectileSpeed,
        float projectileLifetime,
        float projectileDamage,
        GameObject attacker)
    {
        owner = attacker;

        this.projectileDirection = projectileDirection.normalized;
        this.projectileSpeed = projectileSpeed;
        this.projectileLifetime = projectileLifetime;
        this.projectileDamage = projectileDamage;
    }

    private void Update()
    {
        transform.position +=
            projectileDirection * projectileSpeed * Time.deltaTime;

        projectileLifetime -= Time.deltaTime;

        if (projectileLifetime <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner == null)
            return;

        if (other.transform.root == owner.transform.root)
            return;

        Debug.Log($"[SHURKUR] Impacto detectado: {other.name}");

        bool hit = DamageResolver.ApplyDamage(
            other.gameObject,
            projectileDamage,
            owner
        );

        if (hit)
        {
            Debug.Log($"[SHURKUR] Daño aplicado: {projectileDamage}");
            Destroy(gameObject);
        }
    }
}
