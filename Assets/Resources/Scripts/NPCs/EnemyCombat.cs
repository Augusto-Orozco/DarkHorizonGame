/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 28-10-2026 | 
|                                                                         |
| EnemyCombat.cs | Component that reads enemies AttackData, plays         |
| animations and registers impacts.                                       |
|                                                                         |
\*=======================================================================*/

using System.Collections;
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [SerializeField] private EnemyData data;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private Animator animator;

    private Transform player;
    private float nextAttackTime;
    private bool isAttacking;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        PlayerMovement movement =
            FindFirstObjectByType<PlayerMovement>();

        if (movement != null)
            player = movement.transform;
    }

    public bool TryAttack()
    {
        if (data == null || player == null || isAttacking)
            return false;

        if (Time.time < nextAttackTime)
            return false;

        AttackData attack = FindUsableAttack();

        if (attack == null)
            return false;

        StartCoroutine(ExecuteAttack(attack));
        return true;
    }

    private AttackData FindUsableAttack()
    {
        if (data.Attacks == null)
            return null;

        foreach (AttackData attack in data.Attacks)
        {
            if (attack == null)
                continue;

            if (Vector3.Distance(
                transform.position,
                player.position) <= attack.Range)
            {
                return attack;
            }
        }

        return null;
    }

    private IEnumerator ExecuteAttack(AttackData attack)
    {
        isAttacking = true;
        nextAttackTime = Time.time + attack.Cooldown;

        if (animator != null &&
            !string.IsNullOrEmpty(attack.AnimationTrigger))
        {
            animator.SetTrigger(attack.AnimationTrigger);
        }

        yield return new WaitForSeconds(attack.HitDelay);

        if (attack.Type == AttackType.Melee)
            ResolveMeleeAttack(attack);

        isAttacking = false;
    }

    private void ResolveMeleeAttack(AttackData attack)
    {
        Vector3 center = attackPoint != null
            ? attackPoint.position
            : transform.position + transform.forward;

        Collider[] hits = Physics.OverlapSphere(
            center,
            attack.Range,
            targetLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider hit in hits)
        {
            DamageResolver.ApplyDamage(
                hit.gameObject,
                attack.Damage,
                gameObject
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, 2f);
    }

        public void SetData(EnemyData newData)
    {
        data = newData;
    }
}
