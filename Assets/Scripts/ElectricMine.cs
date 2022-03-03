using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricMine : MonoBehaviour
{
    private Animator anim;             // The mine's animator component
    private bool charged;              // Is the mine charged?
    public GameObject boltObject;      // The electric bolt object
    public Sprite boltImage;           // The electric bolt sprite

    // Movement
    [SerializeField]
    private bool moves;                // Does the mine move?
    [SerializeField]
    private float moveSpeed;           // The mines move speed
    [SerializeField]
    private Vector2[] points;           // The points that the mine travels to
    private int targetPoint;            // What point the mine is currently moving towards
    void Start()
    {
        // Attach the mine's animator component
        anim = GetComponent<Animator>();

        // Set the mine to be charged
        charged = true;

        targetPoint = 0;
    }

    private void FixedUpdate()
    {
        // Is the mine supposed to move?
        if (moves)
        {
            // If yes, move the mine towards the target point
            MoveTowardsTarget();
        }
    }

    void MoveTowardsTarget()
    {
        // The current position of the mine
        Vector2 currentPos = transform.position;

        // Is the mine at the target?
        if (currentPos == points[targetPoint])
        {
            // If yes, change the target to the next point
            ChangeTarget();
        }

        // Move the mine towards the target
        transform.position = Vector2.MoveTowards(currentPos, points[targetPoint], moveSpeed * Time.fixedDeltaTime);
    }

    /// <summary>
    /// Changes the mine's target position to the next target.
    /// </summary>
    void ChangeTarget()
    {
        targetPoint++;
        if (targetPoint >= points.Length)
        {
            targetPoint = 0;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Is the mine charged?
        if (!charged)
        {
            // If not, don't do anything
            return;
        }

        // The relevant player's behaviour script
        PlayerMovement playerBeh = null;

        // Is the collided object a hook?
        if (collision.CompareTag("Hook"))
        {
            // If yes, get the behaviour script of the hook
            HookBehaviour hookBeh = collision.GetComponent<HookBehaviour>();

            // Drop the hook to the ground
            hookBeh.EnableHookGravity();

            // Get the movement script from the player that fired the hook
            playerBeh = hookBeh.origin.gameObject.GetComponent<PlayerMovement>();
        }
        // Is the collided object a player?
        else if (collision.CompareTag("Player"))
        {
            // If yes, get the movement script from the player
            playerBeh = collision.gameObject.GetComponent<PlayerMovement>();
        }
        else
        {
            return;
        }

        // Is the player already stunned?
        if (playerBeh.IsStunned())
        {
            // If yes, don't stun the player again
            return;
        }

        // Pull back every hook attached to the player
        playerBeh.PullBackAllHooks();

        // Stun the player for a short while
        StartCoroutine(playerBeh.StunPlayer(3));

        SoundManager.instance.PlaySound("shock");

        // Is the player to the side of the mine?
        Vector3 playerPos = playerBeh.transform.position;
        if (Mathf.Abs(playerPos.x - transform.position.x) > Mathf.Abs(playerPos.y - transform.position.y))
        {
            // If yes, add knockback to the player
            playerBeh.KnockBack(new Vector2(playerPos.x - transform.position.x, 0).normalized, 10);
        }

        // Create an electric bolt
        GameObject bolt = Instantiate(boltObject);

        // Get the behaviour script from the created bolt
        RopeBehaviour boltBeh = bolt.GetComponent<RopeBehaviour>();

        // Attach the first part of the rope to the mine
        boltBeh.points[0] = transform;
        // Attach the second part of the rope to the player
        boltBeh.points[1] = playerBeh.transform;
        // Make the bolt slowly disappear
        boltBeh.disappear = true;
        // Make the bolts sprite the bolt
        bolt.GetComponent<SpriteRenderer>().sprite = boltImage;
        // Set the bolts sprite width
        boltBeh.spriteWidth = 0.5f;

        // Charge and recharge the mine
        StartCoroutine(RechargeMine(2));
    }

    /// <summary>
    /// Temporarily stop the mine's electricity.
    /// </summary>
    /// <returns></returns>
    private IEnumerator RechargeMine(float duration)
    {
        // Set the mine to be uncharged
        charged = false;

        // Update the animation accordingly
        anim.SetBool("Electric", false);

        yield return new WaitForSeconds(duration);

        // Set the mine to be charged
        charged = true;

        // Update the animation accordingly
        anim.SetBool("Electric", true);
    }
}
