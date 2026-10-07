using UnityEngine;

public abstract class WeaponData : ItemData
{
    [Header("Arma")]
    [SerializeField] private WeaponSlotType slot = WeaponSlotType.Primary;
    [SerializeField] private float damage = 10f;
    [Tooltip("Segundos entre cada disparo. Mas alto = dispara mas lento.")]
    [SerializeField] private float timeBetweenShots = 0.2f;

    public WeaponSlotType Slot => slot;
    public float Damage => damage;
    public float TimeBetweenShots => timeBetweenShots;
}