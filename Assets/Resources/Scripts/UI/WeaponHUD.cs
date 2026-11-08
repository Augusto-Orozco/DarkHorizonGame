using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponHUD : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;

    [Header("Arma activa")]
    [SerializeField] private Image mainIcon;
    [SerializeField] private TMP_Text ammoText;

    [Header("Arma secundaria")]
    [SerializeField] private GameObject secondaryRoot;
    [SerializeField] private Image secondaryIcon;

    [Header("Puntos de balas")]
    [SerializeField] private RectTransform dotsContainer;
    [SerializeField] private Sprite dotSprite;
    [SerializeField, Min(4)] private int maxDots = 40;
    [SerializeField, Min(1f)] private float dotSize = 10f;
    [SerializeField] private float radius = 95f;
    [SerializeField] private float startAngle = 90f;
    [SerializeField, Range(10f, 360f)] private float arcDegrees = 360f;

    [Header("Colores")]
    [SerializeField] private Color loadedColor = new Color(0.6f, 1f, 0.5f, 1f);
    [SerializeField] private Color reserveColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color emptyColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);
    [SerializeField] private Color heatColor = new Color(1f, 0.6f, 0.1f, 1f);
    [SerializeField] private Color overheatColor = Color.red;

    [Header("Cambio de arma")]
    [SerializeField, Min(1f)] private float swapPunchScale = 1.2f;
    [SerializeField, Min(0.1f)] private float swapPunchSpeed = 6f;

    private readonly List<Image> dots = new List<Image>();
    private WeaponState active;
    private int builtDotCount = -1;
    private float punch;

    private void Awake()
    {
        if (inventory == null)
            inventory = GetComponentInParent<PlayerInventory>();
    }

    private void OnEnable()
    {
        if (inventory == null)
        {
            Debug.LogWarning("WeaponHUD sin PlayerInventory.", this);
            return;
        }

        inventory.OnWeaponChanged += HandleWeaponChanged;
        inventory.OnActiveSlotChanged += HandleActiveSlotChanged;
        inventory.OnAmmoChanged += HandleAmmoChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (inventory == null)
            return;

        inventory.OnWeaponChanged -= HandleWeaponChanged;
        inventory.OnActiveSlotChanged -= HandleActiveSlotChanged;
        inventory.OnAmmoChanged -= HandleAmmoChanged;
    }

    private void Update()
    {
        if (active != null && active.UsesHeat)
            PaintHeat();

        if (punch > 0f && mainIcon != null)
        {
            punch = Mathf.MoveTowards(punch, 0f, swapPunchSpeed * Time.deltaTime);
            mainIcon.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, swapPunchScale, punch);
        }
    }

    private void HandleWeaponChanged(WeaponSlotType slot, WeaponState weapon)
    {
        Refresh();
    }

    private void HandleActiveSlotChanged(WeaponSlotType slot)
    {
        Refresh();
        punch = 1f;
    }

    private void HandleAmmoChanged(WeaponSlotType slot, WeaponState weapon)
    {
        if (weapon == active)
            PaintAmmo();
    }

    private void Refresh()
    {
        active = inventory.ActiveWeapon;
        WeaponSlotType otherSlot = inventory.ActiveSlot == WeaponSlotType.Primary
            ? WeaponSlotType.Secondary
            : WeaponSlotType.Primary;
        WeaponState other = inventory.GetWeapon(otherSlot);

        SetIcon(mainIcon, active);

        bool hasOther = other != null && other.Data.Icon != null;
        if (secondaryRoot != null)
            secondaryRoot.SetActive(hasOther);
        if (hasOther)
            SetIcon(secondaryIcon, other);

        if (active == null)
        {
            BuildDots(0);
            SetAmmoText(string.Empty);
            return;
        }

        if (active.UsesAmmo)
            PaintAmmo();
        else if (active.UsesHeat)
            PaintHeat();
        else
        {
            BuildDots(0);
            SetAmmoText(string.Empty);
        }
    }

    private static void SetIcon(Image image, WeaponState weapon)
    {
        if (image == null)
            return;

        bool show = weapon != null && weapon.Data.Icon != null;
        image.enabled = show;
        if (show)
            image.sprite = weapon.Data.Icon;
    }

    private void PaintAmmo()
    {
        if (active == null || !(active.Data is AmmoWeaponData ammoData))
            return;

        int capacity = ammoData.MagazineSize + ammoData.MaxReserveAmmo;
        int bulletsPerDot = Mathf.Max(1, Mathf.CeilToInt(capacity / (float)maxDots));
        int dotCount = Mathf.CeilToInt(capacity / (float)bulletsPerDot);
        BuildDots(dotCount);

        int loadedDots = Mathf.CeilToInt(active.AmmoInMagazine / (float)bulletsPerDot);
        int usableDots = Mathf.CeilToInt((active.AmmoInMagazine + active.ReserveAmmo) / (float)bulletsPerDot);

        for (int i = 0; i < dots.Count; i++)
        {
            if (i < loadedDots)
                dots[i].color = loadedColor;
            else if (i < usableDots)
                dots[i].color = reserveColor;
            else
                dots[i].color = emptyColor;
        }

        SetAmmoText($"{active.AmmoInMagazine} / {active.ReserveAmmo}");
    }

    private void PaintHeat()
    {
        if (active == null || !(active.Data is HeatWeaponData heatData))
            return;

        BuildDots(maxDots);

        float remaining = 1f - Mathf.Clamp01(active.Heat / heatData.MaxHeat);
        int litDots = Mathf.CeilToInt(remaining * dots.Count);
        Color lit = active.IsOverheated ? overheatColor : heatColor;

        for (int i = 0; i < dots.Count; i++)
            dots[i].color = i < litDots ? lit : emptyColor;

        SetAmmoText(string.Empty);
    }

    private void SetAmmoText(string text)
    {
        if (ammoText == null)
            return;

        ammoText.gameObject.SetActive(!string.IsNullOrEmpty(text));
        ammoText.text = text;
    }

    private void BuildDots(int count)
    {
        if (dotsContainer == null || count == builtDotCount)
            return;

        builtDotCount = count;

        while (dots.Count < count)
        {
            GameObject dot = new GameObject($"Punto {dots.Count}", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(dotsContainer, false);
            Image image = dot.GetComponent<Image>();
            image.sprite = dotSprite;
            image.raycastTarget = false;
            dots.Add(image);
        }

        for (int i = 0; i < dots.Count; i++)
            dots[i].gameObject.SetActive(i < count);

        if (count == 0)
            return;

        bool fullCircle = arcDegrees >= 359.9f;
        float step = fullCircle ? arcDegrees / count : arcDegrees / Mathf.Max(1, count - 1);

        for (int i = 0; i < count; i++)
        {
            float angle = (startAngle - step * i) * Mathf.Deg2Rad;
            RectTransform rect = dots[i].rectTransform;
            rect.sizeDelta = new Vector2(dotSize, dotSize);
            rect.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
    }
}