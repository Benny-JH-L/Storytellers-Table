using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a prompt via UI elements but will destroy itself on finishing its animaton.
/// </summary>
public class PromptContainer : AbstractUI
{
    [SerializeField] private TTRPG_Text_New text;
    [SerializeField] private Image backgroundImg;   // makes the text easier to see (not neccessary)
    [SerializeField] private AnimatorHelper animatorHelper;   // animations for this container
    [SerializeField] private MessagePrompterService parentPrompter;  // instantiated by this class

    public override void Configure()
    {
        // nothing
    }

    public override void Setup()
    {
        text = GetComponentInChildren<TTRPG_Text_New>();
        backgroundImg = GetComponentInChildren<Image>();
        animatorHelper = GetComponent<AnimatorHelper>();
        parentPrompter = GetComponentInParent<MessagePrompterService>();
    }

    private void Start()
    {
        parentPrompter = GetComponentInParent<MessagePrompterService>();   // needed

    }


    /// <summary>
    /// Prompts a message to the user via the predefined UI element. Puts the message at the middle of the screen.
    /// </summary>
    /// <param name="msg"></param>
    public void Prompt(string msg)
    {
        //this.transform.position = Vector3.zero; // tmp
        text.SetText(msg);
        animatorHelper.CheckAnimationTrigger("Prompt");
    }

    /// <summary>
    /// This must be called at the end of an animation if you want this object to be destroyed!
    /// </summary>
    public void OnAnimationEnd()
    {
        // once the animation ends, delete self
        parentPrompter.promptContainers.Remove(this);
        Destroy(this.gameObject);
    }
}
