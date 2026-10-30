using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(InteractionPrompt))]
public class GroundWeapon : Pickup, IInteractable
{
    [SerializeField] private WeaponData weaponData;
    private WeaponState state;
    private InteractionPrompt prompt;
    public Vector3 Position => transform.position;
    public bool CanInteract => !IsCollected && state != null && isActiveAndEnabled;

    protected override void Awake()
    {
        base.Awake();
        prompt = GetComponent<InteractionPrompt>();

        if (weaponData == null)
            Debug.LogWarning("GrpundWeapon sin WeaponData asignado");
        else if (state == null)
            state = new WeaponState(weaponData);
        
    }
    public void Initialize(WeaponState existingState){
        state = existingState;
        weaponData = existingState.Data;
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
            prompt.Show(BuildPromptText(interactor.Inventory));
        else
            prompt.Hide();
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!CanInteract)
            return;
        WeaponState previous = interactor.Inventory.EquipWeapon(state);
        if (previous != null)
            Spawn(previous, transform.position, transform.rotation);
        Collect(interactor.Inventory);
    }

    protected override void Despawn()
    {
        Destroy(gameObject);
    }

    private string BuildPromptText(PlayerInventory inventory)
    {
        string newName = state.Data.ItemName;
        WeaponState current = inventory.GetWeapon(state.Data.Slot);

        return current == null
            ? $"[E] Recoger {newName}"
            : $"[E] Cambiar {current.Data.ItemName} por {newName}";
    }

    public static GroundWeapon Spawn(WeaponState weapon, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = weapon.Data.Prefab;
        if(prefab == null)
        {
            Debug.LogWarning($"{weapon.Data.ItemName} no tiene prefab asignado");
            return null;
        }
        GameObject spawned = Instantiate(prefab, position, rotation);
        GroundWeapon groundWeapon = spawned.GetComponent<GroundWeapon>();
        if(groundWeapon == null)
        {
            Debug.LogWarning($"El prefab de {weapon.Data.ItemName} no tiene GroundWeapon", spawned);
            Destroy(spawned);
            return null;
        }

        groundWeapon.Initialize(weapon);
        return groundWeapon;

    }
}
