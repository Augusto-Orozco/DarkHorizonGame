using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Capacidad")]
    [SerializeField] private int maxGrenades = 3;
    [SerializeField] private int maxBreads = 3;

    //Estado
    private WeaponState primary;
    private WeaponState secondary;
    private WeaponSlotType activeSlot = WeaponSlotType.Primary;

    private ThrowableData grenadeType;
    private int grenadeCount;
    private ConsumableData breadType;
    private int breadCount;
    private ItemData item;

    //Eventos
    public event Action<WeaponSlotType, WeaponState> OnWeaponChanged;
    public event Action<WeaponSlotType> OnActiveSlotChanged;
    public event Action<WeaponSlotType, WeaponState> OnAmmoChanged;
    public event Action<int> OnGrenadesChanged;
    public event Action<int> OnBreadsChanged;
    public event Action<ItemData> OnItemChanged;
    public event Action<ConsumableData> OnBreadConsumed;

    //Lectura general publica
    public WeaponSlotType ActiveSlot => activeSlot;
    public WeaponState ActiveWeapon => GetWeapon(activeSlot);
    public int GrenadeCount => grenadeCount;
    public int BreadCount => breadCount;
    public int MaxGrenades => maxGrenades;
    public int MaxBreads => maxBreads;
    public ItemData Item => item;

    public WeaponState GetWeapon(WeaponSlotType slot)
    {
        return slot == WeaponSlotType.Primary ? primary : secondary;
    }

    //Armas
    public WeaponState EquipWeapon(WeaponState weapon)
    {
        if (weapon == null || weapon.Data == null)
            return null;
        WeaponSlotType slot = weapon.Data.Slot;
        WeaponState previous = GetWeapon(slot);

        SetWeapon(slot, weapon);
        SetActiveSlot(slot);
        return previous;
    }

    public WeaponState DropActiveWeapon()
    {
        WeaponState dropped = ActiveWeapon;
        if (dropped == null)
            return null;
        SetWeapon(activeSlot, null);

        WeaponSlotType other = GetOtherSlot(activeSlot);
        if (GetWeapon(other) != null)
            SetActiveSlot(other);
        return dropped;

    }

    public bool SelectSlot(WeaponSlotType slot)
    {
        if (GetWeapon(slot) == null)
            return false;
        SetActiveSlot(slot);
        return true;
    }

    public bool SwitchWeapon()
    {
        return SelectSlot(GetOtherSlot(activeSlot));
    }


    //Items
    public bool TryCollect(ItemData collected)
    {
        if (collected is ConsumableData bread)
            return TryAddBread(bread);  
        if (collected is ThrowableData grenade)
            return TryAddGrenade(grenade);
        if (collected is AmmoBoxData ammoBox)
            return TryAddAmmo(ammoBox.AmmoAmount);
        if (collected is WeaponData)
        {
            Debug.LogWarning("Las armas se equipan con EquipWeapon, no se recogen con TryCollect");
            return false;
        }
        return TrySetItem(collected);
           
    }

    public bool TryConsumeBread()
    {
        if (breadCount <= 0)
            return false;
        breadCount--;
        OnBreadsChanged?.Invoke(breadCount);
        OnBreadConsumed?.Invoke(breadType);
        return true;
    }

    public bool TryTakeGrenade(out ThrowableData grenade)
    {
        grenade = grenadeType;
        if (grenadeCount <= 0)
            return false;

        grenadeCount--;
        OnGrenadesChanged?.Invoke(grenadeCount);
        return true;
    }

    public bool TryUseItem(out ItemData used)
    {
        used = item;
        if (item == null)
            return false;
        item = null;
        OnItemChanged?.Invoke(item);
        return true;
    }

    //Internos

    private bool TryAddBread(ConsumableData bread)
    {
        if (bread == null || breadCount >= maxBreads)
            return false;

        if (breadCount > 0 && breadType != bread)
            return false;

        breadType = bread;
        breadCount++;
        OnBreadsChanged?.Invoke(breadCount);
        return true;
    }

    private bool TryAddGrenade(ThrowableData grenade)
    {
        if (grenade == null || grenadeCount >= maxGrenades)
            return false;

        if (grenadeCount > 0 && grenadeType != grenade)
            return false;

        grenadeType = grenade;
        grenadeCount++;
        OnGrenadesChanged?.Invoke(grenadeCount);
        return true;
    }

    private bool TryAddAmmo(int amount)
    {
        if (TryAddAmmoTo(activeSlot, amount))
            return true;

        return TryAddAmmoTo(GetOtherSlot(activeSlot), amount);
    }

    private bool TryAddAmmoTo(WeaponSlotType slot, int amount)
    {
        WeaponState weapon = GetWeapon(slot);
        if (weapon == null || !weapon.UsesAmmo)
            return false;

        int added = weapon.AddReserveAmmo(amount);
        if (added <= 0)
            return false;

        OnAmmoChanged?.Invoke(slot, weapon);
        return true;
    }

    private bool TrySetItem(ItemData newItem)
    {
        if (newItem == null || item != null)
            return false;

        item = newItem;
        OnItemChanged?.Invoke(item);
        return true;
    }

    private void SetWeapon(WeaponSlotType slot, WeaponState weapon)
    {
        if (slot == WeaponSlotType.Primary)
            primary = weapon;
        else
            secondary = weapon;

        OnWeaponChanged?.Invoke(slot, weapon);
    }

    private void SetActiveSlot(WeaponSlotType slot)
    {
        if (slot == activeSlot)
            return;

        activeSlot = slot;
        OnActiveSlotChanged?.Invoke(activeSlot);
    }

    private static WeaponSlotType GetOtherSlot(WeaponSlotType slot)
    {
        return slot == WeaponSlotType.Primary ? WeaponSlotType.Secondary : WeaponSlotType.Primary;
    }
}
