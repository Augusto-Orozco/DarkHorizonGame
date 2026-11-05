using System.Collections.Generic;
using UnityEngine;

public class PickupGlow : MonoBehaviour
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] private Color color = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private float glowIntensity = 2f;
    [SerializeField] private float lightRange = 20f;
    [SerializeField] private float lightIntensity = 8f;
    [SerializeField] private float lightHeight = 1.5f;
    [SerializeField] private float pulseSpeed = 1.5f;
    [Range(0f, 1f)]
    [SerializeField] private float pulseAmount = 0.35f;

    private Light glowLight;
    private readonly List<Material> glowMaterials = new List<Material>();

    private void Awake()
    {
        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            if (rend is ParticleSystemRenderer)
                continue;

            foreach (Material mat in rend.materials)
            {
                if (!mat.HasProperty(EmissionColorId))
                    continue;

                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                glowMaterials.Add(mat);
            }
        }

        GameObject lightObject = new GameObject("PickupGlowLight");
        lightObject.transform.SetParent(transform, false);

        glowLight = lightObject.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = color;
        glowLight.range = lightRange;
        glowLight.shadows = LightShadows.None;
    }

    private void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) * pulseAmount;

        Color glow = color * (glowIntensity * pulse);
        foreach (Material mat in glowMaterials)
            mat.SetColor(EmissionColorId, glow);

        if (glowLight != null)
        {
            glowLight.intensity = lightIntensity * pulse;
            glowLight.transform.position = transform.position + Vector3.up * lightHeight;
        }
    }

    private void OnDestroy()
    {
        foreach (Material mat in glowMaterials)
        {
            if (mat != null)
                Destroy(mat);
        }
    }
}