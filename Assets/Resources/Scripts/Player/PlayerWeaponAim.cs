using UnityEngine;
using UnityEngine.UI;

public class PlayerWeaponAim : MonoBehaviour
{
    private const float MaxAimDistance = 1000f;

    private readonly RaycastHit[] raycastHits = new RaycastHit[32];

    private Camera aimCamera;
    private Canvas crosshairCanvas;
    private bool warnedAboutMissingCamera;

    private void Awake()
    {
        CreateCrosshair();
    }

    private void OnDestroy()
    {
        if (crosshairCanvas != null)
            Destroy(crosshairCanvas.gameObject);
    }

    public void SetWeaponActive(bool active)
    {
        if (crosshairCanvas != null)
            crosshairCanvas.enabled = active;
    }

    public Vector3 GetAimDirection(Transform muzzle)
    {
        if (muzzle == null)
            return Vector3.forward;

        Camera camera = GetAimCamera();
        if (camera == null)
            return muzzle.forward;

        Ray aimRay = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Vector3 aimPoint = aimRay.GetPoint(MaxAimDistance);
        float closestHitDistance = MaxAimDistance;
        int hitCount = Physics.RaycastNonAlloc(
            aimRay,
            raycastHits,
            MaxAimDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = raycastHits[i];
            if (hit.collider == null || hit.transform.root == transform.root)
                continue;

            if (hit.distance < closestHitDistance)
            {
                closestHitDistance = hit.distance;
                aimPoint = hit.point;
            }
        }

        Vector3 direction = aimPoint - muzzle.position;
        return direction.sqrMagnitude > 0.0001f &&
            Vector3.Dot(direction, aimRay.direction) > 0f
            ? direction.normalized
            : aimRay.direction;
    }

    private Camera GetAimCamera()
    {
        if (aimCamera != null && aimCamera.isActiveAndEnabled)
            return aimCamera;

        aimCamera = Camera.main;
        if (aimCamera == null && !warnedAboutMissingCamera)
        {
            Debug.LogWarning(
                "PlayerWeaponAim necesita una cámara activa con la etiqueta MainCamera.",
                this
            );
            warnedAboutMissingCamera = true;
        }

        return aimCamera;
    }

    private void CreateCrosshair()
    {
        GameObject canvasObject = new GameObject("Weapon Aim Crosshair");
        crosshairCanvas = canvasObject.AddComponent<Canvas>();
        crosshairCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        crosshairCanvas.sortingOrder = 100;

        RectTransform crosshair = CreateRectTransform(
            "Crosshair",
            crosshairCanvas.transform,
            Vector2.zero,
            Vector2.zero
        );
        crosshair.anchorMin = new Vector2(0.5f, 0.5f);
        crosshair.anchorMax = new Vector2(0.5f, 0.5f);
        crosshair.pivot = new Vector2(0.5f, 0.5f);

        Color color = new Color(1f, 0.9f, 0.3f, 0.95f);
        CreateBar(crosshair, "Left", new Vector2(-7f, 0f), new Vector2(8f, 2f), color);
        CreateBar(crosshair, "Right", new Vector2(7f, 0f), new Vector2(8f, 2f), color);
        CreateBar(crosshair, "Top", new Vector2(0f, 7f), new Vector2(2f, 8f), color);
        CreateBar(crosshair, "Bottom", new Vector2(0f, -7f), new Vector2(2f, 8f), color);

        crosshairCanvas.enabled = false;
    }

    private static void CreateBar(
        RectTransform parent,
        string barName,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        RectTransform bar = CreateRectTransform(
            barName,
            parent,
            position,
            size
        );
        Image image = bar.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static RectTransform CreateRectTransform(
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2 size)
    {
        GameObject element = new GameObject(objectName, typeof(RectTransform));
        element.transform.SetParent(parent, false);

        RectTransform rectTransform = element.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
        return rectTransform;
    }
}
