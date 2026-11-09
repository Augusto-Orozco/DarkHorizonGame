
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

    // =========================================================
    // START
    // =========================================================

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

    // =========================================================
    // LATE UPDATE
    // =========================================================

    private void LateUpdate()
    {
        if (idleSource == null)
            return;

        UpdateAudioState();
    }

    // =========================================================
    // ACTUALIZAR ESTADOS DE AUDIO
    // =========================================================

    private void UpdateAudioState()
    {
        bool isAttacking = state == MariangulaState.Attacking;
        bool isChasing = state == MariangulaState.Chasing;

        // Movimiento real de la animación.
        bool isMoving =
            animationState == AnimationState.PreMove ||
            animationState == AnimationState.Move ||
            animationState == AnimationState.PostMove;

        // =====================================================
        // ATAQUE
        // =====================================================

        if (isAttacking)
        {
            StopLoop(idleSource);
            StopLoop(footstepsSource);

            // Solo inicia el audio si todavía no está activo.
            if (!attackAudioActive)
            {
                attackAudioActive = true;
                PlayLoop(attackSource);
            }

            // Mientras ataque, se amplía el tiempo antes de detenerlo.
            attackAudioStopTime =
                Time.time + attackAudioGraceTime;

            // Los gritos continúan durante el ataque.
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

        // =====================================================
        // DETENER AUDIO DE ATAQUE
        // =====================================================

        if (attackAudioActive &&
            Time.time >= attackAudioStopTime)
        {
            StopLoop(attackSource);
            attackAudioActive = false;
        }

        // =====================================================
        // PERSECUCIÓN Y GRITOS
        // =====================================================

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

        // =====================================================
        // PASOS
        // =====================================================

        if (isChasing && isMoving)
        {
            StopLoop(idleSource);
            PlayLoop(footstepsSource);
        }
        else
        {
            StopLoop(footstepsSource);

            // =================================================
            // IDLE
            // =================================================

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

    // =========================================================
    // CREAR AUDIO SOURCE
    // =========================================================

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

    // =========================================================
    // REPRODUCIR LOOP
    // =========================================================

    private void PlayLoop(AudioSource source)
    {
        if (source != null &&
            source.clip != null &&
            !source.isPlaying)
        {
            source.Play();
        }
    }

    // =========================================================
    // DETENER LOOP
    // =========================================================

    private void StopLoop(AudioSource source)
    {
        if (source != null && source.isPlaying)
        {
            source.Stop();
        }
    }

    // =========================================================
    // GRITO
    // =========================================================

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
