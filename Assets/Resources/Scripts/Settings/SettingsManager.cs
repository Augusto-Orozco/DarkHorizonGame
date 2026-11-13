using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// Panel de ajustes de la escena. Hay uno por escena: los valores viven en GameSettings.
// Los sliders, toggles y el input se conectan por código; los botones van por OnClick.
// Todos los campos son opcionales menos settingsPanel.
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [SerializeField] private GameObject settingsPanel;

    [Header("Audio")]
    [SerializeField] private Slider volumeSlider;

    [Header("Controles")]
    [Tooltip("Campo numérico (como en Valorant). El valor se limita a 0.1 - 5.")]
    [SerializeField] private TMP_InputField sensitivityInput;
    [SerializeField] private Toggle invertYToggle;

    [Header("Video")]
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Idioma")]
    [Tooltip("Se llena solo con los idiomas de Localization. Opcional.")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    private List<Locale> locales;

    public bool IsOpen => settingsPanel != null && settingsPanel.activeSelf;

    // Se dispara al cerrar el panel; el PauseMenu lo usa para volver a mostrarse.
    public event Action Closed;

    private bool refreshing;
    private bool wasOpen;

    private void Awake()
    {
        // Hay uno por escena: cada escena tiene su propio panel.
        Instance = this;

        GameSettings.EnsureLoaded();
        ConfigureSlider(volumeSlider, 0f, 1f);
        Hook();
        RefreshUI(); // el panel muestra los valores reales desde el inicio
        settingsPanel.SetActive(false);
    }

    // Llena el dropdown con los idiomas disponibles cuando Localization termina de iniciar.
    private IEnumerator Start()
    {
        if (languageDropdown == null)
            yield break;

        yield return LocalizationSettings.InitializationOperation;

        locales = new List<Locale>(LocalizationSettings.AvailableLocales.Locales);
        var names = new List<string>();
        foreach (Locale l in locales)
            names.Add(GetLanguageName(l));

        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(names);
        RefreshUI();
    }

    private void Update()
    {
        // Si el panel se oculta por cualquier medio (StartMenu.Back, ESC, etc.),
        // se descartan los cambios sin guardar.
        bool open = IsOpen;
        if (wasOpen && !open)
            GameSettings.Load();
        wasOpen = open;
    }

    private void OnEnable()
    {
        // Mantiene los controles actualizados sin importar cómo se abrió el panel.
        GameSettings.Changed += RefreshUI;
    }

    private void OnDisable()
    {
        GameSettings.Changed -= RefreshUI;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private static void ConfigureSlider(Slider s, float min, float max)
    {
        if (s == null) return;
        s.wholeNumbers = false;
        s.minValue = min;
        s.maxValue = max;
    }

    private void Hook()
    {
        if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(v => { if (!refreshing) GameSettings.SetVolume(v); });
        if (sensitivityInput != null)
        {
            sensitivityInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            sensitivityInput.onEndEdit.AddListener(OnSensitivityTyped);
        }
        if (invertYToggle != null) invertYToggle.onValueChanged.AddListener(v => { if (!refreshing) GameSettings.SetInvertY(v); });
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(v => { if (!refreshing) GameSettings.SetFullscreen(v); });
        if (languageDropdown != null)
            languageDropdown.onValueChanged.AddListener(i =>
            {
                if (!refreshing && locales != null && i >= 0 && i < locales.Count)
                    GameSettings.SetLanguage(locales[i].Identifier.Code);
            });

    }

    public void OpenSettings()
    {
        RefreshUI();
        settingsPanel.SetActive(true);
    }

    // Cierra el panel y descarta los cambios sin guardar.
    public void CloseSettings()
    {
        if (!IsOpen)
            return;

        GameSettings.Load(); // vuelve a lo guardado
        settingsPanel.SetActive(false);
        Closed?.Invoke();
    }

    // Guarda los ajustes en PlayerPrefs.
    public void Apply()
    {
        GameSettings.Save();
    }

    public void ResetDefaults()
    {
        GameSettings.ResetToDefaults();
        RefreshUI();
    }

    // El jugador escribió un valor: se valida, se limita al rango y se muestra el valor final.
    private void OnSensitivityTyped(string text)
    {
        // Acepta "1.5" y "1,5" sin importar el idioma del sistema.
        string normalized = text.Trim().Replace(',', '.');
        if (float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && !float.IsNaN(value))
            GameSettings.SetSensitivity(value); // limita a 0.1 - 5 (0 -> 0.1, 10000 -> 5)

        // Muestra el valor final (ya limitado); si no era un número, vuelve al valor actual.
        UpdateSensitivityText();
    }

    private void UpdateSensitivityText()
    {
        if (sensitivityInput != null)
            sensitivityInput.SetTextWithoutNotify(GameSettings.Sensitivity.ToString("0.00", CultureInfo.InvariantCulture));
    }

    private void RefreshUI()
    {
        refreshing = true;
        if (volumeSlider != null) volumeSlider.value = GameSettings.Volume;
        UpdateSensitivityText();
        if (invertYToggle != null) invertYToggle.isOn = GameSettings.InvertY;
        if (fullscreenToggle != null) fullscreenToggle.isOn = GameSettings.Fullscreen;
        RefreshLanguageDropdown();
        refreshing = false;
    }

    // Nombre del idioma en su propio idioma, con la primera letra en mayúscula ("Español", "English").
    private static string GetLanguageName(Locale locale)
    {
        CultureInfo culture = locale.Identifier.CultureInfo;
        if (culture == null || string.IsNullOrEmpty(culture.NativeName))
            return locale.LocaleName;

        string name = culture.NativeName;
        return char.ToUpper(name[0], culture) + name.Substring(1);
    }

    // Marca en el dropdown el idioma activo en Localization.
    private void RefreshLanguageDropdown()
    {
        if (languageDropdown == null || locales == null)
            return;

        Locale selected = LocalizationSettings.SelectedLocale;
        int index = selected != null ? locales.IndexOf(selected) : -1;
        if (index >= 0)
            languageDropdown.SetValueWithoutNotify(index);
    }
}
