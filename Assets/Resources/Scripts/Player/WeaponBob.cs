using UnityEngine;

public class WeaponBob : MonoBehaviour
{
    [SerializeField] private Transform weaponHolder;

    [Header("Caminar")]
    [SerializeField] private float bobFrequency = 1.6f;
    [SerializeField] private float bobAmountX = 0.04f;
    [SerializeField] private float bobAmountY = 0.06f;
    [SerializeField] private float bobRoll = 2f;
    [SerializeField] private float minSpeed = 0.3f;
    [SerializeField] private float fullSpeed = 6f;

    [Header("Respiracion (quieto)")]
    [SerializeField] private float idleFrequency = 0.6f;
    [SerializeField] private float idleAmountY = 0.01f;

    [Header("Suavizado")]
    [SerializeField] private float smoothing = 10f;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 lastPosition;
    private float phase;
    private float idlePhase;
    private float intensity;
    private Vector3 currentOffset;
    private float currentRoll;

    private void Awake()
    {
        if (weaponHolder == null)
        {
            Debug.LogError("WeaponBob necesita el Weapon Holder.", this);
            enabled = false;
            return;
        }

        baseLocalPosition = weaponHolder.localPosition;
        baseLocalRotation = weaponHolder.localRotation;
        lastPosition = transform.position;
    }

    private void OnDisable()
    {
        if (weaponHolder == null)
            return;

        weaponHolder.localPosition = baseLocalPosition;
        weaponHolder.localRotation = baseLocalRotation;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        lastPosition = transform.position;

        float speed = delta.magnitude / dt;
        float targetIntensity = Mathf.InverseLerp(minSpeed, fullSpeed, speed);
        intensity = Mathf.Lerp(intensity, targetIntensity, smoothing * dt);

        phase += speed * bobFrequency * dt;
        idlePhase += idleFrequency * Mathf.PI * 2f * dt;

        Vector3 walkOffset = new Vector3(
            Mathf.Sin(phase) * bobAmountX,
            -Mathf.Abs(Mathf.Cos(phase)) * bobAmountY,
            0f) * intensity;

        Vector3 idleOffset = new Vector3(0f, Mathf.Sin(idlePhase) * idleAmountY, 0f) * (1f - intensity);

        currentOffset = Vector3.Lerp(currentOffset, walkOffset + idleOffset, smoothing * dt);
        currentRoll = Mathf.Lerp(currentRoll, Mathf.Sin(phase) * bobRoll * intensity, smoothing * dt);

        weaponHolder.localPosition = baseLocalPosition + currentOffset;
        weaponHolder.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, currentRoll);
    }
}