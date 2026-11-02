using UnityEngine;

public sealed class TraitorRoomGate : MonoBehaviour
{
    [SerializeField] private Collider gateCollider;
    [SerializeField] private InteractionPrompt prompt;

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

        if (gateCollider != null)
            gateCollider.enabled = !open;

        if (open && prompt != null)
            prompt.Hide();
    }

    public void RefreshRequirement()
    {
        if (prompt == null || TraitorGameManager.Instance == null)
            return;

        TraitorGameManager manager = TraitorGameManager.Instance;
        if (!manager.RoomUnlocked)
            prompt.Show(
                $"Necesitas {manager.RequiredBombotellas} bombotellas " +
                $"({manager.CollectedBombotellas}/{manager.RequiredBombotellas})");
        else
            prompt.Hide();
    }
}
