using UnityEngine;
using UnityEngine.UI;

public class DamageFlashUI : MonoBehaviour
{
    [SerializeField] private Image flashImage;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.6f;
    [SerializeField, Min(0.05f)] private float fadeDuration = 0.6f;

    private PlayerHealth health;
    private Color currentColor;
    private float alpha;

    private void Awake()
    {
        health = GetComponentInParent<PlayerHealth>();

        if (flashImage != null)
        {
            flashImage.raycastTarget = false;
            flashImage.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnHitFlash += HandleHitFlash;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnHitFlash -= HandleHitFlash;
    }

    private void HandleHitFlash(Color color)
    {
        currentColor = color;
        alpha = maxAlpha;
    }

    private void Update()
    {
        if (flashImage == null || alpha <= 0f)
            return;

        alpha = Mathf.MoveTowards(alpha, 0f, maxAlpha / fadeDuration * Time.deltaTime);
        flashImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
    }
}
