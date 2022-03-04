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
        uiPanel = GameObject.Find("DialogueBox");
        uiText = GameObject.Find("DialogueText").GetComponent<Text>();

        SetDialogue("Testing 12345");
        QueueDialogue("Testing 123456789");
    }

    /// <summary>
    /// Shows the specified dialogue.
    /// </summary>
    public void SetDialogue(string dia)
    {
        dialogue = dia;

        StartCoroutine(ShowDialogue());
    }

    /// <summary>
    /// Queues the dialogue and shows it if no dialogue is running.
    /// </summary>
    /// <param name="dia"></param>
    public void QueueDialogue(string dia)
    {
        // Add the dialogue to the queue
        queuedDialogue.Add(dia);

        // Show the dialogue if none are running
        if (!uiPanel.GetComponent<Animator>().GetBool("Show"))
        {
            StartCoroutine(ShowDialogue());
        }
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
            // Adds the next character to the shown dialogue
            shownDialogue += dialogueChars[i];
            uiText.text = shownDialogue;

            // Waits a short time (should be adjustable)
            yield return new WaitForSeconds(0.1f);
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
