using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThiefDrone : MonoBehaviour
{
    [SerializeField]
    private float maxSpeed;            // The maximum speed of the drone
    [SerializeField]
    private float detectionRange;      // How far the drone can detect a player
    [SerializeField]
    private Vector2 detectionCentre;   // Where the centre of the detection range is
    private Vector2 spawnPoint;        // Where the drone was spawned
    private Vector2 currSpeed;         // The current speed of the drone
    [SerializeField]
    private bool smart;                // Does the drone have smarter AI? (Predicts player movement, flies above the player)
    [SerializeField]
    private bool disguised;            // Is the drone disguised as a blue drone?
    private bool hasPizza;             // Has the drone captured the pizza?
    private float pizzaCaptureTime;    // When was the pizza captured?
    private bool pizzaCaptured;        // Has the drone kept the pizza for long enough?

    private Collider2D coll;           // The drone's collider
    private Animator anim;             // The drone's animation component
    void Start()
    {
        // Set components
        coll = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();

        spawnPoint = transform.position;

        // Set the disguised state of the drone
        anim.SetBool("Disguised", disguised);
    }

    // Update is called once per frame
    void Update()
    {
        //
        // Movement
        //

        // Find if there are any players in the detection range
        GameObject playerInRange = FindObjectInRange("Player");

        if (playerInRange != null && playerInRange.GetComponent<PlayerHealth>().GetPizzaStatus() == true)
        {
            playerInRange = null;
        }

        // Has the drone captured the pizza?
        if (pizzaCaptured)
        {
            CapturePizza();
        }
        // Does the drone have the pizza?
        else if (hasPizza)
        {
            // If yes, move away from the player or towards the spawn point
            if (playerInRange != null && Vector2.Distance(detectionCentre, transform.position) < detectionRange)
            {
                GameObject targetInRange = playerInRange;

                // Does the drone have smart AI?
                if (smart)
                {
                    // If yes, move away from any hooks
                    GameObject hookInRange = FindObjectInRange("Hook");
                    if (hookInRange != null)
                    {
                        targetInRange = hookInRange;
                    }
                }

                MoveTowardsTarget(targetInRange.transform.position, true);
            }  
            else
            {
                MoveTowardsTarget(spawnPoint);
            }


            // Has it been more than 8 seconds since the pizza was captured?
            if (Time.timeSinceLevelLoad >= pizzaCaptureTime)
            {
                pizzaCaptured = true;
            }
        }
        // If not, is there a player in the detection range
        else if (playerInRange != null)
        {
            if (disguised)
                GetComponent<ParticleSystem>().Play();
            disguised = false;
            anim.SetBool("Disguised", false);

            // Try to predict the player's movement using hooks if smart AI is enabled
            GameObject targetInRange = playerInRange;
            bool rev = false;

            // Does the drone have smart AI?
            if (smart)
            {
                // If yes, move away from any hooks
                GameObject hookInRange = FindObjectInRange("Hook");
                if (hookInRange != null)
                {
                    targetInRange = hookInRange;
                }
            }

            // Move towards the target
            MoveTowardsTarget(targetInRange.transform.position, rev);
        }
        // If not, move towards the original spawn point
        else
        {
            MoveTowardsTarget(spawnPoint);
        }

        //
        // Attached hooks
        //

        // Does the drone have anything attached to it?
        if (transform.childCount > 0)
        {
            // If yes, go through everything attached to the drone
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                // Is the attached object a hook?
                if (child.CompareTag("Hook"))
                {
                    // If yes, set the hook to pull at maximum speed always
                    HookBehaviour hook = child.gameObject.GetComponent<HookBehaviour>();
                    hook.noDamp = true;
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Did the collision happen with a player?
        if (collision.gameObject.CompareTag("Player"))
        {
            // If yes, steal/return the player's pizza
            PlayerHealth pHealth = collision.gameObject.GetComponent<PlayerHealth>();
            PlayerMovement pMov = collision.gameObject.GetComponent<PlayerMovement>();


            // Is the pizza already stolen by someone else?
            if (pHealth.GetPizzaStatus() == true && !hasPizza)
            {
                // If yes, don't steal the pizza again
                return;
            }

            hasPizza = !hasPizza;
            pHealth.SetPizzaStolen(hasPizza);

            // Did the drone steal the player's pizza?
            if (hasPizza)
            {
                // If yes, set the pizza steal time
                pizzaCaptureTime = Time.timeSinceLevelLoad + 8;

                SoundManager.instance.PlaySound("steal");

                // Increase the max speed
                maxSpeed *= 1.5f;

                // Increase the hitbox size
                GetComponent<BoxCollider2D>().size *= 2;
            }
            else
            {
                // If not, decrease the max speed back to normal
                maxSpeed /= 1.5f;

                SoundManager.instance.PlaySound("unsteal");

                // Decrease the hitbox size
                GetComponent<BoxCollider2D>().size /= 2;
            }

            // Make the drone invincible for a short while.
            StartCoroutine(Invincibility());

            // Set the animation of the drone
            anim.SetBool("Pizza", hasPizza);

            // Has the player fired a hook?
            if (pMov.hooks.Count > 0)
            {
                // If yes, pull back all hooks
                pMov.PullBackAllHooks();
            }
        }
        // Is the collided object a hook?
        else if (collision.gameObject.CompareTag("Hook"))
        {
            // If yes, attach the hook to the drone
            HookBehaviour hook = collision.gameObject.GetComponent<HookBehaviour>();

            // Is the hook being pulled back?
            if (hook.IsBeingPulledBack())
            {
                // If yes, don't do anything
                return;
            }

            // Position the hook to the center of the drone
            hook.transform.position = coll.bounds.center;

            // Hide the grappling hook
            hook.GetComponent<SpriteRenderer>().enabled = false;

            // Set the hook to be the drone's parent
            hook.transform.SetParent(transform);
        }
    }

    /// <summary>
    /// Finds the nearest object with the specified tag and checks if it's in the detection radius. Returns null if none are found.
    /// </summary>
    /// <returns></returns>
    private GameObject FindObjectInRange(string tag, bool returnOutOfRange)
    {
        // Get all the specified objects
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);

        // Return nothing if no objects are found
        if (objects.Length == 0)
            return null;

        // Find the closest objects
        GameObject closestObject = objects[0];
        foreach (GameObject obj in objects)
        {
            if (Vector2.Distance(obj.transform.position, transform.position) <= Vector2.Distance(closestObject.transform.position, transform.position))
            {
                closestObject = obj;
            }
        }

        // Is the drone in disguise mode?
        float detRange = detectionRange;
        if (disguised)
        {
            // If yes, shorten the detection range by half
            detRange /= 2f;
        }

        // Check if the closest object is in the detection range
        if (Vector2.Distance(detectionCentre, closestObject.transform.position) <= detRange || returnOutOfRange)
            return closestObject;

        // Return nothing if no objects are in the detection range if appropriate
        return null;
    }

    private GameObject FindObjectInRange(string tag)
    {
        return FindObjectInRange(tag, false);
    }

    /// <summary>
    /// Moves the drone towards a specified target in a drone-like way.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="reverse"></param>
    void MoveTowardsTarget(Vector2 target, bool reverse)
    {
        // Is the drone inside a wall?
        if (WallCheck())
        {
            // If yes, ignore the current target and go towards the original spawn.
            target = spawnPoint;
            reverse = false;
        }

        Vector2 myPos = transform.position;

        // Calculate the direction of the target
        Vector2 direction = (target - myPos).normalized;

        // Change the direction of the drone to the movement direction
        GetComponent<SpriteRenderer>().flipX = direction.x >= 0;

        // Reverse the direction if appropriate
        if (reverse)
            direction = -direction;

        // Set the maximum speed based on the distance to the target
        float mSpeed = maxSpeed;
        if (Vector2.Distance(myPos, target) < maxSpeed / 2 && !reverse)
            mSpeed = Vector2.Distance(myPos, target);

        // Direct the current direction towards the desired direction
        currSpeed.x = Mathf.MoveTowards(currSpeed.x, direction.x * mSpeed, mSpeed / 100);
        currSpeed.y = Mathf.MoveTowards(currSpeed.y, direction.y * mSpeed, mSpeed / 100);

        // Move the drone
        transform.Translate(currSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Checks if the drone is inside a wall.
    /// </summary>
    /// <returns></returns>
    private bool WallCheck()
    {
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.layerMask = LayerMask.NameToLayer("Ground");
        Collider2D[] res = new Collider2D[1];
        coll.OverlapCollider(contactFilter, res);
        if (res[0] != null)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Flies the drone above the stage and resets the level.
    /// </summary>
    void CapturePizza()
    {
        // Disable the drone's collider
        coll.enabled = false;
        
        // Set the maximum speed to a high value
        maxSpeed = 20;

        // Move above the stage
        MoveTowardsTarget(new Vector2(transform.position.x, spawnPoint.y + 30));

        // Check if the drone is high enough to reset the level
        if (transform.position.y >= spawnPoint.y + 29)
        {
            GameManager.instance.ReloadLevel();
        }
    }

    void MoveTowardsTarget(Vector2 target)
    {
        MoveTowardsTarget(target, false);
    }

    /// <summary>
    /// Makes the drone invincible for a short while.
    /// </summary>
    /// <returns></returns>
    private IEnumerator Invincibility()
    {
        coll.enabled = false;

        yield return new WaitForSeconds(1f);

        coll.enabled = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(detectionCentre, detectionRange);
    }
}
