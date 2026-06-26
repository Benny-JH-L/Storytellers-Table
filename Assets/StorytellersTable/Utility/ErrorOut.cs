using UnityEngine;

public class ErrorOut
{

    /// <summary>
    /// Throws a new System.Exception(...) with a structured message.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="msg"></param>
    /// <exception cref="System.Exception"></exception>
    public static void Throw(object caller, string msg)
    {
        throw new System.Exception($"[{caller.GetType()}] {msg}");
    }
}
