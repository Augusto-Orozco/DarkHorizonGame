using UnityEngine;
using UnityEngine.Localization;


[RequireComponent(typeof(InteractionPrompt))]
public sealed class BombotellaPickup : Pickup, IInteractable
{
    private const string TextTable = "Textos";

    [SerializeField] private KeyItemData bombotellaData;

    // Textos de la tabla "Textos".
    [SerializeField] private LocalizedString pickupText = new LocalizedString(TextTable, "G094");
    [SerializeField] private LocalizedString carryingText = new LocalizedString(TextTable, "G095");

    private InteractionPrompt prompt;

    public Vector3 Position => transform.position;
    public bool CanInteract => !IsCollected && isActiveAndEnabled;

    protected override void Awake()
    {
        base.Awake();
        prompt = GetComponent<InteractionPrompt>();

        if (bombotellaData == null)
            Debug.LogWarning("BombotellaPickup sin KeyItemData asignado.", this);
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
        if (!focused)
        {
            prompt.Hide();
            return;
        }

        bool hasFreeSlot = interactor.Inventory.Item == null;
        prompt.Show((hasFreeSlot ? pickupText : carryingText).GetLocalizedString());
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!CanInteract || bombotellaData == null)
            return;


        if (interactor.Inventory.TryCollect(bombotellaData))
            Collect(interactor.Inventory);
    }

    protected override void Despawn()
    {
        RemoveForEveryone();
    }
}