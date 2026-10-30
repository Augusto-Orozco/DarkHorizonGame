using UnityEngine;

public class LightCircleBinder : MonoBehaviour
{
    [SerializeField] private LightsRange lightsRange;
    [SerializeField] private FillCircleUI circle;
    [SerializeField] private int maxPlayers = 4;

    private void OnEnable()
    {
        if (lightsRange == null || circle == null)
        {
            Debug.LogWarning("Falta algo", this);
            return;
        }

        lightsRange.OnJugadoresCercanosChanged += HandlePlayersChanged;
        HandlePlayersChanged(lightsRange.JugadoresCercanosActual);
    }

    private void OnDisable()
    {
        if (lightsRange == null)
            return;

        lightsRange.OnJugadoresCercanosChanged -= HandlePlayersChanged;
    }

    private void HandlePlayersChanged(int count)
    {
        circle.SetValue((float)count / maxPlayers);
    }
}