using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public sealed class TraitorGameManager : MonoBehaviourPun
{
    public static TraitorGameManager Instance { get; private set; }

    [Header("Desbloqueo de la sala")]
    [SerializeField] private int requiredBombotellas = 4;
    [SerializeField] private TraitorRoomGate[] roomGates;

    private readonly List<PlayerMovement> players = new List<PlayerMovement>();
    private int collectedBombotellas;
    private bool artifactCollected;
    private bool roomUnlocked;

    public event Action<int, int> OnBombotellasChanged;

    public bool RoomUnlocked => roomUnlocked;
    public int CollectedBombotellas => collectedBombotellas;
    public int RequiredBombotellas => requiredBombotellas;
    public bool HasRequiredBombotellas => collectedBombotellas >= requiredBombotellas;

    private bool IsNetworked => PhotonNetwork.InRoom && photonView.ViewID != 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (roomGates == null || roomGates.Length == 0)
            roomGates = FindObjectsByType<TraitorRoomGate>(FindObjectsSortMode.None);

        foreach (PlayerMovement player in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
            RegisterPlayer(player);

        CheckRoomUnlock();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RegisterPlayer(PlayerMovement player)
    {
        if (player != null && !players.Contains(player))
            players.Add(player);
    }

    public void UnregisterPlayer(PlayerMovement player)
    {
        players.Remove(player);
    }

    public void RegisterBombotella()
    {
        if (roomUnlocked)
            return;

        if (!IsNetworked)
        {
            SetCollected(collectedBombotellas + 1);
            return;
        }

        if (PhotonNetwork.IsMasterClient)
            AddBombotellaAsMaster();
        else
            photonView.RPC(nameof(RpcRegisterBombotella), RpcTarget.MasterClient);
    }

    [PunRPC]
    private void RpcRegisterBombotella()
    {
        if (PhotonNetwork.IsMasterClient)
            AddBombotellaAsMaster();
    }

    private void AddBombotellaAsMaster()
    {
        if (roomUnlocked)
            return;

        int newCount = Mathf.Min(collectedBombotellas + 1, requiredBombotellas);
        photonView.RPC(nameof(RpcSetCollected), RpcTarget.All, newCount);
    }

    [PunRPC]
    private void RpcSetCollected(int count)
    {
        SetCollected(count);
    }

    private void SetCollected(int count)
    {
        collectedBombotellas = count;
        Debug.Log($"[BOMBOTELLAS] {collectedBombotellas}/{requiredBombotellas}", this);

        OnBombotellasChanged?.Invoke(collectedBombotellas, requiredBombotellas);
        RefreshGatePrompts();
        CheckRoomUnlock();
    }

    public bool TryAssignTraitor()
    {
        if (artifactCollected)
            return false;

        artifactCollected = true;
        players.RemoveAll(player => player == null || !player.isActiveAndEnabled);

        if (players.Count == 0)
        {
            Debug.LogWarning("No hay jugadores registrados para elegir al traidor.", this);
            return false;
        }

        PlayerMovement selectedPlayer = players[UnityEngine.Random.Range(0, players.Count)];
        TraitorPlayer traitor = selectedPlayer.GetComponent<TraitorPlayer>();
        if (traitor == null)
            traitor = selectedPlayer.gameObject.AddComponent<TraitorPlayer>();

        traitor.Activate();
        Debug.Log($"[TRAITOR] {selectedPlayer.name} fue elegido como el traidor.", selectedPlayer);
        return true;
    }

    private void CheckRoomUnlock()
    {
        if (roomUnlocked)
            return;

        if (HasRequiredBombotellas)
        {
            roomUnlocked = true;
            foreach (TraitorRoomGate gate in roomGates)
            {
                if (gate != null)
                    gate.SetOpen(true);
            }

            Debug.Log("[TRAITOR] La entrada a la sala del elemento fue desbloqueada.", this);
        }
    }

    private void RefreshGatePrompts()
    {
        foreach (TraitorRoomGate gate in roomGates)
        {
            if (gate != null)
                gate.RefreshRequirement();
        }
    }
}