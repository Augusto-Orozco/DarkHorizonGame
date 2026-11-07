using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(InteractionPrompt))]
public sealed class BombotellaDropOff : MonoBehaviour, IInteractable
{
    [SerializeField] private KeyItemData bombotellaData;

    private InteractionPrompt prompt;
    private PlayerInteractor focusedInteractor;

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

    private void Start()
    {
        if (TraitorGameManager.Instance != null)
            TraitorGameManager.Instance.OnBombotellasChanged += HandleBombotellasChanged;
    }

    private void OnDestroy()
    {
        if (TraitorGameManager.Instance != null)
            TraitorGameManager.Instance.OnBombotellasChanged -= HandleBombotellasChanged;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInteractor interactor = other.GetComponentInParent<PlayerInteractor>();
        if (interactor != null && IsLocal(interactor))
            interactor.Register(this);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInteractor interactor = other.GetComponentInParent<PlayerInteractor>();
        if (interactor != null && IsLocal(interactor))
            interactor.Unregister(this);
    }

    public void SetFocused(PlayerInteractor interactor, bool focused)
    {
        focusedInteractor = focused ? interactor : null;

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
        ShowPromptFor(interactor);
    }

    private void HandleBombotellasChanged(int collected, int required)
    {
        if (focusedInteractor == null)
            return;

        if (CanInteract)
            ShowPromptFor(focusedInteractor);
        else
            prompt.Hide();
    }

    private void ShowPromptFor(PlayerInteractor interactor)
    {
        TraitorGameManager manager = TraitorGameManager.Instance;
        if (manager == null)
            return;

        string count = $"({manager.CollectedBombotellas}/{manager.RequiredBombotellas})";
        prompt.Show(CarriesBombotella(interactor.Inventory)
            ? $"[E] Entregar bombotella {count}"
            : $"Necesitas bombotellas {count}");
    }

    private bool CarriesBombotella(PlayerInventory inventory)
    {
        return bombotellaData != null && inventory.Item == bombotellaData;
    }

    private static bool IsLocal(PlayerInteractor interactor)
    {
        if (!PhotonNetwork.InRoom)
            return true;

        PhotonView view = interactor.GetComponentInParent<PhotonView>();
        return view == null || view.IsMine;
    }
}