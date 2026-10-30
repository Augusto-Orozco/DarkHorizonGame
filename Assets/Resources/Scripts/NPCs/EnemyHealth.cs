/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 28-10-2026 | 
|                                                                         |
| EnemyHealth.cs | This component manages NPC life value amd general      |
| health.                                                                 |
|                                                                         |
\*=======================================================================*/


using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyData data;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => data != null ? data.MaxHealth : 100f;
    public bool IsDead { get; private set; }

    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;

    private void Awake()
    {
        if (data == null)
        {
            Debug.LogError(
                "EnemyHealth needs an EnemyData asignation.",
                this
            );
            enabled = false;
            return;
        }

        CurrentHealth = data.MaxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0f)
        {
            IsDead = true;
            OnDied?.Invoke();
            Destroy(gameObject);
        }
    }
}
