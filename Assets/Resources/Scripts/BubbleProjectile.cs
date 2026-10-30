using UnityEngine;

public class BubbleProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float lifetime;

    public void Initialize(Vector3 launchDirection, float launchSpeed, float duration)
    {
        direction = launchDirection.normalized;
        speed = launchSpeed;
        lifetime = duration;
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
        lifetime -= Time.deltaTime;

        if (lifetime <= 0f)
            Destroy(gameObject);
    }
}
