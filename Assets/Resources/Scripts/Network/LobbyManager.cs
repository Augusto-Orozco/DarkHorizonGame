using System.Text;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class LobbyManager : MonoBehaviourPunCallbacks
{

    private const string CodeChars = "ABCDEFGHJKMNPQRSTUVWXYZ";

    [Header("Red")]
    [SerializeField] private string gameVersion = "0.1";
    [SerializeField] private byte maxPlayers = 4;
    [SerializeField] private int codeLength = 4;
    [Tooltip("Nombre de la escena del juego (tiene que estar en Build Settings)")]
    [SerializeField] private string gameSceneName = "MapaDemoS4";

    [Header("Panel de crear / unirse")]
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private Button createButton;
    [SerializeField] private Button joinButton;

    [Header("Panel de sala de espera")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private TMP_Text playersText;
    [SerializeField] private Button startButton;

    [Header("Mensajes")]
    [SerializeField] private TMP_Text statusText;

    private void Start()
    {

        PhotonNetwork.AutomaticallySyncScene = true;

        ShowJoinPanel();

        if (PhotonNetwork.IsConnectedAndReady)
        {
            SetButtonsInteractable(true);
            SetStatus("");
            return;
        }

        SetButtonsInteractable(false);
        SetStatus("Conectando...");
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }



    public void CreateRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
            return;

        string code = GenerateCode();
        SetButtonsInteractable(false);
        SetStatus($"Creando sala {code}...");

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsVisible = false,   
            IsOpen = true
        };
        PhotonNetwork.CreateRoom(code, options);
    }

    public void JoinRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
            return;

        string code = codeInput != null ? codeInput.text.Trim().ToUpperInvariant() : "";
        if (code.Length != codeLength)
        {
            SetStatus($"El código tiene {codeLength} letras.");
            return;
        }

        SetButtonsInteractable(false);
        SetStatus($"Entrando a {code}...");
        PhotonNetwork.JoinRoom(code);
    }

    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.LoadLevel(gameSceneName);
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
    }


    public override void OnConnectedToMaster()
    {
        SetButtonsInteractable(true);
        SetStatus("");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[LOBBY] No se pudo crear la sala ({message}). Reintentando...");
        CreateRoom();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        SetButtonsInteractable(true);

        if (returnCode == ErrorCode.GameDoesNotExist)
            SetStatus("No existe una sala con ese código.");
        else if (returnCode == ErrorCode.GameFull)
            SetStatus("La sala está llena.");
        else if (returnCode == ErrorCode.GameClosed)
            SetStatus("La partida ya empezó.");
        else
            SetStatus($"No se pudo entrar: {message}");
    }

    public override void OnJoinedRoom()
    {
        SetStatus("");
        ShowRoomPanel();
        RefreshRoom();
    }

    public override void OnLeftRoom()
    {
        ShowJoinPanel();
        SetStatus("");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) => RefreshRoom();
    public override void OnPlayerLeftRoom(Player otherPlayer) => RefreshRoom();
    public override void OnMasterClientSwitched(Player newMasterClient) => RefreshRoom();

    public override void OnDisconnected(DisconnectCause cause)
    {
        ShowJoinPanel();
        SetButtonsInteractable(false);
        SetStatus($"Sin conexión ({cause}).");
    }



    private void RefreshRoom()
    {
        if (!PhotonNetwork.InRoom)
            return;

        Room room = PhotonNetwork.CurrentRoom;

        if (roomCodeText != null)
            roomCodeText.text = $"Código: {room.Name}";

        if (playersText != null)
        {
            StringBuilder list = new StringBuilder();
            list.AppendLine($"Jugadores ({room.PlayerCount}/{room.MaxPlayers})");

            foreach (Player player in room.Players.Values)
            {
                string host = player.IsMasterClient ? " (host)" : "";
                string me = player.IsLocal ? " ← tú" : "";
                list.AppendLine($"Jugador {player.ActorNumber}{host}{me}");
            }

            playersText.text = list.ToString();
        }


        if (startButton != null)
            startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }

    private void ShowJoinPanel()
    {
        if (joinPanel != null) joinPanel.SetActive(true);
        if (roomPanel != null) roomPanel.SetActive(false);
    }

    private void ShowRoomPanel()
    {
        if (joinPanel != null) joinPanel.SetActive(false);
        if (roomPanel != null) roomPanel.SetActive(true);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (createButton != null) createButton.interactable = value;
        if (joinButton != null) joinButton.interactable = value;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        if (!string.IsNullOrEmpty(message))
            Debug.Log($"[LOBBY] {message}");
    }

    private string GenerateCode()
    {
        StringBuilder code = new StringBuilder(codeLength);
        for (int i = 0; i < codeLength; i++)
            code.Append(CodeChars[Random.Range(0, CodeChars.Length)]);
        return code.ToString();
    }
}