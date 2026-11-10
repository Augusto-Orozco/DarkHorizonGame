using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class EnemyAudio : MonoBehaviourPun
{
    public enum Sound : byte
    {
        Alert,
        Attack,
        Rage,
        Hurt,
        Death
    }

    [Header("Loops")]
    [SerializeField] private AudioClip idleLoop;
    [SerializeField] private AudioClip moveLoop;

    [Header("Eventos")]
    [SerializeField] private AudioClip alertClip;
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip rageClip;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip deathClip;

    [Header("Volumen")]
    [SerializeField, Range(0f, 1f)] private float loopVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("Ajustes")]
    [SerializeField] private Transform body;
    [SerializeField, Min(0f)] private float alertRepeatInterval = 0f;
    [SerializeField, Min(0f)] private float moveSpeedThreshold = 0.3f;
    [SerializeField, Min(0f)] private float hurtCooldown = 0.3f;
    [SerializeField, Min(1f)] private float maxDistance = 40f;

    private AudioSource loopSource;
    private AudioSource sfxSource;
    private EnemyHealth health;
    private Vector3 lastPosition;
    private bool chasing;
    private bool dead;
    private float nextAlertTime;
    private float nextHurtTime;
    private float lastHealth = -1f;

    private bool CanSend => !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;

    private void Awake()
    {
        health = GetComponentInChildren<EnemyHealth>();

        if (body == null)
            body = health != null ? health.transform : transform;

        loopSource = CreateSource(true, loopVolume);
        sfxSource = CreateSource(false, sfxVolume);
        lastPosition = body.position;
    }

    private void OnEnable()
    {
        if (health == null)
            return;

        health.OnHealthChanged += HandleHealthChanged;
        health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (health == null)
            return;

        health.OnHealthChanged -= HandleHealthChanged;
        health.OnDied -= HandleDied;
    }

    private void Update()
    {
        if (dead)
            return;

        Vector3 position = body.position;
        float speed = (position - lastPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = position;

        AudioClip wanted = speed > moveSpeedThreshold && moveLoop != null ? moveLoop : idleLoop;
        if (loopSource.clip != wanted)
        {
            loopSource.clip = wanted;
            if (wanted != null)
                loopSource.Play();
            else
                loopSource.Stop();
        }
        else if (wanted != null && !loopSource.isPlaying)
        {
            loopSource.Play();
        }

        if (chasing && alertRepeatInterval > 0f && Time.time >= nextAlertTime)
        {
            PlayLocal(Sound.Alert);
            nextAlertTime = Time.time + alertRepeatInterval;
        }
    }

    public void SetChasing(bool value)
    {
        if (!CanSend || value == chasing)
            return;

        ApplyChasing(value);

        if (PhotonNetwork.InRoom)
            photonView.RPC(nameof(RpcSetChasing), RpcTarget.Others, value);
    }

    public void PlayEvent(Sound sound)
    {
        if (!CanSend)
            return;

        PlayLocal(sound);

        if (PhotonNetwork.InRoom)
            photonView.RPC(nameof(RpcPlayEvent), RpcTarget.Others, (byte)sound);
    }

    [PunRPC]
    private void RpcSetChasing(bool value)
    {
        ApplyChasing(value);
    }

    [PunRPC]
    private void RpcPlayEvent(byte sound)
    {
        PlayLocal((Sound)sound);
    }

    private void ApplyChasing(bool value)
    {
        chasing = value;

        if (chasing)
        {
            PlayLocal(Sound.Alert);
            nextAlertTime = Time.time + alertRepeatInterval;
        }
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (lastHealth >= 0f && current < lastHealth && current > 0f && Time.time >= nextHurtTime)
        {
            PlayLocal(Sound.Hurt);
            nextHurtTime = Time.time + hurtCooldown;
        }

        lastHealth = current;
    }

    private void HandleDied()
    {
        if (dead)
            return;

        dead = true;
        loopSource.Stop();

        if (deathClip != null)
            AudioSource.PlayClipAtPoint(deathClip, body.position, sfxVolume);
    }

    private void PlayLocal(Sound sound)
    {
        AudioClip clip = ClipFor(sound);
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip, sfxVolume);
    }

    private AudioClip ClipFor(Sound sound)
    {
        switch (sound)
        {
            case Sound.Alert: return alertClip;
            case Sound.Attack: return attackClip;
            case Sound.Rage: return rageClip;
            case Sound.Hurt: return hurtClip;
            case Sound.Death: return deathClip;
            default: return null;
        }
    }

    private AudioSource CreateSource(bool loop, float volume)
    {
        AudioSource source = body.gameObject.AddComponent<AudioSource>();
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.maxDistance = maxDistance;
        source.volume = volume;
        return source;
    }
}