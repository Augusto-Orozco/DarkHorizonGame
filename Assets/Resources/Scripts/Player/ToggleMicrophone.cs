/*=======================================================================*\
| Daniel Díaz Romero........A01801486                        | 09-11-2026 | 
|                                                                         |
| PhotonMicrophoneToggle.cs | Component to toggle microphone state in     |
| Photon Voice, using the Input system                                    |
|                                                                         |
\*=======================================================================*/
using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Voice.PUN;
using Photon.Voice.Unity;

public class PhotonMicrophoneToggle : MonoBehaviour
{
    [Header("Input System")]
    [SerializeField]
    private InputActionReference toggleMicrophoneAction;

    [Header("Photon Voice")]
    [SerializeField]
    private PhotonVoiceView photonVoiceView;

    private PhotonView photonView;
    private Recorder recorder;

    private bool microphoneEnabled = true;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();

        if (photonVoiceView == null)
            photonVoiceView = GetComponent<PhotonVoiceView>();
    }

    private void OnEnable()
    {
        if (toggleMicrophoneAction != null)
        {
            toggleMicrophoneAction.action.performed +=
                OnToggleMicrophone;
        }
    }

    private void Start()
    {
        // Solo el jugador local puede controlar su microfono.
        if (!photonView.IsMine) //Detecta si el jugador es el propietario del objeto
            return;

        InitializeRecorder();
    }

    private void OnDisable()
    {
        if (toggleMicrophoneAction != null)
        {
            toggleMicrophoneAction.action.performed -=
                OnToggleMicrophone;
        }
    }

    private void InitializeRecorder()
    {
        if (photonVoiceView == null)
        {
            Debug.LogError(
                "[VOICE] PhotonVoiceView no encontrado."
            );
            return;
        }

        recorder = photonVoiceView.RecorderInUse;

        if (recorder == null)
        {
            Debug.LogWarning(
                "[VOICE] Recorder todavía no está disponible."
            );
            return;
        }

        microphoneEnabled = recorder.RecordingEnabled;
    }

    private void OnToggleMicrophone(
        InputAction.CallbackContext context)
    {
        if (photonView == null || !photonView.IsMine)
            return;

        if (recorder == null)
            InitializeRecorder();

        if (recorder == null)
            return;

        microphoneEnabled = !microphoneEnabled;

        recorder.RecordingEnabled = microphoneEnabled;
        recorder.TransmitEnabled = microphoneEnabled;

        Debug.Log(
            microphoneEnabled
            ? "[VOICE] Micrófono activado."
            : "[VOICE] Micrófono desactivado."
        );
    }
}
