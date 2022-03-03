using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class BossDrone : MonoBehaviour
{
    [SerializeField]
    private bool active;                // Is the boss active?
    [SerializeField]
    private float moveSpeed;            // How fast the drone moves
    private float defaultMoveSpeed;     // The default move speed of the drone
    [SerializeField]
    private Vector2[] travelPoints;     // The points that the drone travels to
    private int currentPoint;           // The current travel point that the drone is moving towards
    [SerializeField]
    private float detectionRadius;      // The radius at which the drone can detect the player
    private bool hasPlayer;             // Has the drone caught a player?
    public Sprite sprite;               // The boss drone's sprite
    private SpriteRenderer rend;        // The drone's sprite renderer
    [SerializeField]
    private Vector2 playerGrabPos;      // Where the drone will grab the player

    void Start()
    {
        defaultMoveSpeed = moveSpeed;

        rend = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (!active)
        {
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        float disToPlayer = Vector2.Distance(transform.position, GameObject.FindWithTag("Player").transform.position);

        // Is the player within the drone detection radius?
        if (disToPlayer > detectionRadius)
        {
            // If no, move towards the next target point
            MoveTowardsTargetPoint();
        }
        else if (!hasPlayer)
        {
            // If yes, move towards the player
            MoveTowardsPlayer(player.transform);
        }
        else
        {
            // Does the drone have the player?
            MoveTowardsTargetPoint();
            transform.GetChild(1).localPosition = playerGrabPos;
        }
    }

    /// <summary>
    /// Moves the drone towards the current travel point.
    /// </summary>
    void MoveTowardsTargetPoint()
    {
        // Set the current travel point to the next point if the drone has reached the current point
        if (transform.position.Equals(travelPoints[currentPoint]) || transform.position.x > travelPoints[currentPoint].x)
        {
            currentPoint++;

            // Rubber band the drone if it takes more than 5 seconds to reach the player
            GameObject player = GameObject.FindWithTag("Player");
            float disToPlayer = Vector2.Distance(transform.position, player.transform.position);
            if (disToPlayer > moveSpeed * 5)
            {
                moveSpeed = disToPlayer / 5;
            }
            else
            {
                moveSpeed = defaultMoveSpeed;
            }

            // Restart the level if the drone has the player
            if (hasPlayer)
            {
                moveSpeed = 0;
                GameManager.instance.ReloadLevel();
            }

            print("Travel point (" + currentPoint + ") reached. (Distance to player: " + disToPlayer + ", Speed: " + moveSpeed + ")");
        }

        // Increase the movement speed cinematically if drone has player
        if (hasPlayer)
        {
            moveSpeed *= 1.015f;
            transform.GetChild(0).localPosition = playerGrabPos;
        }
            

        // Move the drone towards the travel point
        transform.position = Vector2.MoveTowards(transform.position, travelPoints[currentPoint], moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Moves the drone towards the player
    /// </summary>
    void MoveTowardsPlayer(Transform player)
    {
        moveSpeed = defaultMoveSpeed;

        transform.position = Vector2.MoveTowards(transform.position, player.transform.position, moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Attaches the player to the drone and disables all movement
    /// </summary>
    /// <param name="player"></param>
    void GetPlayer(GameObject player)
    {
        // Set the player to grab position
        player.transform.SetParent(transform);
        player.transform.localPosition = playerGrabPos;

        // Disable player movement
        PlayerMovement playerBeh = player.GetComponent<PlayerMovement>();
        playerBeh.SetCutscene(true);
        playerBeh.StunPlayer(100);

        // Freeze the camera
        CinemachineVirtualCamera cam = GameObject.FindWithTag("VirtualCamera").GetComponent<CinemachineVirtualCamera>();
        GameObject camSpot = Instantiate(new GameObject(), transform.position, transform.rotation);
        cam.Follow = camSpot.transform;

        // Stop all player forces and disable collider
        Rigidbody2D plRb = player.GetComponent<Rigidbody2D>();
        plRb.gravityScale = 0;
        player.GetComponent<Collider2D>().enabled = false;

        // Set the drone to have the player
        hasPlayer = true;

        // Set the target to 30 meters upwards
        travelPoints[0] = new Vector2(transform.position.x, transform.position.y + 30);
        currentPoint = 0;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Is the drone active?
        if (!active)
        {
            // If not, don't do anything
            return;
        }

        // Is the collided object a player?
        if (collision.gameObject.CompareTag("Player"))
        {
            GetPlayer(collision.gameObject);
        }
    }

    public void Activate()
    {
        // Create a "hole" of self in the wall
        GameObject hole = Instantiate(new GameObject(), transform.position, transform.rotation);
        SpriteRenderer holerend = hole.AddComponent<SpriteRenderer>();
        holerend.color = Color.black;
        holerend.sortingOrder = 1;
        holerend.sprite = sprite;
        hole.transform.localScale = transform.localScale;


        // Set the drone to be active
        active = true;
    }

    public void DeActivate()
    {
        // Stop the drone
        moveSpeed = 0;

        // Stop the drone animation
        Destroy(GetComponent<Animator>());

        // Set the drone to the deactivated sprite
        rend.sprite = sprite;

        // Set the drone to be deactive
        active = false;
    }

    public bool IsActivated()
    {
        return active;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (travelPoints.Length > 0)
        {
            for (int i = 0; i < travelPoints.Length - 1; i++)
            {
                Gizmos.DrawLine(travelPoints[i], travelPoints[i + 1]);
            }
        }

        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
