using UnityEngine;

public class KharvekRage : MonoBehaviour
{
    [Header("Furia")]
    [Tooltip("Velocidad cuando está furioso (1.5 = 50% más rápido)")]
    [SerializeField] private float rageSpeedMultiplier = 1.5f;
    [Tooltip("Intervalo de daño cuando está furioso (0.5 = pega el doble de seguido)")]
    [SerializeField] private float rageDamageIntervalMultiplier = 0.5f;
    [Tooltip("Segundos que sigue furioso después de que dejan de iluminarlo")]
    [SerializeField] private float calmDownDelay = 3f;
    [SerializeField] private bool debugLog;

    public bool IsEnraged { get; private set; }

    private LightExposure lightExposure;
    private ProceduralKharvekMovement movement;
    private KharvekContactDamage contactDamage;
    private float calmDownTime = float.PositiveInfinity;

    private void Awake()
    {
        lightExposure = GetComponent<LightExposure>();
        if (lightExposure == null)
            lightExposure = gameObject.AddComponent<LightExposure>();


        movement = GetComponentInParent<ProceduralKharvekMovement>();
        if (movement == null)
            movement = GetComponentInChildren<ProceduralKharvekMovement>();

        contactDamage = GetComponentInParent<KharvekContactDamage>();
        if (contactDamage == null)
            contactDamage = GetComponentInChildren<KharvekContactDamage>();

        if (movement == null || contactDamage == null)
            Debug.LogWarning($"[{name}] KharvekRage no encontró ProceduralKharvekMovement o KharvekContactDamage.", this);
    }

    private void OnEnable()
    {
        lightExposure.OnLightLevelChanged += HandleLightLevelChanged;
    }

    private void OnDisable()
    {
        if (lightExposure != null)
            lightExposure.OnLightLevelChanged -= HandleLightLevelChanged;
    }

    private void HandleLightLevelChanged(int level)
    {
        if (level > 0)
        {

            calmDownTime = float.PositiveInfinity;
            SetEnraged(true);
        }
        else
        {

            calmDownTime = Time.time + calmDownDelay;
        }
    }

    private void Update()
    {
        if (IsEnraged && Time.time >= calmDownTime)
            SetEnraged(false);
    }

    private void SetEnraged(bool enraged)
    {
        if (IsEnraged == enraged)
            return;

        IsEnraged = enraged;

        if (movement != null)
            movement.SpeedMultiplier = enraged ? rageSpeedMultiplier : 1f;

        if (contactDamage != null)
            contactDamage.IntervalMultiplier = enraged ? rageDamageIntervalMultiplier : 1f;

        if (debugLog)
            Debug.Log($"[{name}] {(enraged ? "¡FURIOSO!" : "Se calmó")}", this);
    }
}