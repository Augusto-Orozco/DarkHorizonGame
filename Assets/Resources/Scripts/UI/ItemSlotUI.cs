using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Image icon;

    private void OnEnable()
    {
        if (inventory == null)
        {
            Debug.LogWarning("ItemSlotUI sin PlayerInventory asignado.", this);
            return;
        }

        inventory.OnItemChanged += HandleItemChanged;
        Show(inventory.Item);
    }

    private void OnDisable()
    {
        if (inventory == null)
            return;

        inventory.OnItemChanged -= HandleItemChanged;
    }

    private void HandleItemChanged(ItemData item)
    {
        Show(item);
    }

    private void Show(ItemData item)
    {
        bool hasIcon = item != null && item.Icon != null;
        icon.enabled = hasIcon;
        if (hasIcon)
            icon.sprite = item.Icon;
    }
}