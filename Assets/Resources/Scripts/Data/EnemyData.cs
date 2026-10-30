/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 28-10-2026 | 
|                                                                         |
| EnemyData.cs | ScriptableObject that serves as a template for each      |
| enemy type. Includes the prefab, statistics, and performable attacks.   |
|                                                                         |
\*=======================================================================*/


using UnityEngine;

[CreateAssetMenu(
    fileName = "NewEnemy",
    menuName = "Dark Horizon/Enemies/Enemy Data"
)]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string enemyName;
    [SerializeField] private GameObject enemyPrefab;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float movementSpeed = 5f;

    [Header("Combat")]
    [SerializeField] private AttackData[] attacks;

    public string EnemyName => enemyName;
    public GameObject EnemyPrefab => enemyPrefab;
    public float MaxHealth => maxHealth;
    public float DetectionRange => detectionRange;
    public float MovementSpeed => movementSpeed;
    public AttackData[] Attacks => attacks;
}
