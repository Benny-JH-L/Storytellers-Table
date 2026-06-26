using UnityEngine;

public static class DebugOut
{

    /// <summary>
    /// Calls the Unity `Debug.Log(...)` and structures the debug message.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="message"></param>
    public static void Log(object caller, string message)
    {
        Debug.Log($"[{caller.GetType()}] {message}");
    }

}

