
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LanzaTazosAudio : MonoBehaviour
{
    [Header("Sonidos")]
    [SerializeField] private AudioClip shotSound;
    [SerializeField] private AudioClip emptySound;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
    }

    public void PlayShot()
    {
        if (shotSound != null)
            audioSource.PlayOneShot(shotSound);
    }

    public void PlayEmpty()
    {
        if (emptySound != null)
            audioSource.PlayOneShot(emptySound);
    }
}
