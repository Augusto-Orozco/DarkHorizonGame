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
        if (target == null || attacker == null || amount <= 0f)
            return false;

        IDamageable damageable =
            target.GetComponentInParent<IDamageable>();

        if (damageable == null || damageable.IsDead)
            return false;

        // Evita que un ataque dañe al propio atacante.
        if (target.transform.root == attacker.transform.root)
            return false;

        damageable.TakeDamage(amount);

        return true;
    }
}
