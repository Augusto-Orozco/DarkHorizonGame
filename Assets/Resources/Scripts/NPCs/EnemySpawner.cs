/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 28-10-2026 | 
|                                                                         |
| EnemySpawner.cs | Separes the enemy definition from the colocation      |
| first describes the type of creature, then establishes how many         |
| instances to create and where.                                          |
|                                                                         |
\*=======================================================================*/

using System;
using UnityEngine;

[Serializable]
public class EnemySpawnEntry
{
    public EnemyData enemyData;
    public Transform[] spawnPoints;
}

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemySpawnEntry[] enemies;
    [SerializeField] private bool spawnOnStart = true;

    private void Start()
    {
        if (spawnOnStart)
            SpawnAll();
    }

    public void SpawnAll()
    {
        if (enemies == null)
            return;

        foreach (EnemySpawnEntry entry in enemies)
        {
            if (entry == null ||
                entry.enemyData == null ||
                entry.enemyData.EnemyPrefab == null ||
                entry.spawnPoints == null)
            {
                continue;
            }

            foreach (Transform point in entry.spawnPoints)
            {
                if (point == null)
                    continue;

                GameObject instance = Instantiate(
                    entry.enemyData.EnemyPrefab,
                    point.position,
                    point.rotation
                );

                ConfigureEnemy(instance, entry.enemyData);
            }
        }
    }

    private void ConfigureEnemy(
        GameObject instance,
        EnemyData data)
    {
        EnemyHealth health = instance.GetComponent<EnemyHealth>();
        EnemyCombat combat = instance.GetComponent<EnemyCombat>();
        EnemyController controller =
            instance.GetComponent<EnemyController>();

        if (health != null)
            health.SetData(data);

        if (combat != null)
            combat.SetData(data);

        if (controller != null)
            controller.SetData(data);
    }
}
