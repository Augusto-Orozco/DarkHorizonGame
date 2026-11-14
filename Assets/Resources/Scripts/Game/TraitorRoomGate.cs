using System.Collections;
using UnityEngine;
using UnityEngine.Localization;

public sealed class TraitorRoomGate : MonoBehaviour
{
    private const string TextTable = "Textos";

    [SerializeField] private Collider gateCollider;
    [SerializeField] private InteractionPrompt prompt;

    // Texto de la tabla "Textos": "Necesitas {0} bombotellas ({1}/{0})".
    [SerializeField] private LocalizedString requirementText = new LocalizedString(TextTable, "G093");

    [Header("Animación de apertura")]
    [SerializeField] private Transform doorVisual;
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 30f, 0f);
    [SerializeField] private float openDuration = 3f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool isOpen;

    private void Awake()
    {
        if (gateCollider == null)
            gateCollider = GetComponent<Collider>();
        if (prompt == null)
            prompt = GetComponent<InteractionPrompt>();

        RefreshRequirement();
    }

    public void SetOpen(bool open)
    {
        if (open && TraitorGameManager.Instance != null &&
            !TraitorGameManager.Instance.HasRequiredBombotellas)
        {
            RefreshRequirement();
            return;
        }

        if (!open || isOpen)
            return;

        isOpen = true;

        if (prompt != null)
            prompt.Hide();

        if (doorVisual != null)
            StartCoroutine(OpenRoutine());
        else if (gateCollider != null)
            gateCollider.enabled = false;
    }

    private IEnumerator OpenRoutine()
    {
        Vector3 closedPosition = doorVisual.position;
        Vector3 openPosition = closedPosition + openOffset;
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = openCurve.Evaluate(Mathf.Clamp01(elapsed / openDuration));
            doorVisual.position = Vector3.LerpUnclamped(closedPosition, openPosition, t);
            yield return null;
        }

        doorVisual.position = openPosition;

        if (gateCollider != null)
            gateCollider.enabled = false;
    }

    public void RefreshRequirement()
    {
        if (prompt == null || TraitorGameManager.Instance == null)
            return;

        TraitorGameManager manager = TraitorGameManager.Instance;
        if (!manager.RoomUnlocked)
            prompt.Show(requirementText.GetLocalizedString(manager.RequiredBombotellas, manager.CollectedBombotellas));
        else
            prompt.Hide();
    }
}