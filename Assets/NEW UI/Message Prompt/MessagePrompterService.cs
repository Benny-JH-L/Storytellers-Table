using UnityEngine;
using System.Collections.Generic;

public class MessagePrompterService : MonoBehaviour
{

    [SerializeField] private GameObject promptContainerPrefab;   // use to display messages to user
    [SerializeField] private CameraViewportUpdater cameraViewPortUpdater;   // used to get the active game screen
    [SerializeField] public List<PromptContainer> promptContainers;     // cap size
    //[SerializeField] public List<GameObject> promptContainers;     // cap size
    [SerializeField] [Range(3,5)] private int maxPrompts = 3;     // cap size
    [SerializeField] [Range(50, 100)] private int defaultPosYOffset = 50;     // y-offset for default prompt location
    public bool debugDisabled = false;

    void Start()
    {
        if (promptContainerPrefab.GetComponent<PromptContainer>() == null)
            ErrorOut.Throw(this, "invalid promptContainerPrefab");
        if (GetComponentInParent<Canvas>() == null)
            ErrorOut.Throw(this, "need to have parent with `Canvas`");
        promptContainers = new List<PromptContainer>(maxPrompts);
        //promptContainers = new List<GameObject>(3);
    }

    /// <summary>
    /// Prompts a message to the user via the predefined UI element. Puts the message at the top middle area of the active screen
    /// </summary>
    /// <param name="messsage"></param>
    public void Prompt(string messsage)
    {
        Rect activeScreenRect = cameraViewPortUpdater.screenSpaceGameObject.rect;
        DebugOut.Log(this, $"activeScreenRect: {activeScreenRect}", debugDisabled);

        Vector2 centerOfActiveScreen = new(
            activeScreenRect.width * 0.5f,      // gets the center x position of the rect
            activeScreenRect.height + defaultPosYOffset); // gets the top area of the rect + offset by # of prompts active (is near the top area relative to UI)
        Prompt(messsage, centerOfActiveScreen);
    }

    /// <summary>
    /// Prompts a message to the user via the predefined UI element. Puts the message at the Vector2 position on the screen.
    /// </summary>
    /// <param name="messsage">string</param>
    public void Prompt(string message, Vector2 pos)
    {
        // create an instance of `promptContainer` and send the message
        GameObject containerGameObject = Instantiate(promptContainerPrefab, pos, Quaternion.identity);  // create an instance
        containerGameObject.transform.SetParent(this.transform);                                        // make sure instance is child of this `MessagePrompter`
        
        // get the PromptContainer and send message
        PromptContainer container = containerGameObject.GetComponent<PromptContainer>();
        container.Prompt(message);   

        DebugOut.Log(this, $"spawning prompt at pos: {pos}", debugDisabled);

        // when we've reached the capped # of prompts shown on the screen we remove the oldest one
        if (promptContainers.Count == maxPrompts)
        {
            //Destroy(promptContainers.Dequeue());
            PromptContainer toDestroy = promptContainers[promptContainers.Count - 1];
            //GameObject toDestroy = promptContainers[promptContainers.Count - 1];

            promptContainers.Remove(toDestroy);
            Destroy(toDestroy.gameObject);
            //Destroy(toDestroy);

            DebugOut.Log(this, "removing oldest prompt...", debugDisabled);
        }
        //promptContainers.Enqueue(container);
        promptContainers.Add(container);
        //promptContainers.Add(containerGameObject);
    }
}
