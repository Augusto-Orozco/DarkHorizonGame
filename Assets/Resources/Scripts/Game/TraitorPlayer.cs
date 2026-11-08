using System.Collections;
using Photon.Pun;
using UnityEngine;

public sealed class TraitorPlayer : MonoBehaviour
{
    [Header("Balance")]
    [SerializeField, Min(1f)] private float baseHealth = 100f;
    [SerializeField, Min(0f)] private float healthPerCrewMember = 75f;
    [SerializeField, Range(0.1f, 2f)] private float movementSpeedMultiplier = 0.85f;
    [SerializeField, Min(1f)] private float damageMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float revealStunDuration = 3f;

    [Header("Visual")]
    [SerializeField, Min(1f)] private float sizeMultiplier = 1.2f;
    [SerializeField] private Color traitorEmissionColor = Color.red;
    [SerializeField, Min(0f)] private float traitorEmissionIntensity = 2f;

    private Vector3 originalScale;
    private Renderer[] renderers;
    private Material[][] originalMaterials;

    public bool IsTraitor { get; private set; }
    public bool HasUnlimitedAmmo => IsTraitor;
    public float MovementSpeedMultiplier => IsTraitor ? movementSpeedMultiplier : 1f;
    public float DamageMultiplier => IsTraitor ? damageMultiplier : 1f;

    public static bool IsTraitorObject(GameObject target)
    {
        if (target == null)
            return false;

        TraitorPlayer traitor = target.GetComponentInParent<TraitorPlayer>();
        return traitor != null && traitor.IsTraitor;
    }

    private void Awake()
    {
        originalScale = transform.localScale;
        renderers = GetComponentsInChildren<Renderer>(true);
        originalMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
            originalMaterials[i] = renderers[i].materials;
    }

    public void Activate(int crewCount)
    {
        if (IsTraitor)
            return;

        IsTraitor = true;
        transform.localScale = originalScale * sizeMultiplier;
        ApplyTraitorVisual();

        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
            health.SetMaxHealth(baseHealth + healthPerCrewMember * Mathf.Max(1, crewCount), true);

        PhotonView view = GetComponent<PhotonView>();
        bool isMine = !PhotonNetwork.InRoom || view == null || view.IsMine;
        if (isMine && revealStunDuration > 0f)
            StartCoroutine(RevealStun());
    }

    private IEnumerator RevealStun()
    {
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement == null)
            yield break;

        bool previousInput = movement.readInput;
        movement.readInput = false;
        yield return new WaitForSeconds(revealStunDuration);

        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health == null || !health.IsDead)
            movement.readInput = previousInput;
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