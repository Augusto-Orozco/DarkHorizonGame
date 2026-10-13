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

    private PlayerInventory inventory;


    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (Keyboard.current == null)
            return;

        if (keyboard[dropKey].wasPressedThisFrame)
            DropWeapon();

        if (keyboard[switchKey].wasPressedThisFrame)
            inventory.SwitchWeapon();

        if (keyboard[primaryKey].wasPressedThisFrame)
            inventory.SelectSlot(WeaponSlotType.Primary);

        if (keyboard[secondaryKey].wasPressedThisFrame)
            inventory.SelectSlot(WeaponSlotType.Secondary);

        if (keyboard[breadKey].wasPressedThisFrame)
            inventory.TryConsumeBread();
        if (keyboard[dropBreadKey].wasPressedThisFrame)
            DropBread();

        if (keyboard[dropAmmoKey].wasPressedThisFrame)
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
        GroundWeapon.Spawn(dropped, dropPoint.position, rotation);
    }

    private void DropBread()
    {
        if(dropPoint == null)
        {
            Debug.LogWarning("No tiene DropInput asignado", this);
            return;
        }

        if (!inventory.TryDropBread(out ConsumableData bread))
            return;

        if(bread.Prefab == null)
        {
            Debug.LogWarning("Bread no tiene prefab asignado", this);
            return;
        }

        Quaternion rotation = Quaternion.Euler(0f, dropPoint.eulerAngles.y, 0f);
        GameObject spawned = Instantiate(bread.Prefab, dropPoint.position, rotation);
        if (spawned.TryGetComponent(out ItemPickup pickup))
            pickup.BlockPickup(dropPickupDelay);

    }

    private void DropAmmo()
    {
        Debug.Log($"[C] Arma: {inventory.ActiveWeapon?.Data.ItemName ?? "ninguna"} | Reserva: {inventory.ActiveWeapon?.ReserveAmmo}");

        if (dropPoint == null || ammoBoxToDrop == null || ammoBoxToDrop.Prefab == null)
        {
            Debug.LogWarning("PlayerWeaponInput: falta Drop Point, Ammo Box To Drop o su Prefab.", this);
            return;
        }

        if (!inventory.TryDropAmmo(ammoBoxToDrop.AmmoAmount))
            return;

        Quaternion rotation = Quaternion.Euler(0f, dropPoint.eulerAngles.y, 0f);
        GameObject spawned = Instantiate(ammoBoxToDrop.Prefab, dropPoint.position, rotation);
        if (spawned.TryGetComponent(out ItemPickup pickup))
            pickup.BlockPickup(dropPickupDelay);
    }
}
