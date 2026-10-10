using UnityEngine;

public class ItemPickup : Pickup
{
    [SerializeField] private ItemData item;

    public ItemData Item => item;

    protected override void Awake()
    {
        base.Awake();
        if (item == null)
            Debug.LogWarning("ItemPickup sin ItemData asignado");
        else if (item is WeaponData)
            Debug.LogWarning("Las armas usan GroundWeapon no ItemPickup");

    }
   
    protected override void OnPlayerEnter(PlayerInventory inventory)
    {
        TryGiveTo(inventory);
    }

    protected override void OnPlayerStay(PlayerInventory inventory)
    {
        TryGiveTo(inventory);
    }

    private void TryGiveTo(PlayerInventory inventory)
    {
        if (item == null)
            return;

        if (inventory.TryCollect(item))
            Collect(inventory);
    }
}
