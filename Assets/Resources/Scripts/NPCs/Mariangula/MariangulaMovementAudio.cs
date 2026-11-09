
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
    [SerializeField, Range(0f, 1f)] private float idleVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float footstepsVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float screamVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float attackVolume = 1f;

    private AudioSource idleSource;
    private AudioSource footstepsSource;
    private AudioSource screamSource;
    private AudioSource attackSource;

    private bool wasChasing;
    private float nextScreamTime;

    // Unity ejecuta Start además del Update que ya existe en la clase base.
    private void Start()
    {
        idleSource = CreateAudioSource(idleClip, true, idleVolume);
        footstepsSource = CreateAudioSource(footstepsClip, true, footstepsVolume);
        screamSource = CreateAudioSource(screamClip, false, screamVolume);
        attackSource = CreateAudioSource(attackClip, true, attackVolume);
    }

    // Lee los estados de la IA después de que se ejecute su Update.
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

    // Movimiento real de la animación
    bool isMoving =
        animationState == AnimationState.PreMove ||
        animationState == AnimationState.Move ||
        animationState == AnimationState.PostMove;

    // ATAQUE
    if (isAttacking)
    {
        StopLoop(idleSource);
        StopLoop(footstepsSource);

        PlayLoop(attackSource);

        // Los gritos continúan durante el ataque
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

    StopLoop(attackSource);

    // PERSECUCIÓN Y GRITOS
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

    // PASOS: únicamente cuando persigue al jugador
    if (isChasing && isMoving)
    {
        StopLoop(idleSource);
        PlayLoop(footstepsSource);
    }
    else
    {
        StopLoop(footstepsSource);

        // IDLE: cuando no está persiguiendo ni atacando
        if (!isChasing)
        {
            PlayLoop(idleSource);
        }
        else
        {
            StopLoop(idleSource);
        }
    }
}    private AudioSource CreateAudioSource(
        AudioClip clip,
        bool loop,
        float volume)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();

        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.volume = volume;

        return source;
    }

    private void PlayLoop(AudioSource source)
    {
        if (source != null && source.clip != null && !source.isPlaying)
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
        if (screamSource != null && screamClip != null)
        {
            screamSource.PlayOneShot(screamClip, screamVolume);
        }
    }
}
