using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    // Es true mientras el menú de pausa (o los ajustes abiertos desde él) está visible.
    public static bool IsPaused { get; private set; }

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private string menuSceneName = "StartScene";

    private CursorLockMode previousLock;
    private bool previousVisible;
    private bool subscribed;

    private void Awake()
    {
        IsPaused = false;
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        Unsubscribe();
        IsPaused = false;
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        SettingsManager settings = SettingsManager.Instance;
        if (IsPaused && settings != null && settings.IsOpen)
        {
            settings.CloseSettings(); // vuelve al menú de pausa con el evento Closed
            return;
        }

        if (IsPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        if (IsPaused)
            return;

        previousLock = Cursor.lockState;
        previousVisible = Cursor.visible;
        IsPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        pausePanel.SetActive(true);
    }

    public void Resume()
    {
        if (!IsPaused)
            return;

        IsPaused = false;
        pausePanel.SetActive(false);
        Cursor.lockState = previousLock;
        Cursor.visible = previousVisible;
    }

    // OnClick del botón Ajustes del PauseMenu.
    public void OpenSettings()
    {
        if (SettingsManager.Instance == null)
        {
            Debug.LogWarning("PauseMenuController: no hay SettingsManager (abre el juego desde StartScene).");
            return;
        }

        Subscribe();
        pausePanel.SetActive(false);
        SettingsManager.Instance.OpenSettings();
    }

    private void OnSettingsClosed()
    {
        Unsubscribe();
        if (IsPaused)
            pausePanel.SetActive(true);
    }

    private void Subscribe()
    {
        if (subscribed || SettingsManager.Instance == null) return;
        SettingsManager.Instance.Closed += OnSettingsClosed;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.Closed -= OnSettingsClosed;
        subscribed = false;
    }

    // OnClick del botón Salir al menú.
    public void QuitToMenu()
    {
        IsPaused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ReturnToMenu.Go(menuSceneName);
    }
}
