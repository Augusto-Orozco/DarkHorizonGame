using System;
using UnityEngine;

// Ajustes del jugador local. Se guardan en PlayerPrefs.
public static class GameSettings
{
    private const string KeyVolume = "set_volume";
    private const string KeySensitivity = "set_sensitivity";
    private const string KeyInvertY = "set_invertY";
    private const string KeyFullscreen = "set_fullscreen";

    public const float DefaultVolume = 1f;
    public const float DefaultSensitivity = 1f;
    public const bool DefaultInvertY = false;
    public const bool DefaultFullscreen = true;

    // Rango permitido de la sensibilidad
    public const float MinSensitivity = 0.1f;
    public const float MaxSensitivity = 5f;

    public static float Volume { get; private set; } = DefaultVolume;
    public static float Sensitivity { get; private set; } = DefaultSensitivity;
    public static bool InvertY { get; private set; } = DefaultInvertY;
    public static bool Fullscreen { get; private set; } = DefaultFullscreen;

    // Se dispara cada vez que cambia algún valor.
    public static event Action Changed;

    // Signo que se multiplica al movimiento vertical del mouse.
    public static float PitchSign => InvertY ? -1f : 1f;

    private static bool loaded;

    // Carga y aplica lo guardado al iniciar, sin importar con qué escena se abra el juego.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => Load();

    public static void EnsureLoaded()
    {
        if (!loaded)
            Load();
    }

    // Lee lo guardado en PlayerPrefs y lo aplica (descarta los cambios sin guardar).
    public static void Load()
    {
        loaded = true;
        Volume = PlayerPrefs.GetFloat(KeyVolume, DefaultVolume);
        Sensitivity = PlayerPrefs.GetFloat(KeySensitivity, DefaultSensitivity);
        InvertY = PlayerPrefs.GetInt(KeyInvertY, DefaultInvertY ? 1 : 0) == 1;
        Fullscreen = PlayerPrefs.GetInt(KeyFullscreen, DefaultFullscreen ? 1 : 0) == 1;
        Apply();
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(KeyVolume, Volume);
        PlayerPrefs.SetFloat(KeySensitivity, Sensitivity);
        PlayerPrefs.SetInt(KeyInvertY, InvertY ? 1 : 0);
        PlayerPrefs.SetInt(KeyFullscreen, Fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void ResetToDefaults()
    {
        Volume = DefaultVolume;
        Sensitivity = DefaultSensitivity;
        InvertY = DefaultInvertY;
        Fullscreen = DefaultFullscreen;
        Apply();
    }

    public static void SetVolume(float v) { Volume = Mathf.Clamp01(v); Apply(); }
    public static void SetSensitivity(float v) { Sensitivity = Mathf.Clamp(v, MinSensitivity, MaxSensitivity); Apply(); }
    public static void SetInvertY(bool v) { InvertY = v; Apply(); }
    public static void SetFullscreen(bool v) { Fullscreen = v; Apply(); }

    private static void Apply()
    {
        AudioListener.volume = Volume;
        if (Screen.fullScreen != Fullscreen)
            Screen.fullScreen = Fullscreen;
        Changed?.Invoke();
    }
}
