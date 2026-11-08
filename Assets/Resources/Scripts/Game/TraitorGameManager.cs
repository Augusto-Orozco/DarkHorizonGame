using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public sealed class TraitorGameManager : MonoBehaviourPun
{
    public static TraitorGameManager Instance { get; private set; }

    [Header("Desbloqueo de la sala")]
    [SerializeField] private int requiredBombotellas = 4;
    [SerializeField] private TraitorRoomGate[] roomGates;

    [Header("Comandante")]
    [SerializeField] private float introDelay = 3f;
    [TextArea][SerializeField] private string introMessage = "Aquí el comandante. Hay 4 Kharvek custodiando las bombotellas. Encuéntrenlos y tráiganlas a la bóveda.";
    [TextArea][SerializeField] private string deliveredMessage = "Bombotella entregada. Faltan {0}.";
    [TextArea][SerializeField] private string unlockedMessage = "¡La bóveda está abierta! Vayan por el artefacto.";
    [TextArea][SerializeField] private string traitorMessage = "Eres el traidor. Elimina al escuadrón antes de que lleguen a la nave.";
    [TextArea][SerializeField] private string crewMessage = "¡Hay un traidor entre ustedes! Lleven el artefacto a la nave.";
    [TextArea][SerializeField] private string artifactDroppedMessage = "¡El portador cayó! Recuperen el artefacto.";
    [TextArea][SerializeField] private string artifactRecoveredMessage = "Artefacto recuperado. ¡Llévenlo a la nave!";

    [Header("Fase final")]
    [SerializeField, Range(0.1f, 1f)] private float carrierSpeedMultiplier = 0.8f;
    [SerializeField, Min(0.1f)] private float endCheckInterval = 0.5f;
    [SerializeField] private GameObject artifactPrefab;

    private readonly List<PlayerMovement> players = new List<PlayerMovement>();
    private int collectedBombotellas;
    private bool artifactCollected;
    private bool roomUnlocked;

    public event Action<int, int> OnBombotellasChanged;
    public event Action<PlayerHealth> OnTraitorAssigned;

    public bool RoomUnlocked => roomUnlocked;
    public bool ArtifactCollected => artifactCollected;
    public int CollectedBombotellas => collectedBombotellas;
    public int RequiredBombotellas => requiredBombotellas;
    public bool HasRequiredBombotellas => collectedBombotellas >= requiredBombotellas;
    public int TraitorActorNumber { get; private set; } = -1;
    public int ArtifactHolderActorNumber { get; private set; } = -1;
    public bool ArtifactOnGround => artifactCollected && ArtifactHolderActorNumber == -1;
    public bool CanLocalTakeArtifact =>
        roomUnlocked &&
        !IsGameOver &&
        (!artifactCollected || (IsNetworked && ArtifactOnGround && !IsLocalTraitor));

    private bool IsNetworked => PhotonNetwork.InRoom && photonView.ViewID != 0;

    public event Action<GameResult> OnGameOver;
    public bool IsGameOver { get; private set; }
    public bool IsLocalTraitor => currentTraitor != null && IsLocalPlayer(currentTraitor);

    private PlayerHealth currentTraitor;
    private float nextEndCheck;
    private Vector3 lastCarrierPosition;

    public enum GameResult
    {
        CrewExtracted,
        TraitorDefeated,
        TraitorWins
    }

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
        StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        yield return new WaitForSeconds(introDelay);
        Commander.Say(introMessage, 6f);
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

        if (collectedBombotellas < requiredBombotellas)
            Commander.Say(string.Format(deliveredMessage, requiredBombotellas - collectedBombotellas), 4f);

        CheckRoomUnlock();
    }

    public bool RequestCollectArtifact()
    {
        if (!CanLocalTakeArtifact)
            return false;

        if (!IsNetworked)
        {
            AssignTraitorOffline();
            return true;
        }

        photonView.RPC(nameof(RpcRequestArtifact), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        return true;
    }

    [PunRPC]
    private void RpcRequestArtifact(int collectorActor)
    {
        if (!PhotonNetwork.IsMasterClient || IsGameOver)
            return;

        if (artifactCollected)
        {
            if (ArtifactOnGround && collectorActor != TraitorActorNumber)
                photonView.RPC(nameof(RpcSetCarrier), RpcTarget.All, collectorActor);

            return;
        }

        List<int> candidates = new List<int>();
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.ActorNumber != collectorActor)
                candidates.Add(player.ActorNumber);
        }

        if (candidates.Count == 0)
            candidates.Add(collectorActor);

        int traitorActor = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        photonView.RPC(nameof(RpcArtifactCollected), RpcTarget.All, collectorActor, traitorActor);
    }

    [PunRPC]
    private void RpcArtifactCollected(int collectorActor, int traitorActor)
    {
        artifactCollected = true;
        TraitorActorNumber = traitorActor;

        int crewCount = Mathf.Max(1, PhotonNetwork.PlayerList.Length - 1);

        PlayerHealth traitor = FindPlayer(traitorActor);
        if (traitor != null)
            ActivateTraitor(traitor, crewCount);

        ApplyCarrier(collectorActor);

        bool iAmTraitor = PhotonNetwork.LocalPlayer.ActorNumber == traitorActor;
        Commander.Say(iAmTraitor ? traitorMessage : crewMessage, 6f);

        Debug.Log($"[TRAITOR] Artefacto: jugador {collectorActor}. Traidor: jugador {traitorActor}. Escuadrón: {crewCount}.", this);
    }

    [PunRPC]
    private void RpcSetCarrier(int carrierActor)
    {
        ApplyCarrier(carrierActor);
        Commander.Say(artifactRecoveredMessage, 4f);
    }

    private void ApplyCarrier(int carrierActor)
    {
        ArtifactHolderActorNumber = carrierActor;

        PlayerHealth carrier = FindPlayer(carrierActor);
        if (carrier == null)
            return;

        lastCarrierPosition = carrier.transform.position;
        SetSpeedMultiplier(carrier, carrierSpeedMultiplier);
    }

    private void CheckCarrier()
    {
        if (ArtifactHolderActorNumber == -1)
            return;

        PlayerHealth carrier = FindPlayer(ArtifactHolderActorNumber);
        if (carrier != null)
            lastCarrierPosition = carrier.transform.position;

        if (carrier != null && !carrier.IsDead)
            return;

        if (artifactPrefab != null)
            NetworkItemSpawner.Spawn(artifactPrefab, lastCarrierPosition + Vector3.up * 0.5f, Quaternion.identity, NetworkPickup.BuildData(0f));
        else
            Debug.LogWarning("TraitorGameManager sin Artifact Prefab: el artefacto no puede caer al suelo.", this);

        photonView.RPC(nameof(RpcArtifactDropped), RpcTarget.All);
    }

    [PunRPC]
    private void RpcArtifactDropped()
    {
        PlayerHealth previous = FindPlayer(ArtifactHolderActorNumber);
        if (previous != null)
            SetSpeedMultiplier(previous, 1f);

        ArtifactHolderActorNumber = -1;
        Commander.Say(artifactDroppedMessage, 5f);
    }

    private static void SetSpeedMultiplier(PlayerHealth player, float multiplier)
    {
        if (player.TryGetComponent(out PlayerMovement movement))
            movement.ExternalSpeedMultiplier = multiplier;
    }

    private void AssignTraitorOffline()
    {
        artifactCollected = true;

        PlayerHealth[] all = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        if (all.Length == 0)
        {
            Debug.LogWarning("No hay jugadores para elegir al traidor.", this);
            return;
        }

        PlayerHealth traitor = all[UnityEngine.Random.Range(0, all.Length)];
        ActivateTraitor(traitor, Mathf.Max(1, all.Length - 1));
        Commander.Say(crewMessage, 6f);
    }

    private void ActivateTraitor(PlayerHealth player, int crewCount)
    {
        currentTraitor = player;
        TraitorPlayer traitor = player.GetComponent<TraitorPlayer>();
        if (traitor == null)
            traitor = player.gameObject.AddComponent<TraitorPlayer>();

        traitor.Activate(crewCount);
        OnTraitorAssigned?.Invoke(player);
    }

    public static PlayerHealth FindPlayer(int actorNumber)
    {
        foreach (PlayerHealth player in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            PhotonView view = player.GetComponent<PhotonView>();
            if (view != null && view.OwnerActorNr == actorNumber)
                return player;
        }

        return null;
    }

    private void Update()
    {
        if (IsGameOver || !artifactCollected)
            return;

        if (IsNetworked && !PhotonNetwork.IsMasterClient)
            return;

        if (Time.time < nextEndCheck)
            return;

        nextEndCheck = Time.time + endCheckInterval;
        CheckEndConditions();

        if (!IsGameOver && IsNetworked)
            CheckCarrier();
    }

    private void CheckEndConditions()
    {
        if (currentTraitor == null || currentTraitor.IsDead)
        {
            EndGame(GameResult.TraitorDefeated);
            return;
        }

        foreach (PlayerHealth player in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            if (player != currentTraitor && !player.IsDead)
                return;
        }

        EndGame(GameResult.TraitorWins);
    }

    public void ReportArtifactExtracted()
    {
        if (IsGameOver || !artifactCollected)
            return;

        if (!IsNetworked)
        {
            EndGame(GameResult.CrewExtracted);
            return;
        }

        photonView.RPC(nameof(RpcReportExtraction), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    private void RpcReportExtraction(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient || IsGameOver || actorNumber != ArtifactHolderActorNumber)
            return;

        EndGame(GameResult.CrewExtracted);
    }

    private void EndGame(GameResult result)
    {
        if (IsGameOver)
            return;

        if (IsNetworked)
            photonView.RPC(nameof(RpcGameOver), RpcTarget.All, (int)result);
        else
            ApplyGameOver((int)result);
    }

    [PunRPC]
    private void RpcGameOver(int result)
    {
        ApplyGameOver(result);
    }

    private void ApplyGameOver(int result)
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        Debug.Log($"[FIN] {(GameResult)result}", this);
        OnGameOver?.Invoke((GameResult)result);
    }

    public bool IsArtifactCarrier(PlayerHealth player)
    {
        if (!IsNetworked)
            return true;

        PhotonView view = player.GetComponent<PhotonView>();
        return view != null && view.OwnerActorNr == ArtifactHolderActorNumber;
    }

    private static bool IsLocalPlayer(PlayerHealth player)
    {
        if (!PhotonNetwork.InRoom)
            return true;

        PhotonView view = player.GetComponent<PhotonView>();
        return view != null && view.IsMine;
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
            Commander.Say(unlockedMessage, 6f);
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