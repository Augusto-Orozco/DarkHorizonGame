
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    private const string TextTable = "Textos";

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private string menuSceneName = "StartScene";

    // Textos de la tabla "Textos". Ya traen su clave por defecto; se pueden cambiar en el Inspector.
    [Header("Textos (tabla Textos)")]
    [SerializeField] private LocalizedString victoryTitle = new LocalizedString(TextTable, "G074");
    [SerializeField] private LocalizedString defeatTitle = new LocalizedString(TextTable, "G075");
    [SerializeField] private LocalizedString crewExtractedText = new LocalizedString(TextTable, "G076");
    [SerializeField] private LocalizedString traitorDefeatedText = new LocalizedString(TextTable, "G077");
    [SerializeField] private LocalizedString traitorWinsText = new LocalizedString(TextTable, "G078");

    [SerializeField] private LocalizedString squadWipedText = new LocalizedString(TextTable, "G079");

    private bool subscribed;

    private void Awake()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private void Update()
    {
        if (!subscribed && TraitorGameManager.Instance != null)
        {
            TraitorGameManager.Instance.OnGameOver += Show;
            subscribed = true;
        }
    }

    private void OnDestroy()
    {
        if (subscribed && TraitorGameManager.Instance != null)
            TraitorGameManager.Instance.OnGameOver -= Show;
    }

    private void Show(TraitorGameManager.GameResult result)
    {
        bool iAmTraitor = TraitorGameManager.Instance.IsLocalTraitor;
        bool traitorWon = result == TraitorGameManager.GameResult.TraitorWins;
        bool won = result != TraitorGameManager.GameResult.SquadWiped && traitorWon == iAmTraitor;

        titleText.text = (won ? victoryTitle : defeatTitle).GetLocalizedString();
        subtitleText.text = result switch
        {
            TraitorGameManager.GameResult.CrewExtracted => crewExtractedText.GetLocalizedString(),
            TraitorGameManager.GameResult.TraitorDefeated => traitorDefeatedText.GetLocalizedString(),
            TraitorGameManager.GameResult.SquadWiped => squadWipedText.GetLocalizedString(),
            _ => traitorWinsText.GetLocalizedString()
        };

        panel.SetActive(true);

        PlayerMovement movement = GetComponentInParent<PlayerMovement>();
        if (movement != null)
            movement.readInput = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void BackToMenu()
    {
        ReturnToMenu.Go(menuSceneName);
    }

}