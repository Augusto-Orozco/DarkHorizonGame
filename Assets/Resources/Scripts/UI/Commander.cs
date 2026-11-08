using System;

public static class Commander
{
    public static event Action<string, float> OnMessage;

    public static void Say(string message, float duration = 5f)
    {
        if (!string.IsNullOrEmpty(message))
            OnMessage?.Invoke(message, duration);
    }
}