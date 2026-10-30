/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 28-10-2026 | 
|                                                                         |
| DamageResolver.cs | Common entry point for all attacks. Any weapon,     |
| proyectile or NPC may invoke it. Resolves for basic damage.             |
|                                                                         |
\*=======================================================================*/

using UnityEngine;

public static class DamageResolver
{
    public static bool ApplyDamage(
        GameObject target,
        float amount,
        GameObject attacker)
    {
        if (target == null || attacker == null)
            return false;

        if (target.transform.root == attacker.transform.root)
            return false;

        IDamageable damageable =
            target.GetComponentInParent<IDamageable>();

        if (damageable == null || damageable.IsDead)
            return false;

        damageable.TakeDamage(amount);

        Debug.Log(
            $"{attacker.name} causó {amount} de daño a {target.name}"
        );

        return true;
    }
}