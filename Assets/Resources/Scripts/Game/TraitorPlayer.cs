using UnityEngine;

public sealed class TraitorPlayer : MonoBehaviour
{
    [Header("Bonificaciones")]
    [SerializeField, Min(1f)] private float movementSpeedMultiplier = 1.35f;
    [SerializeField, Min(1f)] private float sizeMultiplier = 1.2f;
    [SerializeField] private Color traitorEmissionColor = Color.red;
    [SerializeField, Min(0f)] private float traitorEmissionIntensity = 2f;

    private Vector3 originalScale;
    private Renderer[] renderers;
    private Material[][] originalMaterials;

    public bool IsTraitor { get; private set; }
    public bool HasUnlimitedAmmo => IsTraitor;
    public float MovementSpeedMultiplier => IsTraitor ? movementSpeedMultiplier : 1f;

    private void Awake()
    {
        originalScale = transform.localScale;
        renderers = GetComponentsInChildren<Renderer>(true);
        originalMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
            originalMaterials[i] = renderers[i].materials;
    }

    public void Activate()
    {
        if (IsTraitor)
            return;

        IsTraitor = true;
        transform.localScale = originalScale * sizeMultiplier;
        ApplyTraitorVisual();
    }

    private void ApplyTraitorVisual()
    {
        if (renderers == null)
            return;

        Color emission = traitorEmissionColor * traitorEmissionIntensity;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            foreach (Material material in renderer.materials)
            {
                if (!material.HasProperty("_EmissionColor"))
                    continue;

                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
        }
    }

    private void OnDisable()
    {
        if (originalScale != Vector3.zero)
            transform.localScale = originalScale;

        RestoreOriginalMaterials();
    }

    private void OnEnable()
    {
        if (IsTraitor)
            ApplyTraitorVisual();
    }

    private void RestoreOriginalMaterials()
    {
        if (renderers == null || originalMaterials == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && originalMaterials[i] != null)
                renderers[i].materials = originalMaterials[i];
        }
    }
}
