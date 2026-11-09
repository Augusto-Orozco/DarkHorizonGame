using System;
using Photon.Pun;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;

    private EnemyData data;
    private PhotonView view;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0f;
    public float DamageTakenMultiplier { get; set; } = 1f;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;

    private bool IsNetworked => PhotonNetwork.InRoom && view != null && view.ViewID != 0;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        view = GetComponent<PhotonView>();
        if (GetComponent<EnemyHitFlash>() == null)
            gameObject.AddComponent<EnemyHitFlash>();

        if (view == null && GetComponentInParent<PhotonView>() != null)
            Debug.LogError($"[{name}] EnemyHealth necesita una PhotonView en ESTE mismo objeto para la red.", this);
    }

    public void SetData(EnemyData newData)
    {
        data = newData;

        if (data != null)
            maxHealth = data.MaxHealth;

        SetHealth(maxHealth);
    }

    public void ScaleMaxHealth(float multiplier)
    {
        maxHealth *= multiplier;
        SetHealth(maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        if (IsNetworked && !PhotonNetwork.IsMasterClient)
        {
            view.RPC(nameof(RpcRequestDamage), RpcTarget.MasterClient, amount);
            return;
        }

        ApplyDamage(amount);
    }

    [PunRPC]
    private void RpcRequestDamage(float amount)
    {
        if (PhotonNetwork.IsMasterClient)
            ApplyDamage(amount);
    }

    private void ApplyDamage(float amount)
    {
        if (IsDead)
            return;

        amount *= DamageTakenMultiplier;  
        SetHealth(CurrentHealth - amount);

        Debug.Log($"[{name}] Recibió {amount} de daño. Vida: {CurrentHealth}/{maxHealth}");

        if (IsNetworked)
            view.RPC(nameof(RpcSyncHealth), RpcTarget.Others, CurrentHealth, maxHealth);

        if (IsDead)
        {
            Debug.Log($"[{name}] Ha sido derrotado.");
            OnDied?.Invoke();
        }
    }

    [PunRPC]
    private void RpcSyncHealth(float current, float max)
    {
        bool wasDead = IsDead;
        maxHealth = max;
        SetHealth(current);

        if (!wasDead && IsDead)
            OnDied?.Invoke();  
    }

    private void SetHealth(float value)
    {
        CurrentHealth = Mathf.Clamp(value, 0f, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}