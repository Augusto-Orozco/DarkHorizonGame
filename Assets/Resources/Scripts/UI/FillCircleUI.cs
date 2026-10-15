using UnityEngine;
using UnityEngine.UI;

public class FillCircleUI : MonoBehaviour
{
    [SerializeField] private Image fill;

    [Tooltip("Que tan rapido el relleno hacia el nuevo valor")]
    [SerializeField] private float smoothSpeed = 2f;

    [Tooltip("Valor actual, se puede mover cuando esta en Play para poder probar")]
    [Range(0f, 1f)]
    [SerializeField] private float value = 1f;


    public void SetValue(float normalizedValue)
    {
        value = Mathf.Clamp01(normalizedValue);
    }

    private void Start()
    {
        if (fill == null)
        {
            Debug.LogWarning("FillCircleUI sin Image de relleno asignada.", this);
            enabled = false;
            return;
        }

        fill.fillAmount = value;
    }

    private void Update()
    {
        fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, value, smoothSpeed * Time.deltaTime);
    }
}
