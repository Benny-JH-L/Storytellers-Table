using UnityEngine;

[RequireComponent (typeof(Animator))]
public class AnimatorHelper : MonoBehaviour
{
    [SerializeField] private Animator animator;     // animations for what ever 
    public GameEventSO gameEvent;
    public bool debugDisabled;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start()
    //{
    //    animator = GetComponent<Animator>();

    //    string msg = "Parameters:\n";
    //    foreach (var param in animator.parameters)
    //    {
    //        msg += $"{param.type} : '{param.name}'\n";
    //    }

    //    if (animator.parameterCount == 0)
    //        msg += "<empty param list>";
    //    DebugOut.Log(this, msg);
    //}

    private void Awake()    //  occures before Start() -- is needed
    {
        debugDisabled = true;

        if (animator == null)
            animator = GetComponent<Animator>();

        string msg = "Parameters:\n";
        foreach (var param in animator.parameters)
        {
            msg += $"{param.type} : '{param.name}'\n";
        }

        if (animator.parameterCount == 0)
            msg += "<empty param list>";
        DebugOut.Log(this, msg, debugDisabled);
    }

    /// <summary>
    /// Calls `animator.SetTrigger(...)` with `triggerName` parameter.
    /// </summary>
    /// <param name="name"></param>
    public void CheckAnimationTrigger(string triggerName)
    {
        animator.SetTrigger(triggerName);
    }

    /// <summary>
    /// Triggers the assigned GameEvent if it is not null.
    /// </summary>
    public void TriggerGameEvent()
    {
        if (gameEvent == null)
            return;
        gameEvent.Raise(this, null);
    }
}
