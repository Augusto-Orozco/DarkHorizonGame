using UnityEngine;

public class WeaponState 
{
    public WeaponData Data { get; }
    public int AmmoInMagazine { get; private set; }
    public int ReserveAmmo { get; private set; }

    public bool UsesAmmo => Data is AmmoWeaponData;

    public WeaponState(WeaponData data)
    {
        Data = data;
        if(data is AmmoWeaponData ammoWeaponData)
        {
            AmmoInMagazine = ammoWeaponData.MagazineSize;
            ReserveAmmo = 0;
        }
    }

    public int AddReserveAmmo(int amount)
    {
        if (!(Data is AmmoWeaponData ammoData) || amount <= 0)
            return 0;

        int space = ammoData.MaxReserveAmmo - ReserveAmmo;
        int added = Mathf.Min(space, amount);
        ReserveAmmo += added;
        return added;
    }
}
