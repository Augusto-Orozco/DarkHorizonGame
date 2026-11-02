using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public sealed class EnemyDeathBombDrop : MonoBehaviour
{
    private const string DefaultBombPrefabPath = "Prefabs/Ground_Granada";

    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private bool destroyEnemyOnDeath = true;

    private EnemyHealth enemyHealth;
    private bool deathHandled;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();

        if (bombPrefab == null)
            bombPrefab = Resources.Load<GameObject>(DefaultBombPrefabPath);

        if (bombPrefab == null)
            Debug.LogError(
                $"No se encontró el prefab de bomba en Resources/{DefaultBombPrefabPath}.",
                this
            );
    }

    private void OnEnable()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        enemyHealth.OnDied += HandleDeath;
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
            enemyHealth.OnDied -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (deathHandled)
            return;

        deathHandled = true;

        if (bombPrefab != null)
        {
            GameObject spawnedBomb = Instantiate(bombPrefab, transform.position, transform.rotation);
            GroundPlacement.PlaceOnGround(spawnedBomb, transform.position, transform);
        }

        if (destroyEnemyOnDeath)
            Destroy(gameObject);
    }
}
