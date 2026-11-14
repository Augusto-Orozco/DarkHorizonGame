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

    // Textos de la tabla "Textos". Ya traen su clave por defecto; se pueden cambiar en el Inspector.
    // Los que llevan {0}, {1} necesitan la casilla "Smart" activada en la tabla.
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

    public override void OnEnable()
    {
        base.OnEnable();
        // Si se cambia el idioma con la sala abierta, se vuelve a escribir el texto de la sala.
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

        if (PhotonNetwork.IsConnectedAndReady)
        {
            SetButtonsInteractable(true);
            SetStatus("");
            return;
        }

        SetButtonsInteractable(false);
        SetStatus(connectingText.GetLocalizedString());
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }



    public void CreateRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
            return;

        string code = GenerateCode();
        SetButtonsInteractable(false);
        SetStatus(creatingRoomText.GetLocalizedString(code));

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
            SetStatus(codeLengthText.GetLocalizedString(codeLength));
            return;
        }

        SetButtonsInteractable(false);
        SetStatus(joiningText.GetLocalizedString(code));
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
            SetStatus(roomNotFoundText.GetLocalizedString());
        else if (returnCode == ErrorCode.GameFull)
            SetStatus(roomFullText.GetLocalizedString());
        else if (returnCode == ErrorCode.GameClosed)
            SetStatus(gameStartedText.GetLocalizedString());
        else
            SetStatus(joinFailedText.GetLocalizedString(message));
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
        SetStatus(disconnectedText.GetLocalizedString(cause.ToString()));
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