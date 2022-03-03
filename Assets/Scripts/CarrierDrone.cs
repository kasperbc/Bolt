using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.SceneManagement;

public class CarrierDrone : MonoBehaviour
{
    // Components
    private Collider2D coll;        // The drone's collider
    private SpriteRenderer sr;      // The drone's sprite renderer
    private Animator anim;          // The drone's animator

    private bool grappled = false;  // Is someone grappled to the drone
    private bool idle = true;       // Is the drone idle/not doing anything

    private Vector2 targetPos;      // The position that the drone is trying to reach
    private Vector2 spawnPos;       // The position that the drone spawned at
    private Vector2 hookDirection;  // The direction that a hook came from

    [SerializeField]
    private bool stationary;        // Is the drone stationary? (Does not have a target)
    [SerializeField]
    private Vector2 movesTowards;   // What position the drone moves towards
    [SerializeField]
    private float moveSpeed;        // How fast the drone moves
    [SerializeField]
    private bool movesUponContact;  // Should the bot only start moving when it has been hooked onto?
    [SerializeField]
    private string loadLevel;       // What level the should the drone load?
    [SerializeField]
    private bool letGoOfHook;       // Does the drone let go of the hook when it's pulled towards the drone?
    private bool moving;            // Is the drone moving towards it's moving target?
    void Start()
    {
        // Assign the drone's collider
        coll = GetComponent<Collider2D>();

        // Assign the drone's sprite renderer
        sr = GetComponent<SpriteRenderer>();

        // Assign the drone's animator
        anim = GetComponent<Animator>();

        // Set the spawn position
        spawnPos = transform.position;

        // Set the target position to be the spawn position
        targetPos = spawnPos;

        // Is the drone stationary?
        if (stationary)
        {
            // If yes, set the bot to target moving towards it's origin point
            movesTowards = spawnPos;
        }
        
        // Does the drone only move upon contact
        if (!movesUponContact && !stationary)
        {
            moving = true;
        }
    }

    void Update()
    {
        // Is the bot idle?
        if (idle)
            // If yes, do the idle animation
            IdleAnimation();
        
        // Has something grabbed onto the drone
        if (grappled)
            // If yes, move the hooks with the drone
            MoveHooksToSelf();

        // Check if the bot is idle
        CheckForIdle();

        // Should the bot be moving?
        if (moving && !stationary)
            // If yes, move towards the target
            MoveTowardsTarget();
    }

    /// <summary>
    /// Moves the drone towards a set target in a linear path.
    /// </summary>
    void MoveTowardsTarget()
    {
        // Calculate the distance, used for slowing down near the destination
        float distance = Vector2.Distance(movesTowards, transform.position);
        distance = Mathf.Clamp(distance * moveSpeed, 0.1f, moveSpeed);

        // Move the drone towards the target
        transform.position = Vector2.MoveTowards(transform.position, movesTowards, distance * Time.deltaTime);

        // Calculate the direction of the drone's target
        Vector2 targetDirection = (Vector2)transform.position - movesTowards;

        // Flip the drone towards the direction of it's target
        sr.flipX = targetDirection.x < 0;

        // Is the drone at the destination and is it a non-level loader?
        if (Vector2.Distance(transform.position, movesTowards) < 0.3f && loadLevel.Equals(""))
        {
            // If yes, change the drone's destination to the start
            StartCoroutine(ChangeDirection());
        }
    }

    /// <summary>
    /// Changes the drone's direction to the previous destination.
    /// </summary>
    /// <returns></returns>
    IEnumerator ChangeDirection()
    {
        // Stop moving the drone
        moving = false;

        // Wait a bit
        yield return new WaitForSeconds(1.5f);

        // Change the destination of the drone to the start
        Vector2 oldTarget = movesTowards;
        movesTowards = spawnPos;
        spawnPos = oldTarget;

        // Start moving the drone again
        moving = true;
    }

    /// <summary>
    /// Check if the drone should be idle
    /// </summary>
    void CheckForIdle()
    {
        // Is the drone doing anything?
        if (!grappled && !moving && (movesUponContact || stationary))
        {
            // If not, set the drone to be idle
            idle = true;
        }
        else
        {
            // If yes, set the drone to be not idle
            idle = false;
        }
    }

