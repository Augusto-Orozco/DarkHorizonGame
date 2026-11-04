using UnityEngine;


public static class EnemyMover
{
    private const float Skin = 0.05f;

    private static readonly Collider[] OverlapBuffer = new Collider[16];

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

        Vector3 allowed = CastMove(origin, radius, delta, obstacles, out Vector3 wallNormal);


        Vector3 remaining = delta - allowed;
        if (wallNormal != Vector3.zero && remaining.sqrMagnitude > 0.000001f)
        {
            Vector3 slide = Vector3.ProjectOnPlane(remaining, wallNormal);
            slide.y = 0f;
            allowed += CastMove(origin + allowed, radius, slide, obstacles, out _);
        }

        body.position += allowed;

        Depenetrate(body, bodyCollider, obstacles);
    }

    private static Vector3 CastMove(Vector3 origin, float radius, Vector3 delta,
                                    LayerMask obstacles, out Vector3 wallNormal)
    {
        wallNormal = Vector3.zero;
        float distance = delta.magnitude;
        if (distance < 0.000001f)
            return Vector3.zero;

        Vector3 direction = delta / distance;
        if (Physics.SphereCast(origin, radius, direction, out RaycastHit hit, distance + Skin,
                               obstacles, QueryTriggerInteraction.Ignore))
        {
            wallNormal = hit.normal;
            wallNormal.y = 0f;
            wallNormal.Normalize();
            return direction * Mathf.Max(0f, hit.distance - Skin);
        }

        return delta;
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
            if (other == null || other == bodyCollider)
                continue;

            if (other.transform.IsChildOf(body))
                continue;

            if (!Physics.ComputePenetration(
                    bodyCollider, colliderTransform.position, colliderTransform.rotation,
                    other, other.transform.position, other.transform.rotation,
                    out Vector3 direction, out float distance))
                continue;

            if (Mathf.Abs(direction.y) > 0.7f)
                continue;

            Vector3 push = new Vector3(direction.x, 0f, direction.z) * distance;
            body.position += push;
        }
    }
}