/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 28-10-2026 |
|                                                                         |
| EnemyHealth.cs | This component manages NPC life value and general      |
| health.                                                                 |
|                                                                         |
\*=======================================================================*/

using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;

    private EnemyData data;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0f;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void SetData(EnemyData newData)
    {
        data = newData;

        if (data != null)
            maxHealth = data.MaxHealth;

        SetHealth(maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        SetHealth(CurrentHealth - amount);

        Debug.Log(
            $"[KHARVEK] Recibió {amount} de daño. " +
            $"Vida: {CurrentHealth}/{maxHealth}"
        );

        if (IsDead)
        {
            Debug.Log("[KHARVEK] Ha sido derrotado.");
            OnDied?.Invoke();
        }
    }

    private void SetHealth(float value)
    {
        CurrentHealth = Mathf.Clamp(value, 0f, maxHealth);

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}