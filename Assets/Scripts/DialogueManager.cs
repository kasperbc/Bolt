using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

    [SerializeField]
    private string dialogue; // The current dialogue
    [SerializeField]
    private string shownDialogue; // The dialogue that is shown on the dialogue box
    [SerializeField]
    private List<string> queuedDialogue; // The dialogue that is queued next

    // UI
    private GameObject uiPanel; // The dialogue box
    private Text uiText; // The dialogue text
    void Awake()
    {
        // Sets the singleton instance
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Start()
    {
        // Assign the dialogue box and text
        uiPanel = GameObject.Find("DialogueBox");
        uiText = GameObject.Find("DialogueText").GetComponent<Text>();
    }

    /// <summary>
    /// Shows the specified dialogue.
    /// </summary>
    public void SetDialogue(string dia)
    {
        dialogue = dia;

        StopAllCoroutines();
        StartCoroutine(ShowDialogue());
    }

    /// <summary>
    /// Queues the dialogue and shows it if no dialogue is running.
    /// </summary>
    /// <param name="dia"></param>
    public void QueueDialogue(string dia)
    {
        // Is any dialogue running?
        if (!uiPanel.GetComponent<Animator>().GetBool("Show"))
        {
            // If yes, Show the dialogue directly
            SetDialogue(dia);
        }
        else
        {
            // If not, Add the dialogue to the queue
            queuedDialogue.Add(dia);
        }
    }

    /// <summary>
    /// Clears the dialogue queue.
    /// </summary>
    public void ClearQueue()
    {
        queuedDialogue.Clear();
    }

    /// <summary>
    /// Writes the dialogue on the dialogue box
    /// </summary>
    /// <returns></returns>
    public IEnumerator ShowDialogue()
    {
        // Seperates all the characters in the dialogue
        char[] dialogueChars = dialogue.ToCharArray();

        // Shows the dialogue box
        uiPanel.GetComponent<Animator>().SetBool("Show", true);

        // Clears the shown dialogue
        shownDialogue = string.Empty;
        uiText.text = string.Empty;

        // Adds all the characters one by one
        for (int i = 0; i < dialogue.Length; i++)
        {
            // Set the wait time to the next character
            float waitTime = 0.05f;

            // Double the wait time if it's at the end of a sentence
            if (dialogueChars[i].Equals('.') || dialogueChars[i].Equals('!') || dialogueChars.Equals('?'))
            {
                waitTime *= 2.5f;
            }

            // Adds the next character to the shown dialogue
            shownDialogue += dialogueChars[i];
            uiText.text = shownDialogue;

            // Waits a short time (should be adjustable)
            yield return new WaitForSeconds(waitTime);
        }

        // Waits some time
        yield return new WaitForSeconds(2);

        // Is any dialogue queued?
        if (queuedDialogue.Count > 0)
        {
            // Show the next dialogue in queue
            dialogue = queuedDialogue[0];
            queuedDialogue.RemoveAt(0);
            StartCoroutine(ShowDialogue());
        }
        else
        {
            // Hide the dialogue box
            uiPanel.GetComponent<Animator>().SetBool("Show", false);
        }
    }
}
