using UnityEngine;

public class StartMenu : MonoBehaviour
{
    [Header("Camara")]
    [SerializeField] private Transform mainCamera;
    [Tooltip("Coordenadas a las que se movera la camara (copiadas de Main Camera (1)).")]
    [SerializeField] private Vector3 targetPosition = new Vector3(-0.66f, 0.76f, -8.23f);
    [SerializeField] private Vector3 targetRotation = new Vector3(0f, -50f, 0f);
    [SerializeField] private float cameraSpeed = 2f;

    [Header("Paneles")]
    [SerializeField] private GameObject startPanel;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 goalPosition;
    private Quaternion goalRotation;
    private GameObject currentPanel;
    private bool movingCamera;

    private void Awake()
    {
        initialPosition = mainCamera.position;
        initialRotation = mainCamera.rotation;
    }

    private void Update()
    {
        if (!movingCamera)
            return;

        float t = cameraSpeed * Time.deltaTime;
        mainCamera.position = Vector3.Lerp(mainCamera.position, goalPosition, t);
        mainCamera.rotation = Quaternion.Slerp(mainCamera.rotation, goalRotation, t);
    }

    // OnClick de Play y Settings: arrastrar el panel que debe mostrarse.
    public void ShowPanel(GameObject panelToShow)
    {
        goalPosition = targetPosition;
        goalRotation = Quaternion.Euler(targetRotation);
        movingCamera = true;
        currentPanel = panelToShow;
        startPanel.SetActive(false);
        panelToShow.SetActive(true);
    }

    // OnClick del boton Regresar: oculta el ultimo panel mostrado.
    public void Back()
    {
        goalPosition = initialPosition;
        goalRotation = initialRotation;
        movingCamera = true;
        if (currentPanel != null)
            currentPanel.SetActive(false);

        startPanel.SetActive(true);
    }

    // OnClick de Exit.

    public void ExitGame()
    {
#if UNITY_EDITOR
        // En el editor, Application.Quit no hace nada: detenemos el Play
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
