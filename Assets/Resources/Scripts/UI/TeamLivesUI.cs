using TMPro;
using UnityEngine;

public class TeamLivesUI : MonoBehaviour
{
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private string format = "Reapariciones: {0}";
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 0.35f, 0.35f, 1f);

    private TraitorGameManager manager;

    private void Awake()
    {
        if (livesText != null)
            livesText.gameObject.SetActive(false);
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
        if (livesText == null)
            return;

        bool visible = lives >= 0 && !manager.ArtifactCollected;
        livesText.gameObject.SetActive(visible);
        if (!visible)
            return;

        livesText.text = string.Format(format, lives);
        livesText.color = lives > 0 ? normalColor : emptyColor;
    }
}