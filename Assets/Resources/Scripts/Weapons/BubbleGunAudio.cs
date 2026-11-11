
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BubbleGunAudio : MonoBehaviour
{
    [Header("Sonidos")]
    [SerializeField] private AudioClip shotSound;
    [SerializeField] private AudioClip coolingSound;

    private AudioSource shotSource;
    private AudioSource coolingSource;

    private bool wasCooling = false;

    private void Awake()
    {
        // Audio de disparo
        shotSource = GetComponent<AudioSource>();
        shotSource.playOnAwake = false;
        shotSource.loop = false;
        shotSource.spatialBlend = 1f;

        // Audio de enfriamiento
        coolingSource = gameObject.AddComponent<AudioSource>();
        coolingSource.playOnAwake = false;
        coolingSource.loop = true;
        coolingSource.spatialBlend = 1f;
    }

    public void PlayShot()
    {
        if (shotSound != null)
            shotSource.PlayOneShot(shotSound, 1f);
    }

    public void SetCooling(bool cooling)
    {
        if (cooling == wasCooling)
            return;

        wasCooling = cooling;

        if (cooling)
        {
            if (coolingSound != null)
            {
                coolingSource.clip = coolingSound;
                coolingSource.loop = true;
                coolingSource.Play();
            }
        }
        else
        {
            coolingSource.Stop();
        }
    }

    private void OnDisable()
    {
        coolingSource.Stop();
        wasCooling = false;
    }
}
