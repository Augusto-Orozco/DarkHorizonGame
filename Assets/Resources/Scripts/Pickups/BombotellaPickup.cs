using UnityEngine;


[RequireComponent(typeof(InteractionPrompt))]
public sealed class BombotellaPickup : Pickup, IInteractable
{
    [SerializeField] private KeyItemData bombotellaData;

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
        prompt.Show(hasFreeSlot ? "[E] Recoger bombotella" : "Ya llevas un item");
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
        Destroy(gameObject);
    }
}