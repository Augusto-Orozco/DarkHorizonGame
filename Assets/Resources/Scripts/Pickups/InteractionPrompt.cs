using UnityEngine;
using TMPro;


public class InteractionPrompt : MonoBehaviour
{
    [Header("Texto")]
    [SerializeField] private float fontSize=3f;
    [SerializeField] private Color color = Color.white;

    [Header("Posicion")]
    [Tooltip("Altura extra arriba del modelo.")]
    [SerializeField] private float extraHeight = 0.35f;
    [Tooltip("Cuanto sube y baja. 0 = quieto.")]
    [SerializeField] private float bobAmount = 0.05f;
    [SerializeField] private float bobSpeed = 2.5f;
    [SerializeField] private bool alwaysOnTop = true;

    private TextMeshPro text;
    private Transform textTransform;
    private float height;
    private Camera cachedCamera;

    public bool IsVisible => textTransform != null && textTransform.gameObject.activeSelf;

    private void Awake()
    {
        height = CalculateHeight();
        CreateText();
        Hide();
    }
    public void Show(string message)
    {
        if (text == null)
            return;
        text.text = message;
        textTransform.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (textTransform != null)
            textTransform.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!IsVisible)
            return;
        float bob = bobAmount > 0f ? Mathf.Sin(Time.time * bobSpeed) * bobAmount : 0f;
        textTransform.position = transform.position + Vector3.up * (height + bob);

        if (cachedCamera == null || !cachedCamera.isActiveAndEnabled)
            cachedCamera = Camera.main;

        if(cachedCamera != null)
            textTransform.rotation = Quaternion.LookRotation(textTransform.position - cachedCamera.transform.position);
    }

    private void OnDiable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if(textTransform != null)
            Destroy(textTransform.gameObject);
    }
    private float CalculateHeight()
    {
        float top = transform.position.y;
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            top = Mathf.Max(top, r.bounds.max.y);
        return (top - transform.position.y) + extraHeight;
    }

    private void CreateText()
    {
        GameObject textObject = new GameObject(name + "_Prompt");
        text = textObject.AddComponent<TextMeshPro>();
        textTransform = text.transform;                 
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(4f, 1f);

        if (text.font != null)
        {
            text.outlineWidth = 0.2f;
            text.outlineColor = Color.black;
        }
        else
        {
            Debug.LogWarning("Falta fuente");
        }

        if (alwaysOnTop)
        {
            Shader overlay = Shader.Find("TextMeshPro/Distance Field Overlay");
            if (overlay != null)
                text.fontMaterial.shader = overlay;
        }
    }

    [ContextMenu("Probar: mostrar")]
    private void DebugShow() => Show("[E] Prueb");

    [ContextMenu("Probar: ocultar")]
    private void DebugHide() => Hide();



}
