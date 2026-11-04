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

    [System.Serializable]
    public class BossSlot
    {
        [Tooltip("Dónde aparece este jefe (en una sala grande). NO lo pongas como hijo del spawner.")]
        public Transform point;
        public Color color = new Color(1f, 0.15f, 0.1f);
        [Tooltip("Nombre en la barra del HUD")]
        public string displayName = "Kharvek";
    }

    [Header("Enemigos normales (al azar)")]
    [SerializeField] private EnemyEntry[] enemies;
    [SerializeField] private int enemyCount = 12;

    [Header("Puntos normales")]
    [Tooltip("Vacío = usa los hijos de este objeto")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Jefes (fijos)")]
    [SerializeField] private EnemyEntry bossEnemy;
    [SerializeField] private BossSlot[] bossSlots;

    [Header("Escoltas (alrededor de cada jefe, salen de la lista de enemigos normales)")]
    [SerializeField] private int escortsPerBoss = 3;
    [Tooltip("Distancia del jefe a la que aparecen (en círculo)")]
    [SerializeField] private float escortRadius = 25f;
    [Tooltip("Tamaño de las escoltas (0.6 = 60% del normal)")]
    [SerializeField] private float escortScale = 0.6f;
    [SerializeField] private float escortHealthMultiplier = 0.5f;
    [Tooltip("Qué tanto se alejan del jefe al patrullar")]
    [SerializeField] private Vector2 escortPatrolHalfExtents = new Vector2(20f, 20f);
    [Tooltip("Segundos de alerta cuando alguien le pega al jefe")]
    [SerializeField] private float escortAlertDuration = 8f;
    [Tooltip("Rango de detección durante la alerta. Que sea MENOR que su Lose Player Range (120)")]
    [SerializeField] private float escortAlertRange = 110f;

    [Header("Piso")]
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
        int escorts;
        int bosses = SpawnBosses(out escorts);
        int normals = SpawnNormalEnemies();
        Debug.Log($"[SPAWNER] {normals} enemigos normales, {bosses} jefes, {escorts} escoltas.", this);
    }



    private int SpawnBosses(out int escortCount)
    {
        escortCount = 0;

        if (bossSlots == null || bossSlots.Length == 0)
            return 0;

        if (bossEnemy == null || bossEnemy.prefab == null)
        {
            Debug.LogWarning("[SPAWNER] Hay Boss Slots pero falta el prefab del jefe.", this);
            return 0;
        }

        int count = 0;
        foreach (BossSlot slot in bossSlots)
        {
            if (slot == null || slot.point == null)
                continue;

            GameObject enemy = SpawnAt(bossEnemy, slot.point.position, 1f);


            EnemyHealth health = enemy.GetComponentInChildren<EnemyHealth>();
            if (health == null)
            {
                Debug.LogWarning($"[SPAWNER] {enemy.name} no tiene EnemyHealth: no puede ser jefe.", enemy);
                continue;
            }

            EnemyBoss boss = health.gameObject.AddComponent<EnemyBoss>();
            boss.SetAppearance(slot.color, slot.displayName);
            count++;

            escortCount += SpawnEscorts(health, slot.point.position);

            Debug.Log($"[SPAWNER] Jefe '{slot.displayName}' en {slot.point.name}", enemy);
        }

        return count;
    }

    private int SpawnEscorts(EnemyHealth bossHealth, Vector3 center)
    {
        if (escortsPerBoss <= 0 || enemies == null || enemies.Length == 0)
            return 0;

        int count = 0;
        float startAngle = Random.Range(0f, 360f);

        for (int i = 0; i < escortsPerBoss; i++)
        {
            EnemyEntry entry = PickRandomEntry();
            if (entry == null || entry.prefab == null)
                continue;


            float angle = (startAngle + i * 360f / escortsPerBoss) * Mathf.Deg2Rad;
            Vector3 point = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * escortRadius;

            GameObject escort = SpawnAt(entry, point, escortScale);

            EnemyHealth health = escort.GetComponentInChildren<EnemyHealth>();
            if (health != null)
                health.ScaleMaxHealth(escortHealthMultiplier);

            EnemyEscort escortComponent = escort.AddComponent<EnemyEscort>();
            escortComponent.Init(bossHealth, escortPatrolHalfExtents, escortAlertDuration, escortAlertRange);

            count++;
        }

        return count;
    }



    private int SpawnNormalEnemies()
    {
        List<Transform> points = GetSpawnPoints();
        if (points.Count == 0 || enemies == null || enemies.Length == 0)
        {
            Debug.LogWarning("[SPAWNER] Faltan puntos de spawn o enemigos en la lista.", this);
            return 0;
        }

        Shuffle(points);
        int count = Mathf.Min(enemyCount, points.Count);
        if (count < enemyCount)
            Debug.LogWarning($"[SPAWNER] Pediste {enemyCount} enemigos pero solo hay {points.Count} puntos.", this);

        int spawnedCount = 0;
        for (int i = 0; i < count; i++)
        {
            EnemyEntry entry = PickRandomEntry();
            if (entry == null || entry.prefab == null)
                continue;

            GameObject enemy = SpawnAt(entry, points[i].position, 1f);
            spawnedCount++;

            Debug.Log($"[SPAWNER] {entry.prefab.name} en {points[i].name}: " +
                      $"quedó en y={enemy.transform.position.y:0.0}", enemy);
        }

        return spawnedCount;
    }




    private GameObject SpawnAt(EnemyEntry entry, Vector3 point, float scale)
    {
        Vector3 ground = FindGround(point);
        Vector3 position = ground + Vector3.up * entry.heightOffset;
        Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        GameObject enemy = SpawnEnemy(entry.prefab, position, rotation);


        if (!Mathf.Approximately(scale, 1f))
            enemy.transform.localScale *= scale;

        if (entry.alignToGround)
            AlignToGround(enemy, ground.y + entry.heightOffset * scale);

        spawned.Add(enemy);
        return enemy;
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

        if (bossSlots == null)
            return;

        foreach (BossSlot slot in bossSlots)
        {
            if (slot == null || slot.point == null)
                continue;


            Gizmos.color = slot.color;
            Gizmos.DrawWireSphere(slot.point.position, 10f);
            Gizmos.DrawLine(slot.point.position, slot.point.position + Vector3.up * 30f);


            Gizmos.color = new Color(slot.color.r, slot.color.g, slot.color.b, 0.4f);
            Gizmos.DrawWireSphere(slot.point.position, escortRadius);
        }
    }
}