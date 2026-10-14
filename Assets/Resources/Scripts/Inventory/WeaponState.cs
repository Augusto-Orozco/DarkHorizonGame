using UnityEngine;

public class WeaponState
{
    public WeaponData Data { get; }
    public int AmmoInMagazine { get; private set; }
    public int ReserveAmmo { get; private set; }

    public float Heat { get; private set; }
    public bool IsOverheated { get; private set; }
    public bool UsesHeat => Data is HeatWeaponData;
    public bool CanFire => !IsOverheated;
    private float lastShotTime;

    public bool UsesAmmo => Data is AmmoWeaponData;

    public WeaponState(WeaponData data)
    {
        Data = data;
        if (data is AmmoWeaponData ammoWeaponData)
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

    public bool TryRemoveReserveAmmo(int amount)
    {
        if (!UsesAmmo || amount <= 0 || ReserveAmmo < amount)
            return false;

        ReserveAmmo -= amount;
        return true;
    }

    public void AddShotHeat()
    {
        if (!(Data is HeatWeaponData heatData))
            return;
        lastShotTime = Time.time;
        Heat = Mathf.Min(Heat + heatData.HeatPerShot, heatData.MaxHeat);

        if (Heat >= heatData.MaxHeat)
            IsOverheated = true;
    }

    public void Cool(float deltaTime)
    {
        if (!(Data is HeatWeaponData heatData) || Heat <= 0f)
            return;
        if (Time.time - lastShotTime < heatData.CoolDelay)
            return;

        Heat = Mathf.Max(0f, Heat - heatData.CoolingPerSecond * deltaTime);

        if (IsOverheated && Heat <= heatData.ResumeAtHeat)
            IsOverheated = false;

    }
}
