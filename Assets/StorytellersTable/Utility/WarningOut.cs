using UnityEngine;

public static class WarningOut
{

    /// <summary>
    /// Calls the Unity `Debug.LogWarning(...)` and structures the debug message.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="message"></param>
    public static void Log(object caller, string message)
    {
        Debug.LogWarning($"[{caller.GetType()}] {message}");
    }

}

