using UnityEngine;

public static class GroundPlacement
{
    private const float RayStartHeight = 50f;
    private const float RayDistance = 100f;
    private const float MaxSurfaceHeightAboveOrigin = 0.25f;

    public static void PlaceOnGround(GameObject spawnedObject, Vector3 origin, Transform ignoredRoot = null)
    {
        if (spawnedObject == null)
            return;

        Collider[] colliders = spawnedObject.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
        {
            Debug.LogWarning("No se pudo colocar el objeto en el suelo porque no tiene Collider.", spawnedObject);
            return;
        }

        RaycastHit[] hits = Physics.RaycastAll(
            origin + Vector3.up * RayStartHeight,
            Vector3.down,
            RayDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        RaycastHit? closestHit = null;

        foreach (RaycastHit hit in hits)
        {
            if (IsCharacter(hit.collider))
                continue;

            if (hit.point.y > origin.y + MaxSurfaceHeightAboveOrigin)
                continue;

            if (IsPartOf(hit.collider.transform, spawnedObject.transform) ||
                ignoredRoot != null && IsPartOf(hit.collider.transform, ignoredRoot))
            {
                continue;
            }

            if (!closestHit.HasValue || hit.distance < closestHit.Value.distance)
                closestHit = hit;
        }

        if (!closestHit.HasValue)
            return;

        float lowestPoint = GetLowestPoint(spawnedObject, colliders); 
        spawnedObject.transform.position +=
            Vector3.up * (closestHit.Value.point.y - lowestPoint);
    }

 
    private static float GetLowestPoint(GameObject spawnedObject, Collider[] colliders)
    {
        float lowestPoint = float.PositiveInfinity;

        foreach (Collider collider in colliders)
            lowestPoint = Mathf.Min(lowestPoint, collider.bounds.min.y);

        foreach (Renderer renderer in spawnedObject.GetComponentsInChildren<Renderer>())
            lowestPoint = Mathf.Min(lowestPoint, renderer.bounds.min.y);

        return lowestPoint;
    }

    private static bool IsPartOf(Transform child, Transform root)
    {
        return child == root || child.IsChildOf(root);
    }

    private static bool IsCharacter(Collider collider)
    {
        return collider.GetComponentInParent<PlayerHealth>() != null ||
               collider.GetComponentInParent<EnemyHealth>() != null ||
               collider.GetComponentInParent<NetworkEnemy>() != null;
    }
}
