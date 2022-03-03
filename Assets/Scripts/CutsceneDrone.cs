using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneDrone : MonoBehaviour
{
    private GameObject player;          // The player GameObject
    private PlayerMovement playerBeh;   // The player movement script
    [SerializeField]
    private Vector2 targetPoint;        // The target point of the drone
    private Vector2 startPoint;         // The starting point of the drone
    private bool carryingPlayer;   // Is the drone moving towards the target point?
    [SerializeField]
    private float moveSpeed = 5f;       // The speed of the drone
    void Start()
    {
        player = GameObject.FindWithTag("Player");
        playerBeh = player.GetComponent<PlayerMovement>();
        
    }

    // Update is called once per frame
    void Update()
    {
        // Is the drone carrying the player?
        if (carryingPlayer)
        {
            // If yes, carry the player towards the target
            MoveTowardsTarget(true);

            // Is the drone close enough to the target?
            if (Vector2.Distance(transform.position, targetPoint) < 3)
            {
                // If yes, let go of the player
                DisableDrone();
            }
        }
        else
        {
            // If not, move the drone towards the target
            MoveTowardsTargetIncreasingSpeed(false);

            // Despawn the drone if it's close enough to the target point
            if (Vector2.Distance(transform.position, targetPoint) < 2)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Move towards the target point and carry the player
    /// </summary>
    public void MoveTowardsTarget(bool carryPlayer)
    {
        // Move towards the target point
        transform.position = Vector2.Lerp(transform.position, targetPoint, moveSpeed * Time.deltaTime);

        if (carryPlayer)
        {
            player.transform.position = new Vector2(transform.position.x, transform.position.y - 0.5f);
        }
    }

    void MoveTowardsTargetIncreasingSpeed(bool carryPlayer)
    {
        // Increase the move speed
        moveSpeed *= 1.03f;

        // Move towards the target point
        transform.position = Vector2.MoveTowards(transform.position, targetPoint, moveSpeed * Time.deltaTime);

        if (carryPlayer)
        {
            player.transform.position = new Vector2(transform.position.x, transform.position.y - 0.5f);
        }
    }

    /// <summary>
    /// Enables the drone and carries the player towards the target point
    /// </summary>
    public void EnableDrone()
    {
        // Set the drone to be moving
        carryingPlayer = true;

        // Disable player movement
        playerBeh.SetCutscene(true);
        player.GetComponent<Rigidbody2D>().gravityScale = 0;
    }

    public void DisableDrone()
    {
        // Set the drone to be not moving
        carryingPlayer = false;

        // Enable player movement
        playerBeh.SetCutscene(false);
        player.GetComponent<Rigidbody2D>().gravityScale = 1;
        playerBeh.ForceJump();

        moveSpeed = 0;

        Invoke("FlyAway", 2);
    }

    /// <summary>
    /// Sets new target position to be high up
    /// </summary>
    private void FlyAway()
    {
        // Set a new target point to the air
        targetPoint = new Vector2(transform.position.x, transform.position.y + 30);
        moveSpeed = 1;
    }
}
