using System.Text;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    private const string CodeChars = "ABCDEFGHJKMNPQRSTUVWXYZ";
    private const string TextTable = "Textos";

    [Header("Red")]
    [SerializeField] private string gameVersion = "0.2";
    [SerializeField] private byte maxPlayers = 4;
    [SerializeField] private int codeLength = 4;
    [SerializeField] private string gameSceneName = "MapaDemoS4";
    [SerializeField] private int maxCreateRetries = 3;

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

    [Header("Textos (tabla Textos)")]
    [SerializeField] private LocalizedString connectingText = new LocalizedString(TextTable, "G060");
    [SerializeField] private LocalizedString creatingRoomText = new LocalizedString(TextTable, "G061");
    [SerializeField] private LocalizedString codeLengthText = new LocalizedString(TextTable, "G062");
    [SerializeField] private LocalizedString joiningText = new LocalizedString(TextTable, "G063");
    [SerializeField] private LocalizedString roomNotFoundText = new LocalizedString(TextTable, "G064");
    [SerializeField] private LocalizedString roomFullText = new LocalizedString(TextTable, "G065");
    [SerializeField] private LocalizedString gameStartedText = new LocalizedString(TextTable, "G066");
    [SerializeField] private LocalizedString joinFailedText = new LocalizedString(TextTable, "G067");
    [SerializeField] private LocalizedString disconnectedText = new LocalizedString(TextTable, "G068");
    [SerializeField] private LocalizedString roomCodeLabel = new LocalizedString(TextTable, "G069");
    [SerializeField] private LocalizedString playersLabel = new LocalizedString(TextTable, "G070");
    [SerializeField] private LocalizedString playerLabel = new LocalizedString(TextTable, "G071");
    [SerializeField] private LocalizedString hostLabel = new LocalizedString(TextTable, "G072");
    [SerializeField] private LocalizedString youLabel = new LocalizedString(TextTable, "G073");

    private bool busy;
    private bool starting;
    private bool leaving;
    private int createRetries;

    private bool CanAct => !busy && !leaving && PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom;

    public override void OnEnable()
    {
        base.OnEnable();
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale locale) => RefreshRoom();

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        ShowJoinPanel();

        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom)
        {
            SetButtonsInteractable(true);
            SetStatus("");
            return;
        }

        SetButtonsInteractable(false);
        SetStatus(connectingText.GetLocalizedString());

        if (PhotonNetwork.IsConnected)
            return;

        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    public void CreateRoom()
    {
        if (!CanAct)
            return;

        createRetries = 0;
        TryCreateRoom();
    }

    private void TryCreateRoom()
    {
        busy = true;
        SetButtonsInteractable(false);

        string code = GenerateCode();
        SetStatus(creatingRoomText.GetLocalizedString(code));

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsVisible = false,
            IsOpen = true
        };

        if (!PhotonNetwork.CreateRoom(code, options))
            FailOperation(joinFailedText.GetLocalizedString("CreateRoom"));
    }

    public void JoinRoom()
    {
        if (!CanAct)
            return;

        string code = codeInput != null ? codeInput.text.Trim().ToUpperInvariant() : "";
        if (code.Length != codeLength)
        {
            SetStatus(codeLengthText.GetLocalizedString(codeLength));
            return;
        }

        busy = true;
        SetButtonsInteractable(false);
        SetStatus(joiningText.GetLocalizedString(code));

        if (!PhotonNetwork.JoinRoom(code))
            FailOperation(joinFailedText.GetLocalizedString("JoinRoom"));
    }

    public void StartGame()
    {
        if (starting || leaving || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        starting = true;
        if (startButton != null)
            startButton.interactable = false;

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.LoadLevel(gameSceneName);
    }

    public void LeaveRoom()
    {
        if (leaving || starting || !PhotonNetwork.InRoom)
            return;

        leaving = true;
        PhotonNetwork.LeaveRoom();
    }

    public override void OnConnectedToMaster()
    {
        busy = false;
        leaving = false;
        starting = false;
        SetButtonsInteractable(true);
        SetStatus("");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        createRetries++;

        if (createRetries <= maxCreateRetries && PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning($"[LOBBY] No se pudo crear la sala ({message}). Reintento {createRetries}/{maxCreateRetries}");
            TryCreateRoom();
            return;
        }

        FailOperation(joinFailedText.GetLocalizedString(message));
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (returnCode == ErrorCode.GameDoesNotExist)
            FailOperation(roomNotFoundText.GetLocalizedString());
        else if (returnCode == ErrorCode.GameFull)
            FailOperation(roomFullText.GetLocalizedString());
        else if (returnCode == ErrorCode.GameClosed)
            FailOperation(gameStartedText.GetLocalizedString());
        else
            FailOperation(joinFailedText.GetLocalizedString(message));
    }

    public override void OnJoinedRoom()
    {
        busy = false;
        starting = false;
        createRetries = 0;

        if (startButton != null)
            startButton.interactable = true;

        SetStatus("");
        ShowRoomPanel();
        RefreshRoom();
    }

    public override void OnLeftRoom()
    {
        busy = false;
        starting = false;
        ShowJoinPanel();
        SetButtonsInteractable(false);
        SetStatus(connectingText.GetLocalizedString());
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) => RefreshRoom();
    public override void OnPlayerLeftRoom(Player otherPlayer) => RefreshRoom();
    public override void OnMasterClientSwitched(Player newMasterClient) => RefreshRoom();

    public override void OnDisconnected(DisconnectCause cause)
    {
        busy = false;
        starting = false;
        leaving = false;
        ShowJoinPanel();
        SetButtonsInteractable(false);
        SetStatus(disconnectedText.GetLocalizedString(cause.ToString()));
    }

    private void FailOperation(string message)
    {
        busy = false;
        SetButtonsInteractable(PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom);
        SetStatus(message);
    }

    private void RefreshRoom()
    {
        if (!PhotonNetwork.InRoom)
            return;

        Room room = PhotonNetwork.CurrentRoom;

        if (roomCodeText != null)
            roomCodeText.text = roomCodeLabel.GetLocalizedString(room.Name);

        if (playersText != null)
        {
            StringBuilder list = new StringBuilder();
            list.AppendLine(playersLabel.GetLocalizedString(room.PlayerCount, (int)room.MaxPlayers));

            foreach (Player player in room.Players.Values)
            {
                string host = player.IsMasterClient ? " " + hostLabel.GetLocalizedString() : "";
                string me = player.IsLocal ? " " + youLabel.GetLocalizedString() : "";
                list.AppendLine(playerLabel.GetLocalizedString(player.ActorNumber) + host + me);
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