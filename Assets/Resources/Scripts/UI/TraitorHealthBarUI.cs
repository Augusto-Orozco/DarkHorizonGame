using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class TraitorHealthBarUI : MonoBehaviour
{
    private const string TextTable = "Textos";

    [SerializeField] private GameObject panel;
    [SerializeField] private Image fill;
    [SerializeField] private TMP_Text nameText;
    // Texto de la tabla "Textos" ("TRAIDOR").
    [SerializeField] private LocalizedString label = new LocalizedString(TextTable, "G081");
    [SerializeField] private float smoothSpeed = 1.5f;

    private PlayerHealth traitorHealth;
    private bool subscribed;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    // Al cambiar el idioma se vuelve a escribir el nombre mientras la barra se muestra.
    private void OnLocaleChanged(Locale locale)
    {
        if (traitorHealth != null && nameText != null)
            nameText.text = label.GetLocalizedString();
    }

    private void Update()
    {
        if (!subscribed && TraitorGameManager.Instance != null)
        {
            TraitorGameManager.Instance.OnTraitorAssigned += Show;
            subscribed = true;
        }

        if (traitorHealth == null)
            return;

        if (traitorHealth.IsDead)
        {
            panel.SetActive(false);
            traitorHealth = null;
            return;
        }

        float target = traitorHealth.MaxHealth > 0f ? traitorHealth.CurrentHealth / traitorHealth.MaxHealth : 0f;
        fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, target, smoothSpeed * Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (subscribed && TraitorGameManager.Instance != null)
            TraitorGameManager.Instance.OnTraitorAssigned -= Show;
    }

    private void Show(PlayerHealth traitor)
    {
        traitorHealth = traitor;
        nameText.text = label.GetLocalizedString();
        fill.fillAmount = 1f;
        panel.SetActive(true);
    }
}