using System.Collections.Generic;
using UnityEngine;


public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemyEntry
    {
        public GameObject prefab;
        [Tooltip("Espacio entre los pies y el piso (Shurkur vuela). En Kharvek: altura de su raíz.")]
        public float heightOffset = 0f;
        [Tooltip("Bajar/subir al enemigo hasta que la parte más baja de su collider toque el piso (+ Height Offset). Apágalo en Kharvek: él se acomoda solo.")]
        public bool alignToGround = true;
        [Tooltip("Probabilidad relativa: 2 = sale el doble que uno con 1. 0 = nunca sale")]
        public float weight = 1f;
    }

    [Header("Enemigos")]
    [SerializeField] private EnemyEntry[] enemies;
    [SerializeField] private int enemyCount = 12;
    [Tooltip("Cuántos de los enemigos serán jefes (los que sueltan bombotella)")]
    [SerializeField] private int bossCount = 4;

    [Header("Puntos de spawn")]
    [Tooltip("Vacío = usa los hijos de este objeto")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private LayerMask groundMask = ~0;
    [Tooltip("Desde qué altura sobre el punto se busca el piso hacia abajo")]
    [SerializeField] private float groundProbeHeight = 1f;
    [SerializeField] private float groundProbeDistance = 50f;

    [SerializeField] private bool spawnOnStart = true;

    private readonly List<GameObject> spawned = new List<GameObject>();
    public IReadOnlyList<GameObject> Spawned => spawned;

    private void Start()
    {
        if (spawnOnStart)
            SpawnAll();
    }

    public void SpawnAll()
    {
        List<Transform> points = GetSpawnPoints();
        if (points.Count == 0 || enemies == null || enemies.Length == 0)
        {
            Debug.LogWarning("[SPAWNER] Faltan puntos de spawn o enemigos en la lista.", this);
            return;
        }

        Shuffle(points);
        int count = Mathf.Min(enemyCount, points.Count);
        if (count < enemyCount)
            Debug.LogWarning($"[SPAWNER] Pediste {enemyCount} enemigos pero solo hay {points.Count} puntos.", this);

        for (int i = 0; i < count; i++)
        {
            EnemyEntry entry = PickRandomEntry();
            if (entry == null || entry.prefab == null)
                continue;

            Vector3 ground = FindGround(points[i].position);
            Vector3 position = ground + Vector3.up * entry.heightOffset;
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject enemy = SpawnEnemy(entry.prefab, position, rotation);
            if (entry.alignToGround)
                AlignToGround(enemy, ground.y + entry.heightOffset);

            spawned.Add(enemy);

            Debug.Log($"[SPAWNER] {entry.prefab.name} en {points[i].name}: " +
                      $"piso y={ground.y:0.0}, quedó en y={enemy.transform.position.y:0.0}", enemy);
        }

        ChooseBosses();
        Debug.Log($"[SPAWNER] {spawned.Count} enemigos, {Mathf.Min(bossCount, spawned.Count)} jefes.", this);
    }


    private GameObject SpawnEnemy(GameObject prefab, Vector3 position, Quaternion rotation)
    {

        Vector3 offset = GetBodyOffset(prefab);
        Vector3 rotatedOffset = rotation * Quaternion.Inverse(prefab.transform.rotation) * offset;

        return Instantiate(prefab, position - rotatedOffset, rotation);
    }

  
    private static Vector3 GetBodyOffset(GameObject prefab)
    {
        EnemyHealth body = prefab.GetComponentInChildren<EnemyHealth>(true);
        if (body == null)
            return Vector3.zero;

        Vector3 offset = body.transform.position - prefab.transform.position;
        offset.y = 0f;  
        return offset;
    }

  
    private static void AlignToGround(GameObject enemy, float targetY)
    {
        
        Physics.SyncTransforms();

        float lowest = float.PositiveInfinity;
        foreach (Collider col in enemy.GetComponentsInChildren<Collider>())
        {
            if (col.isTrigger)
                continue;

            lowest = Mathf.Min(lowest, col.bounds.min.y);
        }

        if (float.IsInfinity(lowest))
            return;  

        enemy.transform.position += Vector3.up * (targetY - lowest);
    }

    private void ChooseBosses()
    {
        List<GameObject> candidates = new List<GameObject>(spawned);
        Shuffle(candidates);

        int bosses = Mathf.Min(bossCount, candidates.Count);
        if (bosses < bossCount)
            Debug.LogWarning($"[SPAWNER] Solo hay {candidates.Count} enemigos: no alcanzan para {bossCount} jefes.", this);

        for (int i = 0; i < bosses; i++)
        {

            EnemyHealth health = candidates[i].GetComponentInChildren<EnemyHealth>();
            if (health != null)
                health.gameObject.AddComponent<EnemyBoss>();
        }
    }


    private EnemyEntry PickRandomEntry()
    {
        float total = 0f;
        foreach (EnemyEntry entry in enemies)
            total += Mathf.Max(0f, entry.weight);

        if (total <= 0f)
            return null;

        float roll = Random.Range(0f, total);
        foreach (EnemyEntry entry in enemies)
        {
            float weight = Mathf.Max(0f, entry.weight);
            if (weight <= 0f)
                continue;  

            roll -= weight;
            if (roll <= 0f)
                return entry;
        }


        for (int i = enemies.Length - 1; i >= 0; i--)
            if (enemies[i].weight > 0f)
                return enemies[i];

        return null;
    }

    private Vector3 FindGround(Vector3 point)
    {
        Vector3 origin = point + Vector3.up * groundProbeHeight;
        float remaining = groundProbeDistance + groundProbeHeight;


        for (int attempt = 0; attempt < 10 && remaining > 0f; attempt++)
        {
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, remaining,
                                 groundMask, QueryTriggerInteraction.Ignore))
                break;

            if (hit.point.y <= point.y)
                return hit.point;  

            float step = hit.distance + 0.05f;
            origin += Vector3.down * step;
            remaining -= step;
        }

        Debug.LogWarning($"[SPAWNER] No encontré piso debajo de {point}. ¿El punto está debajo del mapa?", this);
        return point;
    }

    private List<Transform> GetSpawnPoints()
    {
        List<Transform> points = new List<Transform>();

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            foreach (Transform point in spawnPoints)
                if (point != null)
                    points.Add(point);
        }
        else
        {
            foreach (Transform child in transform)
                points.Add(child);
        }

        return points;
    }

   
    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        foreach (Transform point in GetSpawnPoints())
        {
            Gizmos.DrawWireSphere(point.position, 3f);
            Gizmos.DrawLine(point.position, point.position + Vector3.up * 10f);
        }
    }
}