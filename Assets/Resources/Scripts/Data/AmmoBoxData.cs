using UnityEngine;

[CreateAssetMenu(fileName = "NewAmmoBox", menuName = "Dark Horizon/Items/Ammo Box")]
public class AmmoBoxData : ItemData
{
    [Tooltip("Balas de reserva que da al recogerla.")]
    [SerializeField] private int ammoAmount = 25;

    public int AmmoAmount => ammoAmount;
}