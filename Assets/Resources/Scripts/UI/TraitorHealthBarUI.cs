using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TraitorHealthBarUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Image fill;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private string label = "TRAIDOR";
    [SerializeField] private float smoothSpeed = 1.5f;

    private PlayerHealth traitorHealth;
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
        nameText.text = label;
        fill.fillAmount = 1f;
        panel.SetActive(true);
    }
}