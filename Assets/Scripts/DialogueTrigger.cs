using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [TextArea]
    public string dialogue;
    [Tooltip("Overwrites the current dialogue")]
    public bool showInstantly;
    [Tooltip("How many seconds does the player have to stay in the trigger zone for the dialogue to activate? (0 = No wait)")]
    public float requiredStayDuration;
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (requiredStayDuration > 0)
            {
                StartCoroutine(CheckIfPlayerStays(requiredStayDuration));
            }
            else
            {
                // Show/queue the dialogue
                TriggerDialogue();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Destroy the trigger if the player exits the trigger zone. (Used for stay duration)
            Destroy(gameObject);
        }
    }

    void TriggerDialogue()
    {
        // Show/queue the dialogue
        if (showInstantly)
        {
            DialogueManager.instance.SetDialogue(dialogue);
            DialogueManager.instance.ClearQueue();
        }
        else
        {
            DialogueManager.instance.QueueDialogue(dialogue);
        }


        // Destroy the trigger to avoid triggering the dialogue multiple times.
        Destroy(gameObject);
    }

    /// <summary>
    /// Trigger the dialogue if the player is in the dialogue zone after the specified amount of seconds.
    /// </summary>
    /// <param name="seconds"></param>
    /// <returns></returns>
    IEnumerator CheckIfPlayerStays(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        TriggerDialogue();
    }
}
