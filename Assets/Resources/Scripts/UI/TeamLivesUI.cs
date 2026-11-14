using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class TeamLivesUI : MonoBehaviour
{
    private const string TextTable = "Textos";

    [SerializeField] private TMP_Text livesText;
    // Texto de la tabla "Textos" ("Reapariciones: {0}"). Necesita la casilla "Smart" activada.
    [SerializeField] private LocalizedString livesLabel = new LocalizedString(TextTable, "G080");
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 0.35f, 0.35f, 1f);

    private TraitorGameManager manager;
    private int lastLives = -1;

    private void Awake()
    {
        if (livesText != null)
            livesText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    // Al cambiar el idioma se vuelve a escribir el texto con el mismo número de vidas.
    private void OnLocaleChanged(Locale locale)
    {
        if (manager != null)
            Show(lastLives);
    }

    private void Update()
    {
        if (manager == null)
        {
            manager = TraitorGameManager.Instance;
            if (manager == null)
                return;

            manager.OnTeamLivesChanged += Show;
            Show(manager.TeamLives);
        }

        if (livesText != null && manager.ArtifactCollected && livesText.gameObject.activeSelf)
            livesText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.OnTeamLivesChanged -= Show;
    }

    private void Show(int lives)
    {
        lastLives = lives;

        if (livesText == null)
            return;

        bool visible = lives >= 0 && !manager.ArtifactCollected;
        livesText.gameObject.SetActive(visible);
        if (!visible)
            return;

        livesText.text = livesLabel.GetLocalizedString(lives);
        livesText.color = lives > 0 ? normalColor : emptyColor;
    }
}