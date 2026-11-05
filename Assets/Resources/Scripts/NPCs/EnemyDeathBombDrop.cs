using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public sealed class EnemyDeathBombDrop : MonoBehaviour
{
    private const string DefaultBombPrefabPath = "Prefabs/Ground_Granada";

    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private bool destroyEnemyOnDeath = true;
    [Tooltip("Si está activo, solo suelta la bombotella si este enemigo es jefe (tiene EnemyBoss)")]
    [SerializeField] private bool dropOnlyIfBoss = true;

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

        bool isBoss = TryGetComponent(out EnemyBoss _);
        bool shouldDrop = !dropOnlyIfBoss || isBoss;

        if (bombPrefab != null && shouldDrop)
        {
            Collider body = GetComponent<Collider>();
            Vector3 dropPoint = body != null ? body.bounds.center : transform.position;

            GameObject spawnedBomb = Instantiate(bombPrefab, dropPoint, transform.rotation);
            GroundPlacement.PlaceOnGround(spawnedBomb, dropPoint, transform);
        }

        if (destroyEnemyOnDeath)
            DestroyEnemy();
    }

    private void DestroyEnemy()
    {
        GameObject root = transform.root.gameObject;

        if (PhotonNetwork.InRoom)
        {
            PhotonView rootView = root.GetComponent<PhotonView>();
            if (rootView != null)
            {
                PhotonNetwork.Destroy(rootView.gameObject);
                return;
            }
        }

        Destroy(root);
    }
}