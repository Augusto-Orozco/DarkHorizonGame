using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class BossHealthBarUI : MonoBehaviour
{
    [Tooltip("Jugador dueño de este HUD. Vacío = lo busca en los padres (PlayerHealth)")]
    [SerializeField] private Transform player;

    [Header("Partes")]
    [Tooltip("Lo que se muestra/oculta (hijo de este objeto)")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Image fill;
    [SerializeField] private TMP_Text nameText;

    [Header("Ajustes")]
    [SerializeField] private float showRange = 100f;
    [SerializeField] private float searchInterval = 0.25f;
    [SerializeField] private float smoothSpeed = 1.5f;

    private EnemyBoss currentBoss;
    private float nextSearchTime;

    private void Awake()
    {
        if (player == null)
        {
            PlayerHealth playerHealth = GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
                player = playerHealth.transform;
        }

        if (panel != null)
            panel.SetActive(false);
    }

    private void Update()
    {
        if (Time.time >= nextSearchTime)
        {
            nextSearchTime = Time.time + searchInterval;
            SelectClosestBoss();
        }


        if (currentBoss == null || currentBoss.Health.IsDead)
        {
            Hide();
            return;
        }

        float target = currentBoss.Health.MaxHealth > 0f
            ? currentBoss.Health.CurrentHealth / currentBoss.Health.MaxHealth
            : 0f;
        fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, target, smoothSpeed * Time.deltaTime);
    }

    private void SelectClosestBoss()
    {
        Vector3 origin = GetOrigin();
        EnemyBoss closest = null;
        float closestDistance = showRange;

        foreach (EnemyBoss boss in EnemyBoss.Active)
        {
            if (boss == null || boss.Health.IsDead)
                continue;

            float distance = Vector3.Distance(origin, boss.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = boss;
            }
        }

        if (closest != currentBoss)
            Show(closest);
    }

    private void Show(EnemyBoss boss)
    {
        currentBoss = boss;

        if (boss == null)
        {
            Hide();
            return;
        }

        nameText.text = boss.BossName;
      
        fill.fillAmount = boss.Health.CurrentHealth / boss.Health.MaxHealth;
        panel.SetActive(true);
    }

    private void Hide()
    {
        currentBoss = null;
        if (panel != null && panel.activeSelf)
            panel.SetActive(false);
    }

    private Vector3 GetOrigin()
    {
        if (player != null)
            return player.position;


        return Camera.main != null ? Camera.main.transform.position : Vector3.zero;
    }
}