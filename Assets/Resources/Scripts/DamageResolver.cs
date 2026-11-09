using UnityEngine;

public static class DamageResolver
{
    public static bool ApplyDamage(
        GameObject target,
        float amount,
        GameObject attacker)
    {
        if (target == null || attacker == null || amount <= 0f)
            return false;

        if (target.transform.root == attacker.transform.root)
            return false;

        IDamageable damageable = FindDamageable(target);
        if (damageable == null || damageable.IsDead)
            return false;

        PlayerHealth targetPlayer = target.GetComponentInParent<PlayerHealth>();
        bool targetIsPlayer = targetPlayer != null;
        bool attackerIsPlayer = attacker.GetComponentInParent<PlayerHealth>() != null;
        if (targetIsPlayer && attackerIsPlayer &&
            !TraitorPlayer.IsTraitorObject(attacker) &&
            !TraitorPlayer.IsTraitorObject(target))
            return false;

        TraitorPlayer attackerTraitor = attacker.GetComponentInParent<TraitorPlayer>();
        if (attackerTraitor != null)
            amount *= attackerTraitor.DamageMultiplier;

        damageable.TakeDamage(amount);

        if (targetIsPlayer)
        {
            EnemyBoss boss = FindBoss(attacker);
            if (boss != null)
                targetPlayer.FlashHit(boss.AuraColor);
        }

        Debug.Log($"[DAMAGE] {attacker.name} causó {amount} de daño a {target.name}");
        return true;
    }

    public static IDamageable FindDamageable(GameObject target)
    {
        if (target == null)
            return null;

        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
            return damageable;

        NetworkEnemy networkEnemy = target.GetComponentInParent<NetworkEnemy>();
        return networkEnemy != null
            ? networkEnemy.GetComponentInChildren<IDamageable>(true)
            : null;
    }

    private static EnemyBoss FindBoss(GameObject attacker)
    {
        EnemyBoss boss = attacker.GetComponentInParent<EnemyBoss>();
        if (boss == null)
            boss = attacker.GetComponentInChildren<EnemyBoss>();

        return boss;
    }
}