using UnityEngine;
using UnityEngine.Localization;

[RequireComponent(typeof(InteractionPrompt))]
public sealed class TraitorArtifactPickup : Pickup, IInteractable
{
    private const string TextTable = "Textos";

    // Texto de la tabla "Textos".
    [SerializeField] private LocalizedString pickupText = new LocalizedString(TextTable, "G098");

    private InteractionPrompt prompt;

    public Vector3 Position => transform.position;
    public bool CanInteract =>
        !IsCollected &&
        isActiveAndEnabled &&
        TraitorGameManager.Instance != null &&
        TraitorGameManager.Instance.CanLocalTakeArtifact;

    protected override void Awake()
    {
        base.Awake();
        prompt = GetComponent<InteractionPrompt>();
    }

    protected override void OnPlayerEnter(PlayerInventory inventory)
    {
        PlayerInteractor interactor = inventory.GetComponent<PlayerInteractor>();
        if (interactor != null)
            interactor.Register(this);
    }

    protected override void OnPlayerExit(PlayerInventory inventory)
    {
        PlayerInteractor interactor = inventory.GetComponent<PlayerInteractor>();
        if (interactor != null)
            interactor.Unregister(this);
    }

    public void SetFocused(PlayerInteractor interactor, bool focused)
    {
        if (focused)
            prompt.Show(pickupText.GetLocalizedString());
        else
            prompt.Hide();
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!CanInteract || !TraitorGameManager.Instance.RequestCollectArtifact())
            return;

        Collect(interactor.Inventory);
    }

    protected override void Despawn()
    {
        RemoveForEveryone();
    }
}
