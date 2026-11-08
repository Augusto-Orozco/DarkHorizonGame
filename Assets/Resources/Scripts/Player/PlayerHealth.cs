using System;
using System.Collections;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
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
    public event Action<Color> OnHitFlash;

    private PlayerInventory inventory;
    private PlayerMovement movement;
    private CharacterController controller;
    private PhotonView view;
    private Vector3 defaultSpawnPosition;
    private Quaternion defaultSpawnRotation;
    private bool deathHandled;
    private bool originalReadInput;

    private bool IsNetworked => PhotonNetwork.InRoom && view != null && view.ViewID != 0;
    private bool IsOwner => !IsNetworked || view.IsMine;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        movement = GetComponent<PlayerMovement>();
        controller = GetComponent<CharacterController>();
        view = GetComponent<PhotonView>();
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

        if (!IsOwner)
        {
            view.RPC(nameof(RpcRequestDamage), view.Owner, amount);
            return;
        }

        ApplyDamage(amount);
    }

    [PunRPC]
    private void RpcRequestDamage(float amount)
    {
        if (IsOwner)
            ApplyDamage(amount);
    }

    private void ApplyDamage(float amount)
    {
        if (IsDead)
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
        if (IsDead || amount <= 0f || !IsOwner)
            return;

        SetHealth(CurrentHealth + amount);
    }

    public void SetMaxHealth(float newMaxHealth, bool healToFull)
    {
        if (!IsOwner || newMaxHealth <= 0f)
            return;

        maxHealth = newMaxHealth;
        SetHealth(healToFull ? maxHealth : CurrentHealth);
    }

    private void SetHealth(float value)
    {
        CurrentHealth = Mathf.Clamp(value, 0f, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (IsNetworked && view.IsMine)
            view.RPC(nameof(RpcSyncHealth), RpcTarget.Others, CurrentHealth, maxHealth);
    }

    [PunRPC]
    private void RpcSyncHealth(float current, float max)
    {
        if (IsOwner)
            return;

        maxHealth = max;
        CurrentHealth = Mathf.Clamp(current, 0f, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void FlashHit(Color color)
    {
        if (IsOwner)
        {
            OnHitFlash?.Invoke(color);
            return;
        }

        view.RPC(nameof(RpcFlashHit), view.Owner, color.r, color.g, color.b);
    }

    [PunRPC]
    private void RpcFlashHit(float r, float g, float b)
    {
        if (IsOwner)
            OnHitFlash?.Invoke(new Color(r, g, b, 1f));
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

        bool finalPhase = TraitorGameManager.Instance != null && TraitorGameManager.Instance.ArtifactCollected;
        if (!finalPhase)
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

        deathHandled = false;
        movement.readInput = originalReadInput;
        SetHealth(maxHealth);
    }
}