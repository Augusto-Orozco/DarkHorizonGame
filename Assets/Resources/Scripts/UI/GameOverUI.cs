
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private string menuSceneName = "StartScene";

    [Header("Textos")]
    [SerializeField] private string victoryTitle = "VICTORIA";
    [SerializeField] private string defeatTitle = "DERROTA";
    [SerializeField] private string crewExtractedText = "El escuadrón escapó con el artefacto.";
    [SerializeField] private string traitorDefeatedText = "El traidor fue eliminado.";
    [SerializeField] private string traitorWinsText = "El traidor acabó con el escuadrón.";

    [SerializeField] private string squadWipedText = "El escuadrón cayó antes de recuperar el artefacto.";

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

        titleText.text = won ? victoryTitle : defeatTitle;
        subtitleText.text = result switch
        {
            TraitorGameManager.GameResult.CrewExtracted => crewExtractedText,
            TraitorGameManager.GameResult.TraitorDefeated => traitorDefeatedText,
            TraitorGameManager.GameResult.SquadWiped => squadWipedText,
            _ => traitorWinsText
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