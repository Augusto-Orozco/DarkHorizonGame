using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerWeaponInput : MonoBehaviour
{
    [Header("teclas")]
    [SerializeField] private Key dropKey = Key.X;
    [SerializeField] private Key switchKey = Key.Q;
    [SerializeField] private Key primaryKey = Key.Digit1;
    [SerializeField] private Key secondaryKey = Key.Digit2;
    [SerializeField] private Key breadKey = Key.F;
    [SerializeField] private Key dropBreadKey = Key.Z;
    [SerializeField] private Key dropAmmoKey = Key.C;

    [Header("Soltar")]
    [Tooltip("Punto donde cae el arma. Colócalo enfrente del personaje.")]
    [SerializeField] private Transform dropPoint;

    [Tooltip("Segundos en que el objeto soltado no se puede recoger.")]
    [SerializeField] private float dropPickupDelay = 2f;

    [Tooltip("Caja que cae al soltar balas.")]
    [SerializeField] private AmmoBoxData ammoBoxToDrop;

    [Header("Input System")]
    [SerializeField] private PlayerInput playerInput;

    private InputAction dropWeaponAction;
    private InputAction switchWeaponAction;
    private InputAction primaryAction;
    private InputAction secondaryAction;
    private InputAction breadAction;
    private InputAction dropBreadAction;
    private InputAction dropAmmoAction;

    private PlayerInventory inventory;
    private PlayerHealth health;


    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        health = GetComponent<PlayerHealth>();

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            InputActionMap playerMap =
                playerInput.actions.FindActionMap("Player", true);

            dropWeaponAction = playerMap.FindAction("DropWeapon", true);
            switchWeaponAction = playerMap.FindAction("SwitchWeapon", true);
            primaryAction = playerMap.FindAction("SelectPrimary", true);
            secondaryAction = playerMap.FindAction("SelectSecondary", true);
            breadAction = playerMap.FindAction("ConsumeBread", true);
            dropBreadAction = playerMap.FindAction("DropBread", true);
            dropAmmoAction = playerMap.FindAction("DropAmmo", true);
        }
        else
        {
            Debug.LogError("PlayerWeaponInput necesita PlayerInput.", this);
        }
    }

   private void Update()
    {
        if (health != null && health.IsDead)
            return;

        if (PauseMenuController.IsPaused)
            return;

        if (dropWeaponAction == null)
            return;

        if (dropWeaponAction.WasPressedThisFrame())
            DropWeapon();

        if (switchWeaponAction.WasPressedThisFrame())
            inventory.SwitchWeapon();

        if (primaryAction.WasPressedThisFrame())
            inventory.SelectSlot(WeaponSlotType.Primary);

        if (secondaryAction.WasPressedThisFrame())
            inventory.SelectSlot(WeaponSlotType.Secondary);

        if (breadAction.WasPressedThisFrame() &&
            (health == null || health.CanHeal))
            inventory.TryConsumeBread();

        if (dropBreadAction.WasPressedThisFrame())
            DropBread();

        if (dropAmmoAction.WasPressedThisFrame())
            DropAmmo();
    }
    private void DropWeapon()
    {
        if (dropPoint == null)
        {
            Debug.LogWarning("PlayerWeaponInput no tiene Drop Point asignado.", this);
            return;
        }

        WeaponState dropped = inventory.DropActiveWeapon();
        if (dropped == null)
            return;

        Quaternion rotation = Quaternion.Euler(0f, dropPoint.eulerAngles.y, 0f);
        GroundWeapon.Spawn(dropped, dropPoint.position, rotation, transform);
    }

    private void DropBread()
    {
        if (dropPoint == null)
        {
            Debug.LogWarning("No tiene DropInput asignado", this);
            return;
        }

        if (!inventory.TryDropBread(out ConsumableData bread))
            return;

        if (bread.Prefab == null)
        {
            Debug.LogWarning("Bread no tiene prefab asignado", this);
            return;
        }

        Quaternion rotation = Quaternion.Euler(0f, dropPoint.eulerAngles.y, 0f);
        NetworkItemSpawner.Spawn(bread.Prefab, dropPoint.position, rotation, NetworkPickup.BuildData(dropPickupDelay));
    }

    private void DropAmmo()
    {
        if (dropPoint == null || ammoBoxToDrop == null || ammoBoxToDrop.Prefab == null)
        {
            Debug.LogWarning("PlayerWeaponInput: falta Drop Point, Ammo Box To Drop o su Prefab.", this);
            return;
        }

        if (!inventory.TryDropAmmo(ammoBoxToDrop.AmmoAmount))
            return;

        Quaternion rotation = Quaternion.Euler(0f, dropPoint.eulerAngles.y, 0f);
        NetworkItemSpawner.Spawn(ammoBoxToDrop.Prefab, dropPoint.position, rotation, NetworkPickup.BuildData(dropPickupDelay));
    }
}
