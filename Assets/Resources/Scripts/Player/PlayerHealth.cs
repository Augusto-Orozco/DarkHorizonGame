using System;
using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerHealth : MonoBehaviour, IDamageable
{ //                                     // Interface that registers taken damage   
    [SerializeField] private float maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0f;
    public bool CanHeal => !IsDead && CurrentHealth < maxHealth;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;


    private PlayerInventory inventory;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        CurrentHealth = maxHealth;
    }

    private void OnEnable()
    {
        inventory.OnBreadConsumed += HandleBreadConsumed;
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnBreadConsumed -= HandleBreadConsumed;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        SetHealth(CurrentHealth - amount);

        Debug.Log(
            $"[PLAYER] Recibió {amount} de daño. " +
            $"Vida: {CurrentHealth}/{maxHealth}"
        );

        if (IsDead)
        {
            Debug.Log("[PLAYER] Ha sido derrotado.");
            OnDied?.Invoke();
        }
    }
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        SetHealth(CurrentHealth + amount);
    }

    private void SetHealth(float value)
    {
        CurrentHealth = Mathf.Clamp(value, 0f, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void HandleBreadConsumed(ConsumableData bread)
    {
        if (bread != null)
            Heal(bread.HealAmount);
    }
}