using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Is the collided object a player?
        if (collision.gameObject.CompareTag("Player"))
        {
            // Does the player have it's pizza?
            if (collision.gameObject.GetComponent<PlayerHealth>().GetPizzaStatus() == true)
            {
                // If not, do not activate the checkpoint
                return;
            }

            // Update the player's checkpoint
            Collider2D checkPointColl = GetComponent<Collider2D>();
            Vector2 checkPointPos = new Vector2(transform.position.x, transform.position.y - checkPointColl.bounds.extents.y);
            GameManager.instance.UpdateCheckpoint(checkPointPos);

            print("Checkpoint activated. (x " + checkPointPos.x + " y " + checkPointPos.y + ")");
        }
    }
}
