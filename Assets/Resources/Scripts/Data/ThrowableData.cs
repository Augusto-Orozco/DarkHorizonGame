using UnityEngine;

[CreateAssetMenu(fileName = "NewThrowable", menuName = "Dark Horizon/Items/Throwable")]
public class ThrowableData : ItemData
{
    [SerializeField] private float damage = 50f;
    [Tooltip("Radio de la explosion en metros.")]
    [SerializeField] private float explosionRadius = 4f;
    [Tooltip("Fuerza con la que se lanza.")]
    [SerializeField] private float throwForce = 12f;
    [Tooltip("Segundos desde que se lanza hasta que explota.")]
    [SerializeField] private float fuseTime = 3f;

    public float Damage => damage;
    public float ExplosionRadius => explosionRadius;
    public float ThrowForce => throwForce;
    public float FuseTime => fuseTime;
}