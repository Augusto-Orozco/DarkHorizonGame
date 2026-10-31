/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 31-10-2026 |
| Omar o Roge no sé qpd....                                               | 
|                                                                         |
| HealthCircleBinder.cs | Updates the UI health distribution component,   |
| changes on real time, if healed or if damaged.                          |
|                                                                         |
\*=======================================================================*/

using UnityEngine;

public class HealthCircleBinder : MonoBehaviour
{
    [SerializeField] private PlayerHealth health;
    [SerializeField] private FillCircleUI circle;

    private void OnEnable()
    {
        if (health == null || circle == null)
        {
            Debug.LogWarning("Falta algo", this);
            return;
        }

        health.OnHealthChanged += HandleHealthChanged;

        HandleHealthChanged(
            health.CurrentHealth,
            health.MaxHealth
        );
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        circle.SetValue(max > 0f ? current / max : 0f);
    }
}