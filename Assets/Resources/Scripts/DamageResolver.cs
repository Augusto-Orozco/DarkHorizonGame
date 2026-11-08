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

        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable == null || damageable.IsDead)
            return false;

        bool targetIsPlayer = target.GetComponentInParent<PlayerHealth>() != null;
        bool attackerIsPlayer = attacker.GetComponentInParent<PlayerHealth>() != null;
        if (targetIsPlayer && attackerIsPlayer &&
            !TraitorPlayer.IsTraitorObject(attacker) &&
            !TraitorPlayer.IsTraitorObject(target))
            return false;

        TraitorPlayer attackerTraitor = attacker.GetComponentInParent<TraitorPlayer>();
        if (attackerTraitor != null)
            amount *= attackerTraitor.DamageMultiplier;

        damageable.TakeDamage(amount);

        Debug.Log($"[DAMAGE] {attacker.name} causó {amount} de daño a {target.name}");
        return true;
    }
}