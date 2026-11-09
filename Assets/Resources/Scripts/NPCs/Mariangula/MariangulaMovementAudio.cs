
using UnityEngine;

public partial class MariangulaMovement
{
    [Header("Sonidos Mariangula")]
    [SerializeField] private AudioClip idleClip;
    [SerializeField] private AudioClip footstepsClip;
    [SerializeField] private AudioClip screamClip;
    [SerializeField] private AudioClip attackClip;

    [Header("Configuración de sonidos")]
    [SerializeField] private float screamInterval = 3f;

    [SerializeField, Range(0f, 1f)]
    private float idleVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float footstepsVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float screamVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float attackVolume = 1f;

    [Header("Configuración del audio de ataque")]
    [SerializeField] private float attackAudioGraceTime = 0.15f;

    private AudioSource idleSource;
    private AudioSource footstepsSource;
    private AudioSource screamSource;
    private AudioSource attackSource;

    private bool wasChasing;
    private float nextScreamTime;

    private bool attackAudioActive;
    private float attackAudioStopTime;

    private void Start()
    {
        idleSource = CreateAudioSource(
            idleClip, true, idleVolume
        );

        footstepsSource = CreateAudioSource(
            footstepsClip, true, footstepsVolume
        );

        screamSource = CreateAudioSource(
            screamClip, false, screamVolume
        );

        // El audio de ataque tiene loop.
        attackSource = CreateAudioSource(
            attackClip, true, attackVolume
        );
    }


    private void LateUpdate()
    {
        if (idleSource == null)
            return;

        UpdateAudioState();
    }


    private void UpdateAudioState()
    {
        bool isAttacking = state == MariangulaState.Attacking;
        bool isChasing = state == MariangulaState.Chasing;

      
        bool isMoving =
            animationState == AnimationState.PreMove ||
            animationState == AnimationState.Move ||
            animationState == AnimationState.PostMove;

        if (isAttacking)
        {
            StopLoop(idleSource);
            StopLoop(footstepsSource);

           
            if (!attackAudioActive)
            {
                attackAudioActive = true;
                PlayLoop(attackSource);
            }

            
            attackAudioStopTime =
                Time.time + attackAudioGraceTime;

    
            if (!wasChasing)
            {
                PlayScream();
                nextScreamTime = Time.time + screamInterval;
                wasChasing = true;
            }
            else if (Time.time >= nextScreamTime)
            {
                PlayScream();
                nextScreamTime = Time.time + screamInterval;
            }

            return;
        }

        if (attackAudioActive &&
            Time.time >= attackAudioStopTime)
        {
            StopLoop(attackSource);
            attackAudioActive = false;
        }
        if (isChasing)
        {
            if (!wasChasing)
            {
                PlayScream();
                nextScreamTime = Time.time + screamInterval;
            }
            else if (Time.time >= nextScreamTime)
            {
                PlayScream();
                nextScreamTime = Time.time + screamInterval;
            }

            wasChasing = true;
        }
        else
        {
            wasChasing = false;
        }

        if (isChasing && isMoving)
        {
            StopLoop(idleSource);
            PlayLoop(footstepsSource);
        }
        else
        {
            StopLoop(footstepsSource);

            if (!isChasing)
            {
                PlayLoop(idleSource);
            }
            else
            {
                StopLoop(idleSource);
            }
        }
    }
    private AudioSource CreateAudioSource(
        AudioClip clip,
        bool loop,
        float volume)
    {
        AudioSource source =
            gameObject.AddComponent<AudioSource>();

        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.volume = volume;

        return source;
    }
    private void PlayLoop(AudioSource source)
    {
        if (source != null &&
            source.clip != null &&
            !source.isPlaying)
        {
            source.Play();
        }
    }

    private void StopLoop(AudioSource source)
    {
        if (source != null && source.isPlaying)
        {
            source.Stop();
        }
    }
    private void PlayScream()
    {
        if (screamSource != null &&
            screamClip != null)
        {
            screamSource.PlayOneShot(
                screamClip,
                screamVolume
            );
        }
    }
}
