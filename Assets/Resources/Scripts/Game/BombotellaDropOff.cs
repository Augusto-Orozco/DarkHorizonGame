using UnityEngine;


[RequireComponent(typeof(InteractionPrompt))]
public sealed class BombotellaDropOff : MonoBehaviour, IInteractable
{
    [SerializeField] private KeyItemData bombotellaData;

    private InteractionPrompt prompt;

    public Vector3 Position => transform.position;

   
    public bool CanInteract =>
        isActiveAndEnabled &&
        TraitorGameManager.Instance != null &&
        !TraitorGameManager.Instance.RoomUnlocked;

    private void Awake()
    {
        prompt = GetComponent<InteractionPrompt>();

        Collider zone = GetComponent<Collider>();
        if (zone == null || !zone.isTrigger)
            Debug.LogWarning("BombotellaDropOff necesita un Collider con Is Trigger.", this);

        if (bombotellaData == null)
            Debug.LogWarning("BombotellaDropOff sin KeyItemData de bombotella.", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInteractor interactor = other.GetComponentInParent<PlayerInteractor>();
        if (interactor != null)
            interactor.Register(this);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInteractor interactor = other.GetComponentInParent<PlayerInteractor>();
        if (interactor != null)
            interactor.Unregister(this);
    }

    public void SetFocused(PlayerInteractor interactor, bool focused)
    {
        if (focused)
            ShowPromptFor(interactor);
        else
            prompt.Hide();
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!CanInteract || !CarriesBombotella(interactor.Inventory))
            return;

        
        if (!interactor.Inventory.TryUseItem(out _))
            return;

        TraitorGameManager.Instance.RegisterBombotella();

        if (CanInteract)
            ShowPromptFor(interactor);
        else
            prompt.Hide();              
    }

    private void ShowPromptFor(PlayerInteractor interactor)
    {
        TraitorGameManager manager = TraitorGameManager.Instance;
        if (manager == null) return;

        string count = $"({manager.CollectedBombotellas}/{manager.RequiredBombotellas})";
        prompt.Show(CarriesBombotella(interactor.Inventory)
            ? $"[E] Entregar bombotella {count}"
            : $"Necesitas bombotellas {count}");
    }

    private bool CarriesBombotella(PlayerInventory inventory)
    {
        return bombotellaData != null && inventory.Item == bombotellaData;
    }
}