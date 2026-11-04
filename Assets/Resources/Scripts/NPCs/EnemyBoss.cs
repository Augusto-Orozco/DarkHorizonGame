using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(EnemyHealth))]
public class EnemyBoss : MonoBehaviour
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");


    public static readonly List<EnemyBoss> Active = new List<EnemyBoss>();

    [Header("Jefe")]
    [SerializeField] private float healthMultiplier = 2.5f;
    [Tooltip("Nombre en la barra del HUD. Vacío = usa el nombre del objeto")]
    [SerializeField] private string bossName = "";

    [Header("Aura")]
    [SerializeField] private Color auraColor = new Color(1f, 0.15f, 0.1f);
    [Tooltip("Qué tanto brilla el cuerpo (más de 1 = brillo fuerte)")]
    [SerializeField] private float glowIntensity = 1f;
    [Tooltip("0 = se calcula solo según el tamaño del enemigo")]
    [SerializeField] private float auraRange = 0f;
    [SerializeField] private float auraIntensity = 6f;
    [SerializeField] private float pulseSpeed = 2f;
    [Range(0f, 1f)]
    [SerializeField] private float pulseAmount = 0.4f;

    public EnemyHealth Health { get; private set; }

    public string BossName => string.IsNullOrEmpty(bossName)
        ? name.Replace("(Clone)", "").Trim()
        : bossName;

    private Light auraLight;
    private Collider bodyCollider;
    private readonly List<Material> glowMaterials = new List<Material>();

    private void Awake()
    {
        Health = GetComponent<EnemyHealth>();
        Health.ScaleMaxHealth(healthMultiplier);

        foreach (Collider candidate in GetComponentsInChildren<Collider>())
        {
            if (!candidate.isTrigger)
            {
                bodyCollider = candidate;
                break;
            }
        }

        SetupGlow();
        CreateAuraLight();
    }

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);


    private void SetupGlow()
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
    }

    private void CreateAuraLight()
    {
        GameObject auraObject = new GameObject("BossAura");
        auraObject.transform.SetParent(transform, false);

        auraLight = auraObject.AddComponent<Light>();
        auraLight.type = LightType.Point;
        auraLight.color = auraColor;
        auraLight.shadows = LightShadows.None; 

        if (auraRange <= 0f)
        {
 
            float size = bodyCollider != null ? bodyCollider.bounds.extents.magnitude : 5f;
            auraRange = size * 4f;
        }
        auraLight.range = auraRange;
    }

    private void Update()
    {

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) * pulseAmount;

        Color glow = auraColor * (glowIntensity * pulse);
        foreach (Material mat in glowMaterials)
            mat.SetColor(EmissionColorId, glow);

        if (auraLight != null)
        {
            auraLight.intensity = auraIntensity * pulse;

    
            if (bodyCollider != null)
                auraLight.transform.position = bodyCollider.bounds.center + Vector3.up * bodyCollider.bounds.extents.y;
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