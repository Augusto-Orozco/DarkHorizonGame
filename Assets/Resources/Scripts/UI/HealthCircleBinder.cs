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
        
    }

    private void OnDisable()
    {
        if (health != null)
            HandleHealthChanged(health.CurrentHealth, health.MaxHealth); ;

        health.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        circle.SetValue(max > 0f ? current / max : 0f);
    }
}