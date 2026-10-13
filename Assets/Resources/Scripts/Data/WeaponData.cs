using Unity.VisualScripting;
using UnityEngine;

public abstract class WeaponData : ItemData
{
    [Header("Arma")]
    [SerializeField] private WeaponSlotType slot = WeaponSlotType.Primary;
    [SerializeField] private float damage = 10f;
    [Tooltip("Segundos entre cada disparo. Mas alto = dispara mas lento.")]
    [SerializeField] private float timeBetweenShots = 0.2f;
    [Header("En la mano")]
    [Tooltip("Modelo que se ve en la mano del jugador")]
    [SerializeField] private GameObject heldPrefab;
    [Tooltip("Activar si el Held Prefab ya trae su brazo y hay que esconder el brazo derecho")]
    [SerializeField] private bool hidesRightArm;

    public WeaponSlotType Slot => slot;
    public float Damage => damage;
    public float TimeBetweenShots => timeBetweenShots;

    public GameObject HeldPrefab => heldPrefab;
    public bool HidesRightArm => hidesRightArm;
}