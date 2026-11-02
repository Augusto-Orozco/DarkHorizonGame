/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 31-10-2026 | 
|                                                                         |
| KharvekContactDamage.cs | Detects a player contact or collision and     |
| applies damage to the player at certain set intervals.                  |
|                                                                         |
\*=======================================================================*/

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KharvekContactDamage : MonoBehaviour
{
    [Header("Contact Damage")]
    [SerializeField] private float damagePerHit = 15f;
    [SerializeField] private float damageInterval = 10f;

    private float nextDamageTime;

    private EnemyHealth enemyHealth;

    private void Awake()
    {
        enemyHealth = GetComponentInParent<EnemyHealth>();
    }

    // Cuando los colliders utilizan Is Trigger
    private void OnTriggerEnter(Collider other)
    {
        TryDamagePlayer(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamagePlayer(other.gameObject);
    }

    // Cuando los colliders NO utilizan Is Trigger
    private void OnCollisionEnter(Collision collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void TryDamagePlayer(GameObject target)
    {
        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        if (Time.time < nextDamageTime)
            return;

        // Comprobar que el objetivo realmente sea el jugador
        PlayerHealth playerHealth =
            target.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null || playerHealth.IsDead)
            return;

        bool damageApplied = DamageResolver.ApplyDamage(
            target,
            damagePerHit,
            transform.root.gameObject
        );

        if (damageApplied)
        {
            nextDamageTime = Time.time + damageInterval;

            Debug.Log(
                $"[KHARVEK] Contacto con jugador. " +
                $"Daño aplicado: {damagePerHit}"
            );
        }
    }
}