using UnityEngine;


public class EnemyTargeting : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Cada cuántos segundos se vuelve a buscar la lista de jugadores")]
    [SerializeField] private float refreshInterval = 0.5f;

    private Transform[] players = new Transform[0];
    private PlayerHealth[] healths = new PlayerHealth[0];
    private float nextRefreshTime;
    private bool tagErrorLogged;

    public Transform FindClosestPlayer(float range)
    {
        if (range <= 0f)
            return null;

        RefreshPlayersIfNeeded();

        Transform closest = null;
        float closestDistanceSqr = range * range;

        for (int i = 0; i < players.Length; i++)
        {
            Transform candidate = players[i];
            if (candidate == null || !candidate.gameObject.activeInHierarchy)
                continue;

            if (healths[i] != null && healths[i].IsDead)
                continue;

            Vector3 offset = candidate.position - transform.position;
            offset.y = 0f;
            float distanceSqr = offset.sqrMagnitude;

            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closest = candidate;
            }
        }

        return closest;
    }

    private void RefreshPlayersIfNeeded()
    {
        if (Time.time < nextRefreshTime)
            return;

        nextRefreshTime = Time.time + refreshInterval;

        GameObject[] found;
        try
        {
            found = GameObject.FindGameObjectsWithTag(playerTag);
        }
        catch (UnityException)
        {
            if (!tagErrorLogged)
            {
                Debug.LogError($"EnemyTargeting: el tag '{playerTag}' no existe.", this);
                tagErrorLogged = true;
            }
            found = new GameObject[0];
        }

      
        players = new Transform[found.Length];
        healths = new PlayerHealth[found.Length];
        for (int i = 0; i < found.Length; i++)
        {
            players[i] = found[i].transform;
            healths[i] = found[i].GetComponentInParent<PlayerHealth>();
        }
    }
}