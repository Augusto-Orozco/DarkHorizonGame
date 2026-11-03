using System;
using UnityEngine;


public class LightExposure : MonoBehaviour
{
    [Tooltip("Punto del enemigo que se revisa. Si está vacío, usa este objeto + Check Height")]
    [SerializeField] private Transform checkPoint;
    [SerializeField] private float checkHeight = 1f;
    [SerializeField] private float checkInterval = 0.2f;
    [Tooltip("Capas que bloquean la luz (paredes, piso...)")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private bool debugLog;

    public int LightLevel { get; private set; }
    public bool IsLit => LightLevel > 0;
    public Vector3 LightSourcePosition { get; private set; }

    public event Action<int> OnLightLevelChanged;

    private float nextCheckTime;

    private Collider bodyCollider;

    private void Awake()
    {

        foreach (Collider candidate in GetComponentsInChildren<Collider>())
        {
            if (!candidate.isTrigger)
            {
                bodyCollider = candidate;
                break;
            }
        }
    }

   
    private Vector3 CheckPosition
    {
        get
        {
            if (checkPoint != null) return checkPoint.position;
            if (bodyCollider != null) return bodyCollider.bounds.center;
            return transform.position + Vector3.up * checkHeight;
        }
    }

    private void Update()
    {
        if (Time.time < nextCheckTime)
            return;
        nextCheckTime = Time.time + checkInterval;

        if (debugLog)
            Debug.Log($"[LUZ] {name}: linternas registradas = {LightsRange.Active.Count}", this);

        int bestLevel = 0;
        Vector3 bestSource = LightSourcePosition;

        foreach (LightsRange lantern in LightsRange.Active)
        {
            if (lantern == null || !IsLitBy(lantern.myLight))
                continue;

            int level = Mathf.Max(1, lantern.JugadoresCercanosActual);   
            if (level > bestLevel)
            {
                bestLevel = level;
                bestSource = lantern.myLight.transform.position;
            }
        }

        LightSourcePosition = bestSource;

        if (bestLevel != LightLevel)
        {
            LightLevel = bestLevel;
            OnLightLevelChanged?.Invoke(LightLevel);
        }
    }

    private bool IsLitBy(Light lamp)
    {
        if (lamp == null || !lamp.enabled || !lamp.gameObject.activeInHierarchy)
        {
            Log("la linterna no tiene Light asignada o está apagada");
            return false;
        }

        Vector3 origin = lamp.transform.position;
        Vector3 toTarget = CheckPosition - origin;
        float distance = toTarget.magnitude;

        if (distance > lamp.range || distance < 0.01f)
        {
            Log($"fuera de alcance (distancia {distance:F1} > range {lamp.range:F1})");
            return false;
        }

        float angle = Vector3.Angle(lamp.transform.forward, toTarget);
        if (lamp.type == LightType.Spot && angle > lamp.spotAngle * 0.5f)
        {
            Log($"fuera del cono (ángulo {angle:F0}° > {lamp.spotAngle * 0.5f:F0}°)");
            return false;
        }

        RaycastHit[] hits = Physics.RaycastAll(origin, toTarget / distance, distance,
                                               obstacleMask, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            Transform hitRoot = hit.collider.transform.root;
            if (hitRoot != transform.root && hitRoot != lamp.transform.root)
            {
                Log($"tapado por '{hit.collider.name}'");
                return false;
            }
        }

        Log("¡ILUMINADO!");
        return true;
    }

    private void Log(string message)
    {
        if (debugLog)
            Debug.Log($"[LUZ] {name}: {message}", this);
    }
}