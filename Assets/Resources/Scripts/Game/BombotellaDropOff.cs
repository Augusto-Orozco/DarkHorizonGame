using Photon.Pun;
using UnityEngine;
using UnityEngine.Localization;

[RequireComponent(typeof(InteractionPrompt))]
public sealed class BombotellaDropOff : MonoBehaviour, IInteractable
{
    private const string TextTable = "Textos";

    [SerializeField] private KeyItemData bombotellaData;

    // Textos de la tabla "Textos"; {0} es el conteo "(x/y)". Necesitan la casilla "Smart".
    [SerializeField] private LocalizedString deliverText = new LocalizedString(TextTable, "G091");
    [SerializeField] private LocalizedString needText = new LocalizedString(TextTable, "G092");

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
            ? deliverText.GetLocalizedString(count)
            : needText.GetLocalizedString(count));
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