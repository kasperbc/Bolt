using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeBehaviour : MonoBehaviour
{
    public Transform[] points = new Transform[2];     // The gameobjects that the rope is attached to
    private SpriteRenderer sr;      // The sprite renderer of the rope
    public bool disappear;          // Should the rope disappear slowly?
    public float spriteWidth;       // How wide should the sprite be?

    void Start()
    {
        // Assign the sprite renderer
        sr = GetComponent<SpriteRenderer>();

        if (spriteWidth == 0)
        {
            spriteWidth = 1;
        }
    }

    void Update()
    {
        // Get the center of the two points
        transform.position = Vector3.Lerp(points[0].position, points[1].position, 0.5f);

        // Stretch the rope to the approriate distance
        sr.size = new Vector2(Vector2.Distance(points[0].position, points[1].position), spriteWidth);

        // Get the direction towards the target
        Vector2 direction = points[1].position - transform.position;
        // Calculate the angle to the target
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // Rotate with the angle towards the target
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void FixedUpdate()
    {
        // Should the bolt disappear slowly?
        if (disappear)
        {
            // If yes, get the rope's color
            Color c = sr.color;
            // Update the rope's color to slightly make it more transparent
            sr.color = new Color(c.r, c.g, c.b, c.a - 0.03f);
        }

        // Is the rope fully invisible?
        if (sr.color.a <= 0)
        {
            // If yes, destroy the rope
            Destroy(gameObject);
        }
    }
}
