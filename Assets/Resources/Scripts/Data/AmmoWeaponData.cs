using UnityEngine;

[CreateAssetMenu(fileName = "NewAmmoWeapon", menuName = "Dark Horizon/Weapons/Ammo Weapon")]
public class AmmoWeaponData : WeaponData
{
    [Header("Balas")]
    [SerializeField] private int magazineSize = 25;
    [Tooltip("Maximo de balas que se pueden cargar de reserva.")]
    [SerializeField] private int maxReserveAmo = 75;

    public int MagazineSize => magazineSize;
    public int MaxReserveAmmo=> maxReserveAmo;
}