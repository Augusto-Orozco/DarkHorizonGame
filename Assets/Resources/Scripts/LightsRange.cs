using UnityEngine;
using System.Collections.Generic;

public class LightsRange : MonoBehaviour
{
    
    public Light myLight;

    [Header("Grupo")]
    public float Radio = 7f; 
    public Vector3 UbicacionRango = Vector3.zero;
    public LayerMask playerLayer;      

    [Header("Jugadores")]
    public float[] NivelesIntenso = new float[] { 20f, 30f, 40f, 50f };
    public float[] rangeLevels = new float[] { 50f, 60f, 70f, 800f };
    
    
    public float[] AngulosSpot = new float[] { 70f, 90f, 110f, 130f };

    
    public float suavizado = 3f;

    private Vector3 CenterPosition => transform.TransformPoint(UbicacionRango);

    void Start()
    {
        if (myLight == null) 
            myLight = GetComponentInChildren<Light>();
    }

    void Update()
    {
        if (myLight == null) return;

        // Detectar colliders dentro del área
        Collider[] hitColliders = Physics.OverlapSphere(CenterPosition, Radio, playerLayer);
        
        // Filtrar jugadores únicos
        HashSet<Transform> uniquePlayers = new HashSet<Transform>();
        for (int i = 0; i < hitColliders.Length; i++)
        {
            uniquePlayers.Add(hitColliders[i].transform.root);
        }

        int JugadoresCercanos = uniquePlayers.Count;

        if (JugadoresCercanos > 0)
        {
            int index = Mathf.Clamp(JugadoresCercanos - 1, 0, NivelesIntenso.Length - 1);

            float targetIntensity = NivelesIntenso[index];
            float targetRange = rangeLevels[index];
            float targetOuterAngle = (index < AngulosSpot.Length) ? AngulosSpot[index] : myLight.spotAngle;

            // Interpolación suave
            myLight.intensity = Mathf.Lerp(myLight.intensity, targetIntensity, Time.deltaTime * suavizado);
            myLight.range = Mathf.Lerp(myLight.range, targetRange, Time.deltaTime * suavizado);

            if (myLight.type == LightType.Spot)
            {
                targetOuterAngle = Mathf.Min(targetOuterAngle, 179f);
                myLight.spotAngle = Mathf.Lerp(myLight.spotAngle, targetOuterAngle, Time.deltaTime * suavizado);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(CenterPosition, Radio);
    }
}