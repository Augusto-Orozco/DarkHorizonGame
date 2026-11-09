
using UnityEngine;
using Photon.Voice.PUN;

public class VoiceSpeakingIndicator : MonoBehaviour
{
    [Header("Photon Voice")]
    [SerializeField]
    private PhotonVoiceView photonVoiceView;

    [Header("Visual")]
    [SerializeField]
    private SpriteRenderer speakerIcon;

    [SerializeField]
    private Vector3 offset = new Vector3(0f, 2.2f, 0f);

    private bool lastSpeakingState;

    private void Awake()
    {
        if (photonVoiceView == null)
            photonVoiceView = GetComponentInParent<PhotonVoiceView>();

        if (speakerIcon == null)
            speakerIcon = GetComponent<SpriteRenderer>();

        if (speakerIcon != null)
            speakerIcon.enabled = false;
    }

    private void LateUpdate()
    {
        if (photonVoiceView == null || speakerIcon == null)
            return;

        bool isSpeaking = photonVoiceView.IsSpeaking;

        if (isSpeaking != lastSpeakingState)
        {
            speakerIcon.enabled = isSpeaking;
            lastSpeakingState = isSpeaking;
        }

        transform.localPosition = offset;
    }
}
