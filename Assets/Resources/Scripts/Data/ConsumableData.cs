using UnityEngine;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "Dark Horizon/Items/Consumable")]
public class ConsumableData : ItemData
{
    [Tooltip("Cuanta vida recupera al consumirse.")]
    [SerializeField] private float healAmount = 25f;

    public float HealAmount => healAmount;
}