    /// <summary>
    /// Moves all the hooks attached to the drone with the drone.
    /// </summary>
    void MoveHooksToSelf()
    {
        // The position that the hooks should be moved to
        Vector2 tPos = coll.bounds.center;
        tPos.y -= 0.25f;

        // Get all the hooks that are attached to the drone
        List<Transform> hooks = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).CompareTag("Hook"))
                hooks.Add(transform.GetChild(i));
        }

        // Does the drone have any hooks attached to it?
        if (!(hooks.Count > 0))
        {
            // If not, set the drone to be ungrappled onto
            grappled = false;
            return;
        }

        // Move all the hooks to drone
        foreach (Transform hook in hooks)
        {
            hook.transform.position = tPos;
            // Pull the hook towards the hook faster if appropriate
            if (letGoOfHook)
            {
                HookBehaviour hBeh = hook.GetComponent<HookBehaviour>();
                hBeh.noDamp = true;

                // Let go of the hook if close enough
                if (Vector2.Distance(transform.position, hBeh.origin.transform.position) < 2)
                {
                    hBeh.origin.GetComponent<PlayerMovement>().PullBackAllHooks();
                    grappled = false;
                    SoundManager.instance.PlaySound("whoosh");
                }
            }
        }
    }

    /// <summary>
    /// Slowly drift up and down from the spawning position.
    /// </summary>
    void IdleAnimation()
    {
        // Round the current position to 2 decimals
        Vector2 currentPos = RoundedPos(transform.position, 2);

        // Is the drone at the target position?
        if (currentPos == targetPos)
        {
            // If yes, assign a new target position

            // Was the current target positon above or below the spawning position and is the bot idle?
            if (targetPos.y >= spawnPos.y)
            {
                // If above, set the new target position to be below
                targetPos = new Vector2(spawnPos.x, spawnPos.y - 0.3f);
            }
            else
            {
                // If below, set the new target position to be above
                targetPos = new Vector2(spawnPos.x, spawnPos.y + 0.3f);
            }
        }

        // Distance to the target position, used for calculating the movement speed
        float disToTarget = Vector2.Distance(transform.position, targetPos) / 50;

        // If the bot is idle, cap the movement speed
        disToTarget = Mathf.Clamp(disToTarget, 0, 5f);

        // Move the drone towards the target position
        transform.position = Vector2.MoveTowards(transform.position, targetPos, disToTarget);
    }

    /// <summary>
    /// Rounds the given Vector2 to 2 decimals
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    Vector2 RoundedPos(Vector2 position, int decimals)
    {
        // Creates a new variable for the rounded position
        Vector2 roundedPos = position;

        // Calculate how much the position should be rounded
        int roundAmount = 10 * decimals;

        // Puts the round amount to a full number if 0 or less decimals were specified
        if (decimals <= 0)
            roundAmount = 1;

        // Round the postion to 2 decimals
        roundedPos.x = Mathf.Round(position.x * roundAmount) / roundAmount;
        roundedPos.y = Mathf.Round(position.y * roundAmount) / roundAmount;

        // Return the rounded position
        return roundedPos;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Is the collided object a hook?
        if (collision.gameObject.CompareTag("Hook"))
        {
            // If yes, assign the hook object for later use
            GameObject hook = collision.gameObject;

            // Get the hook's behaviour script
            HookBehaviour hookBeh = hook.GetComponent<HookBehaviour>();

            // Is the hook grabbed onto the drone?
            if (!hookBeh.GetHookedObject() == gameObject)
            {
                // If no, don't teleport the hook
                return;
            }

            // Load the next level if appropriate
            if (!loadLevel.Equals(""))
            {
                hookBeh.origin.GetComponent<PlayerMovement>().SetCutscene(true);
                StartCoroutine(LoadLevel());
            }

            // Set the direction that the hook came from
            hookDirection = (transform.position - collision.transform.position) * 1.5f;

            // Position the hook to the center of the drone
            hook.transform.position = coll.bounds.center;

            // Hide the grappling hook
            hook.GetComponent<SpriteRenderer>().enabled = false;

            // Set the hook to be the drone's parent
            hook.transform.parent = transform;

            // Has the drone been grappled onto?
            if (!grappled && !moving && idle)
            {
                // If not, recoil to the opposite direction of the hook
                StartCoroutine(DroneRecoil(hookDirection));
            }

            // Play drone sound
            SoundManager.instance.PlaySound("drone");

            if (letGoOfHook)
            {
                hookBeh.origin.GetComponent<Rigidbody2D>().gravityScale = 0;
            }

            // Set the drone to be grappled onto
            grappled = true;
        }
    }

    /// <summary>
    /// Make the drone recoil from a hook
    /// </summary>
    /// <returns></returns>
    private IEnumerator DroneRecoil(Vector2 target)
    {
        // Create a target joint to recoil the drone towards
        TargetJoint2D joint = gameObject.AddComponent<TargetJoint2D>();
        joint.autoConfigureTarget = false;
        joint.target = (Vector2)transform.position + target;
        joint.frequency = 1.5f;
        joint.dampingRatio = 0.75f;

        // Destroy the joint if the drone isnt supposed to move
        if (stationary)
        {
            Destroy(joint);
        }

        // Trigger the drone's happy animation
        anim.SetTrigger("HappyDance");

        // Wait a bit to "readjust"
        yield return new WaitForSeconds(1f);

        // Set the drone to be moving if appropriate
        if (movesUponContact)
        {
            moving = true;
        }

        // Do not move upon contant
        movesUponContact = false;

        // Destroy the target joint
        if (!stationary)
        {
            Destroy(joint);
        }
    }

    /// <summary>
    /// Load the specified level.
    /// </summary>
    /// <returns></returns>
    private IEnumerator LoadLevel()
    {
        // Freeze the camera
        CinemachineVirtualCamera vcam = GameObject.FindGameObjectWithTag("VirtualCamera").GetComponent<CinemachineVirtualCamera>();
        vcam.Follow = null;

        // Wait 5 seconds
        yield return new WaitForSeconds(3);

        // Load the next level
        GameManager.instance.ShowLevelCompletePanel();
    }
}
