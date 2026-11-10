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
    [SerializeField, Min(0f)] private float footstepsSpeedThreshold = 0.3f;

    [Header("Configuración del audio de ataque")]
    [SerializeField] private float attackAudioGraceTime = 0.15f;

    private enum AudioMood : byte
    {
        Idle,
        Chasing,
        Attacking
    }

    private AudioSource idleSource;
    private AudioSource footstepsSource;
    private AudioSource screamSource;
    private AudioSource attackSource;

    private AudioMood audioMood;
    private bool audioReady;
    private Vector3 lastAudioPosition;
    private NetworkEnemyVisuals networkVisuals;

    private bool wasChasing;
    private float nextScreamTime;
    private bool attackAudioActive;
    private float attackAudioStopTime;

    private void LateUpdate()
    {
        AudioMood mood = state == MariangulaState.Attacking
            ? AudioMood.Attacking
            : state == MariangulaState.Chasing
                ? AudioMood.Chasing
                : AudioMood.Idle;

        if (mood != audioMood)
        {
            audioMood = mood;

            if (networkVisuals == null)
                networkVisuals = GetComponent<NetworkEnemyVisuals>();

            if (networkVisuals != null)
                networkVisuals.SendAudioMood((byte)mood);
        }

        TickAudio();
    }

    public void SetAudioMood(byte mood)
    {
        audioMood = (AudioMood)mood;
    }

    public void TickAudio()
    {
        if (!audioReady)
            SetupAudio();

        UpdateAudioState(IsMovingForAudio());
    }

    private void SetupAudio()
    {
        idleSource = CreateAudioSource(idleClip, true, idleVolume);
        footstepsSource = CreateAudioSource(footstepsClip, true, footstepsVolume);
        screamSource = CreateAudioSource(screamClip, false, screamVolume);
        attackSource = CreateAudioSource(attackClip, true, attackVolume);

        lastAudioPosition = transform.position;
        audioReady = true;
    }

    private bool IsMovingForAudio()
    {
        Vector3 position = transform.position;
        float speed = (position - lastAudioPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastAudioPosition = position;
        return speed > footstepsSpeedThreshold;
    }

    private void UpdateAudioState(bool isMoving)
    {
        bool isAttacking = audioMood == AudioMood.Attacking;
        bool isChasing = audioMood == AudioMood.Chasing;

        if (isAttacking)
        {
            StopLoop(idleSource);
            StopLoop(footstepsSource);

            if (!attackAudioActive)
            {
                attackAudioActive = true;
                PlayLoop(attackSource);
            }

            attackAudioStopTime = Time.time + attackAudioGraceTime;
            UpdateScream();
            wasChasing = true;
            return;
        }

        if (attackAudioActive && Time.time >= attackAudioStopTime)
        {
            StopLoop(attackSource);
            attackAudioActive = false;
        }

        if (isChasing)
        {
            UpdateScream();
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
                PlayLoop(idleSource);
            else
                StopLoop(idleSource);
        }
    }

    private void UpdateScream()
    {
        if (!wasChasing || Time.time >= nextScreamTime)
        {
            PlayScream();
            nextScreamTime = Time.time + screamInterval;
        }
    }

    private AudioSource CreateAudioSource(AudioClip clip, bool loop, float volume)
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
            source.Play();
    }

    private void StopLoop(AudioSource source)
    {
        if (source != null && source.isPlaying)
            source.Stop();
    }

    private void PlayScream()
    {
        if (screamSource != null && screamClip != null)
            screamSource.PlayOneShot(screamClip, screamVolume);
    }
}