
using UnityEngine;
using Photon.Pun;
using Photon.Voice.PUN;

public class VoiceSpeakingIndicator : MonoBehaviour
{
    [Header("Photon")]
    [SerializeField]
    private PhotonVoiceView photonVoiceView;

    [SerializeField]
    private PhotonView photonView;

    [Header("Visual")]
    [SerializeField]
    private SpriteRenderer speakerIcon;

    private void Awake()
    {
        if (photonVoiceView == null)
            photonVoiceView = GetComponentInParent<PhotonVoiceView>();

        if (photonView == null)
            photonView = GetComponentInParent<PhotonView>();

        if (speakerIcon == null)
            speakerIcon = GetComponent<SpriteRenderer>();

        if (speakerIcon != null)
            speakerIcon.enabled = false;
    }

    private void Update()
    {
        if (photonVoiceView == null ||
            photonView == null ||
            speakerIcon == null)
            return;

        bool isSpeaking;

        if (photonView.IsMine)
        {
            // Jugador local: mostrar cuando transmite.
            isSpeaking = photonVoiceView.IsRecording;
        }
        else
        {
            // Jugadores remotos: mostrar cuando reproducen voz.
            isSpeaking = photonVoiceView.IsSpeaking;
        }

        speakerIcon.enabled = isSpeaking;
    }
}
