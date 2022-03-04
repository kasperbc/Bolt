using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookBehaviour : MonoBehaviour
{
    // Hook movement
    private float defaultSpeed;  // How fast the hook should move on default
    public float speed;          // How fast the hook travels
    private bool outOfRope;      // Has the hook traveled it's maximum distance
    public float hookDistance;   // How far the hook is able to travel
    public Vector2 firePoint;    // Where the hook was fired
    public bool noDamp;          // Does the hook have damping?

    // Hook grab
    private bool hooked;              // Has the hook grabbed onto something?
    private GameObject hookObj;       // What object has the hook grabbed onto?

    // Hook origin
    public Transform origin;     // The origin of the hook
    public TargetJoint2D joint;  // The joint that the player is using to pull itself towards the hook
    public GameObject rope;      // The rope that the hook is attached to

    // Components
    private Rigidbody2D rb;     // The rigidbody component of the hook
    void Start()
    {
        // Set the default value of the hook speed
        if (speed == 0)
        {
            speed = 35;
        }
        // Set the default value of the hook distance
        if (hookDistance == 0)
        {
            hookDistance = 13;
        }

        // Assign the hook physics component
        rb = GetComponent<Rigidbody2D>();

        // Assign the default speed of the hook
        defaultSpeed = speed;
    }

    // Update is called once per frame
    void Update()
    {
        float travelDistance = Vector2.Distance(transform.position, origin.position);

        // Check if the grappling hook has traveled the maximum distance
        if (travelDistance > hookDistance || (Vector2.Distance(transform.position, firePoint) > hookDistance * 2 && !hooked))
        {
            // Assign the hook to be at it's maximum distance
            PullBack();
        }

        // Is the hook hooked?
        if (hooked && joint != null && !noDamp)
        {
            // If yes, increase the damping ratio of the joint the closer you are to the player
            joint.dampingRatio = (hookDistance - travelDistance) / (hookDistance * 2);
        }

        // Does the player joint exist when hooked onto something?
        if (joint == null && hooked)
        {
            // If not, pull back the hook
            PullBack();
        }

        // Check if the hook has grabbed onto a moving object
        if (transform.parent != null)
        {
            joint.target = transform.position;
        }

        rope.transform.localScale = new Vector2(1, (hookDistance - travelDistance / 3) / hookDistance);

        // Does the hook have damp?
        if (noDamp && joint != null)
        {
            joint.dampingRatio = 0;
        }
    }

    void FixedUpdate()
    {
        // Has the grappling hook grabbed onto something?
        if (hooked)
        {
            // If yes, don't execute the movement scripts
            return;
        }

        // Has the grappling hook traveled it's maximum distance?
        if (!outOfRope)
        {
            // If no, keep moving forward
            MoveForward();
        }
        else
        {
            // If yes, move back to the origin
            ReverseDir();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Is the hook being affected by gravity?
        if (rb.gravityScale != 0)
        {
            PullBack();
        }

        // Is the hook being pulled back?
        if (outOfRope)
            // If yes, don't grab onto anything
            return;

        // Get the ground layer mask
        LayerMask ground = LayerMask.NameToLayer("Ground");

        // Has the grappling hook hit the ground/wall?
        if (collision.gameObject.layer == ground)
        {
            // If yes, declare the hook hooked
            hooked = true;
            hookObj = collision.gameObject;

            SoundManager.instance.PlaySound("hookhit");

            // Get the ground object and collider
            GameObject groundObject = collision.gameObject;
            Collider2D groundCollider = groundObject.GetComponent<Collider2D>();
            // Get the position to grab onto in the wall
            Vector2 grabPosition = groundCollider.ClosestPoint(transform.position);

            // Rotate the hook to the grab position
            transform.rotation = CalculateAngle(grabPosition);

            // Failsafe rotation
            if (transform.rotation.z == 0)
            {
                transform.rotation = CalculateAngle(firePoint);
            }

            // Position the hook to the wall
            transform.position = grabPosition;

            // Pull the player towards the hook
            PlayerMovement originMovement = origin.gameObject.GetComponent<PlayerMovement>();
            originMovement.PullTowardsHook(gameObject);

            // Play the particle effect
            GetComponent<ParticleSystem>().Play();
        }
    }

    Quaternion CalculateAngle(Vector2 targetPosition)
    {
        // Get the direction to the grab position
        Vector2 direction = targetPosition - (Vector2)transform.position;
        // Get the angle to the grab position
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // Return the angle
        return Quaternion.AngleAxis(angle, Vector3.forward);
    }

    /// <summary>
    /// Moves the hook forwards.
    /// </summary>
    void MoveForward()
    {
        // Move forward
        transform.Translate(Vector2.right * speed * Time.fixedDeltaTime, Space.Self);
    }

    /// <summary>
    /// Moves the hook towards the origin.
    /// </summary>
    void ReverseDir()
    {
        // Move towards the origin position
        transform.position = Vector2.MoveTowards(transform.position, origin.position, speed * Time.fixedDeltaTime);

        // Check if the hook has reached the rope
        if (Vector2.Distance(transform.position, origin.position) < 0.25f)
        {
            // If yes, despawn the hook and rope as they are no longer needed
            DestroyHook();
        }
    }

    /// <summary>
    /// Pull the hook back to the player
    /// </summary>
    public void PullBack()
    {
        outOfRope = true;
        hooked = false;
        rb.velocity = Vector2.zero;
        transform.parent = null;
        speed = defaultSpeed;
        rb.gravityScale = 0;
        Destroy(joint);
    }

    /// <summary>
    /// Despawns the hook and attached components
    /// </summary>
    private void DestroyHook()
    {
        Destroy(rope);
        Destroy(gameObject);
    }

    /// <summary>
    /// Is the hook able to grab onto anything?
    /// </summary>
    /// <returns></returns>
    public bool IsAbleToGrab()
    {
        return !(outOfRope || hooked);
    }

    /// <summary>
    /// Is the hook being pulled back?
    /// </summary>
    /// <returns></returns>
    public bool IsBeingPulledBack()
    {
        return outOfRope;
    }

    /// <summary>
    /// Returns what GameObject the hook is hooked onto. If not hooked, returns null.
    /// </summary>
    /// <returns></returns>
    public GameObject GetHookedObject()
    {
        if (!hooked)
        {
            return null;
        }
        return hookObj;
    }

    /// <summary>
    /// Drops the hook to the ground and pulls it back once it reaches the floor
    /// </summary>
    public void EnableHookGravity()
    {
        SoundManager.instance.PlaySound("cling");

        // Enable gravity on the hook
        rb.gravityScale = 2;

        // Decrease the hooks speed to 0
        // to prevent it from moving forward further
        speed = 0;
    }
}
