/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 30-10-2026 | 
|                                                                         |
| EnemyHealthBar.cs | Updates the enemy healthbar when registers any      |
| taken damage.                                                           |
|                                                                         |
\*=======================================================================*/


using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Camera targetCamera;

    private void Start()
    {
        if (enemyHealth == null || healthSlider == null)
        {
            Debug.LogWarning(
                "[EnemyHealthBar] Faltan referencias.",
                this
            );
            return;
        }

        if (targetCamera == null)
            targetCamera = Camera.main;

        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;

        UpdateHealthBar(
            enemyHealth.CurrentHealth,
            enemyHealth.MaxHealth
        );

        enemyHealth.OnHealthChanged += UpdateHealthBar;
    }

    private void LateUpdate()
    {
        if (targetCamera != null)
        {
            transform.rotation = targetCamera.transform.rotation;
        }
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
            enemyHealth.OnHealthChanged -= UpdateHealthBar;
    }

    private void UpdateHealthBar(float current, float max)
    {
        healthSlider.value = max > 0f ? current / max : 0f;
    }
}