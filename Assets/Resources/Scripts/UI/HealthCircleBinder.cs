using UnityEngine;

public class HealthCircleBinder : MonoBehaviour
{
    [SerializeField] private PlayerHealth health;
    [SerializeField] private FillCircleUI circle;

    private void Awake()
    {
        if (health == null)
            health = GetComponentInParent<PlayerHealth>();
    }

    private void OnEnable()
    {
        if (health == null || circle == null)
        {
            Debug.LogWarning("HealthCircleBinder sin PlayerHealth o FillCircleUI.", this);
            return;
        }

        health.OnHealthChanged += HandleHealthChanged;
        HandleHealthChanged(health.CurrentHealth, health.MaxHealth);
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