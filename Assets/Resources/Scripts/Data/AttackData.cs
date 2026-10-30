/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 28-10-2026 | 
|                                                                         |
| AttackData.cs | ScriptableObject that defines the attack proporties and |
| behaviour of an enemy.                                                  |
|                                                                         |
\*=======================================================================*/

using UnityEngine;

public enum AttackType
{
    Melee,
    Projectile
}

[CreateAssetMenu(
    fileName = "NewAttack",
    menuName = "Dark Horizon/Combat/Attack Data"
)]
public class AttackData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string attackName;
    [SerializeField] private AttackType attackType;

    [Header("Damage and reach")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float range = 2f;
    [SerializeField] private float cooldown = 1.5f;
    [SerializeField] private float hitDelay = 0.3f;

    [Header("Animation")]
    [SerializeField] private string animationTrigger = "Attack";

    public string AttackName => attackName;
    public AttackType Type => attackType;
    public float Damage => damage;
    public float Range => range;
    public float Cooldown => cooldown;
    public float HitDelay => hitDelay;
    public string AnimationTrigger => animationTrigger;
}
