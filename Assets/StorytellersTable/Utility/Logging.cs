
using UnityEngine;

namespace StorytellersTable.Utility.Log
{
    public static class DebugOut
    {
        /// <summary>
        /// Calls the Unity `Debug.Log(...)` and structures the debug message.
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="message"></param>
        public static void Log(object caller, string message)
        {
            Debug.Log($"[{caller.GetType()}] \n{message}");
        }
    }

    public static class WarningOut
    {
        /// <summary>
        /// Calls the Unity `Debug.LogWarning(...)` and structures the debug message.
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="message"></param>
        public static void Log(object caller, string message)
        {
            Debug.LogWarning($"[{caller.GetType()}] \n{message}");
        }

    }

    public class ErrorOut
    {
        /// <summary>
        /// Calls the Unity `Debug.LogError(...)` with a structured message.
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="msg"></param>
        /// <exception cref="System.Exception"></exception>
        public static void Log(object caller, string msg)
        {
            Debug.LogError($"[{caller.GetType()}] \n{msg}");
        }
    }
}
