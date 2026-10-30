using UnityEngine;
using TMPro;
using UnityEngine.UI;



public class ItemCountUI : MonoBehaviour
{
    private enum CountType { Bread, Grenade }

    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private CountType type = CountType.Bread;

    [Header("Partes")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;

    [Header("Colores")]
    [SerializeField] private Color filledColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.3f);

    private int CurrentCount => type == CountType.Bread ? inventory.BreadCount : inventory.GrenadeCount;

    private void OnEnable()
    {
        if (inventory == null)
        {
            Debug.LogWarning("ItemCountUI sin PlayerInventory asignado.", this);
            return;
        }

        if (type == CountType.Bread)
            inventory.OnBreadsChanged += HandleCountChanged;
        else
            inventory.OnGrenadesChanged += HandleCountChanged;

        Show(CurrentCount);
    }

    private void OnDisable()
    {
        if (inventory == null)
            return;

        if (type == CountType.Bread)
            inventory.OnBreadsChanged -= HandleCountChanged;
        else
            inventory.OnGrenadesChanged -= HandleCountChanged;
    }

    private void HandleCountChanged(int count)
    {
        Show(count);
    }

    private void Show(int count)
    {
        bool hasAny = count > 0;
        icon.color = hasAny ? filledColor : emptyColor;
        countText.gameObject.SetActive(hasAny);
        countText.text = $"x{count}";
    }
}
