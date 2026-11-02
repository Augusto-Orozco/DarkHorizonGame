using UnityEngine;

[RequireComponent(typeof(InteractionPrompt))]
public sealed class BombotellaPickup : Pickup, IInteractable
{
    private InteractionPrompt prompt;

    public Vector3 Position => transform.position;
    public bool CanInteract => !IsCollected && isActiveAndEnabled;

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
            prompt.Show("[E] Recoger bombotella");
        else
            prompt.Hide();
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!CanInteract)
            return;

        if (TraitorGameManager.Instance == null)
        {
            Debug.LogError("BombotellaPickup necesita un TraitorGameManager en la escena.", this);
            return;
        }

        TraitorGameManager.Instance.RegisterBombotella();
        Collect(interactor.Inventory);
    }

    protected override void Despawn()
    {
        Destroy(gameObject);
    }
}
