using UnityEngine;


public static class EnemyMover
{
    private const float Skin = 0.05f;


    private static readonly Collider[] OverlapBuffer = new Collider[16];
    private static readonly RaycastHit[] CastBuffer = new RaycastHit[16];

    public static void Move(Transform body, Collider bodyCollider, Vector3 delta, LayerMask obstacles)
    {
        delta.y = 0f;
        if (delta.sqrMagnitude < 0.000001f)
            return;

        if (bodyCollider == null)
        {
            body.position += delta;
            return;
        }

        Bounds bounds = bodyCollider.bounds;
        float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.6f;
        radius = Mathf.Min(radius, bounds.extents.y * 0.8f);
        Vector3 origin = bounds.center;

        Vector3 allowed = CastMove(body, origin, radius, delta, obstacles, out Vector3 wallNormal);


        Vector3 remaining = delta - allowed;
        if (wallNormal != Vector3.zero && remaining.sqrMagnitude > 0.000001f)
        {
            Vector3 slide = Vector3.ProjectOnPlane(remaining, wallNormal);
            slide.y = 0f;
            allowed += CastMove(body, origin + allowed, radius, slide, obstacles, out _);
        }

        body.position += allowed;

        Depenetrate(body, bodyCollider, obstacles);
    }

    private static Vector3 CastMove(Transform body, Vector3 origin, float radius, Vector3 delta,
                                    LayerMask obstacles, out Vector3 wallNormal)
    {
        wallNormal = Vector3.zero;
        float distance = delta.magnitude;
        if (distance < 0.000001f)
            return Vector3.zero;

        Vector3 direction = delta / distance;
        int count = Physics.SphereCastNonAlloc(origin, radius, direction, CastBuffer, distance + Skin,
                                               obstacles, QueryTriggerInteraction.Ignore);


        float closest = float.PositiveInfinity;
        Vector3 closestNormal = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = CastBuffer[i];


            if (hit.distance <= 0f)
                continue;

            if (IsIgnored(body, hit.collider))
                continue;

            if (hit.distance < closest)
            {
                closest = hit.distance;
                closestNormal = hit.normal;
            }
        }

        if (float.IsInfinity(closest))
            return delta; 

        wallNormal = closestNormal;
        wallNormal.y = 0f;
        wallNormal.Normalize();
        return direction * Mathf.Max(0f, closest - Skin);
    }

    private static void Depenetrate(Transform body, Collider bodyCollider, LayerMask obstacles)
    {

        Physics.SyncTransforms();

        Bounds bounds = bodyCollider.bounds;
        int count = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents, OverlapBuffer,
                                               Quaternion.identity, obstacles, QueryTriggerInteraction.Ignore);

        Transform colliderTransform = bodyCollider.transform;

        for (int i = 0; i < count; i++)
        {
            Collider other = OverlapBuffer[i];
            if (other == null || other == bodyCollider || IsIgnored(body, other))
                continue;

            if (!Physics.ComputePenetration(
                    bodyCollider, colliderTransform.position, colliderTransform.rotation,
                    other, other.transform.position, other.transform.rotation,
                    out Vector3 direction, out float distance))
                continue;


            if (Mathf.Abs(direction.y) > 0.7f)
                continue;

            body.position += new Vector3(direction.x, 0f, direction.z) * distance;
        }
    }


    private static bool IsIgnored(Transform body, Collider other)
    {
        if (other.transform.IsChildOf(body))
            return true;

        return other.GetComponentInParent<PlayerHealth>() != null;
    }
}