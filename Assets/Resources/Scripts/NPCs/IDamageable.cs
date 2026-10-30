/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 28-10-2026 | 
|                                                                         |
| IDamageable.cs | Interface that can be used for any player and NPC,     |
| declares if the object is dead and registers taken damage.              |
|                                                                         |
\*=======================================================================*/

public interface IDamageable
{
    bool IsDead { get; }
    void TakeDamage(float amount);
}
