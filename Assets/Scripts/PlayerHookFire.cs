using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHookFire : MonoBehaviour
{
    Vector2 mousePos;           // The position of the mouse

    [SerializeField]
    private GameObject hook;    // The hook of the grappling hook
    [SerializeField]
    private GameObject rope;    // The rope of the grappling hook

    private PlayerMovement pMov;    // The player movement script
    private SpriteRenderer rend;    // The renderer component

    private float xPos;         // The X position of the aim
    private SpriteRenderer playerRend;  // The player's renderer component
    void Start()
    {
        // Assign components
        pMov = transform.parent.GetComponent<PlayerMovement>();
        rend = GetComponent<SpriteRenderer>();
        playerRend = transform.parent.GetComponent<SpriteRenderer>();

        // Assign the x position of the aim
        xPos = transform.localPosition.x;
    }

    void Update()
    {
        // Execute the mouse aiming script
        MouseAim();

        // Update the sprite based on if the hooks have been fired
        rend.enabled = !(pMov.hooks.Count >= pMov.maxHooks || pMov.IsStunned());

        // Flip the position based on if the player is flipped
        if (playerRend.flipX)
            transform.localPosition = new Vector2(-xPos, transform.localPosition.y);
        else
            transform.localPosition = new Vector2(xPos, transform.localPosition.y);
        
    }

    /// <summary>
    /// Move the aim towards the mouse.
    /// </summary>
    void MouseAim()
    {
        // Update the position of the mouse
        mousePos = Mouse.current.position.ReadValue();

        // Get the direction towards the mouse position
        Vector2 direction = mousePos - (Vector2)Camera.main.WorldToScreenPoint(transform.position);
        // Calculate the angle to the mouse position
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Is the player on the ground and is the angle downwards?
        if (angle < 0 && pMov.IsOnGround())
        {
            // If yes, limit the angles that the aim can be in
            if (angle > -90)
                angle = 0;
            if (angle < -90)
                angle = 180;
        }
        // Rotate with the angle towards the mouse position
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    /// <summary>
    /// Fire the grappling hook.
    /// </summary>
    public void FireHook(float distance, Vector2 velocity)
    {
        // Disable nearby drones hookability for a short while
        DisableNearestDrone();

        // Create the hook object
        GameObject spawnedHook = Instantiate(hook, transform.position, transform.rotation);
        // Create the rope object
        GameObject spawnedRope = Instantiate(rope);

        // Get the behaviour script of the rope
        RopeBehaviour ropeBeh = spawnedRope.GetComponent<RopeBehaviour>();
        // Get the behaviour script of the hook
        HookBehaviour hookBeh = spawnedHook.GetComponent<HookBehaviour>();

        // Assign the first point of the rope to the player
        ropeBeh.points[0] = transform.parent;
        // Assign the second point of the rope to the player
        ropeBeh.points[1] = spawnedHook.transform;

        // Change the rope color based on how far it has traveled
        float ropeColor = 0.5f + distance / 26;
        spawnedRope.GetComponent<SpriteRenderer>().color = new Color(ropeColor, ropeColor, ropeColor);

        // Assign the origin point of the hook
        hookBeh.origin = transform.parent;
        // Assign the point that the hook was fired at
        hookBeh.firePoint = transform.position;
        // Assign the rope to the hook
        hookBeh.rope = spawnedRope;
        // Assign the distance to the hook
        hookBeh.hookDistance = distance;
        // Set the hook speed
        hookBeh.speed = 35 + Mathf.Abs(velocity.x);
        pMov.hooks.Add(spawnedHook);

        

        print(hookBeh.speed);
    }

    public void FireHook()
    {
        FireHook(14, Vector2.zero);
    }

    /// <summary>
    /// Set every nearby drone to be unhookable.
    /// </summary>
    /// <returns></returns>
    private void DisableNearestDrone()
    {
        // Get every nearby drone
        GameObject nearestDrone = GameObject.FindWithTag("Drone");
        foreach(GameObject drone in GameObject.FindGameObjectsWithTag("Drone"))
        {
            // Is the drone close to the player?
            if (Vector2.Distance(transform.position, drone.transform.position) < Vector2.Distance(transform.position, nearestDrone.transform.position))
            {
                // If yes, add drone to nearby drones
                nearestDrone = drone;
            }
        }

        // Is the nearest drone too far away or non-existant?
        if (nearestDrone == null || Vector2.Distance(transform.position, nearestDrone.transform.position) > 1)
        {
            // If yes, dont try to disable the nearest drone
            return;
        }

        // Disable the nearest drones collider
        nearestDrone.GetComponent<Collider2D>().enabled = false;

        // Wait for a short while and set the nearest drone to be hookable
        StartCoroutine(EnableNearestDrone(nearestDrone));
    }

    /// <summary>
    /// Set the nearest drone to be hookable.
    /// </summary>
    /// <returns></returns>
    private IEnumerator EnableNearestDrone(GameObject drone)
    {
        yield return new WaitForSeconds(0.5f);

        // Set all nearby drones to be hookable
        drone.GetComponent<Collider2D>().enabled = true;
    }
}
