using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class WeaponSlotUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private WeaponSlotType slot = WeaponSlotType.Primary;

    [Header("Partes")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private GameObject heatBar;
    [SerializeField] private Image heatFill;

    [Header("Colores del calor")]
    [SerializeField] private Color heatColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color overheatColor = Color.red;

    private WeaponState weapon;

    private void OnEnable()
    {
        if(inventory == null)
        {
            Debug.LogWarning("WeaponSlotUI sin PlayerInventory", this);
            return;
        }

        inventory.OnWeaponChanged += HandleWeaponChanged;
        inventory.OnAmmoChanged += HandleAmmoChanged;
        Refresh();
    }
    private void OnDisable()
    {
        if (inventory == null)
            return;

        inventory.OnWeaponChanged -= HandleWeaponChanged;
        inventory.OnAmmoChanged -= HandleAmmoChanged;
    }

    private void Update()
    {
        UpdateHeat();
    }

    private void HandleWeaponChanged(WeaponSlotType changedSlot, WeaponState newWeapon)
    {
        if (changedSlot == slot)
            Refresh();
    }

    private void HandleAmmoChanged(WeaponSlotType changedSlot, WeaponState changedWeapon)
    {
        if (changedSlot == slot)
            UpdateAmmo();
    }

    private void Refresh()
    {
        weapon = inventory.GetWeapon(slot);
        bool hasWeapon = weapon != null;

        icon.enabled = hasWeapon && weapon.Data.Icon != null;
        if (icon.enabled)
            icon.sprite = weapon.Data.Icon;

        ammoText.gameObject.SetActive(hasWeapon && weapon.UsesAmmo);
        heatBar.SetActive(hasWeapon && weapon.UsesHeat);

        UpdateAmmo();
        UpdateHeat();
    }

    private void UpdateAmmo()
    {
        if (weapon == null || !weapon.UsesAmmo)
            return;

        ammoText.text = $"{weapon.AmmoInMagazine} / {weapon.ReserveAmmo}";
    }

    private void UpdateHeat()
    {
        if (weapon == null || !(weapon.Data is HeatWeaponData heatData))
            return;

        heatFill.fillAmount = weapon.Heat / heatData.MaxHeat;
        heatFill.color = weapon.IsOverheated ? overheatColor : heatColor;
    }

}
