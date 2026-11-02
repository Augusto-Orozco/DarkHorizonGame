/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 31-10-2026 | 
|                                                                         |
| PlayerHealth.cs | Script that administrates the health of the player,   |
| damage an heal recieved as well a isDead state. Updates the player HUD. |
|                                                                         |
\*=======================================================================*/


using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerHealth : MonoBehaviour, IDamageable
{ //                                     // Interface that registers taken damage
    [SerializeField] private float maxHealth = 100f;
    [Header("Respawn")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField, Min(0f)] private float respawnDelay = 2f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0f;
    public bool CanHeal => !IsDead && CurrentHealth < maxHealth;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;


    private PlayerInventory inventory;
    private PlayerMovement movement;
    private CharacterController controller;
    private Vector3 defaultSpawnPosition;
    private Quaternion defaultSpawnRotation;
    private bool deathHandled;
    private bool originalReadInput;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        movement = GetComponent<PlayerMovement>();
        controller = GetComponent<CharacterController>();
        defaultSpawnPosition = transform.position;
        defaultSpawnRotation = transform.rotation;
        originalReadInput = movement.readInput;
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
            DeathPlayer();
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

    private void DeathPlayer()
    {
        if (deathHandled)
            return;

        deathHandled = true;
        movement.readInput = false;
        OnDied?.Invoke();
        StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnDelay);

        Vector3 position = spawnPoint != null
            ? spawnPoint.position
            : defaultSpawnPosition;
        Quaternion rotation = spawnPoint != null
            ? spawnPoint.rotation
            : defaultSpawnRotation;

        controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        controller.enabled = true;

        CurrentHealth = maxHealth;
        deathHandled = false;
        movement.readInput = originalReadInput;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}