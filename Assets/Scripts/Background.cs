using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Background : MonoBehaviour
{
    public float levelLength, levelHeight;
    public float parralaxStrength, heightStrength;

    // Update is called once per frame
    void Update()
    {
        Vector3 parPos = new Vector3((transform.position.x - levelLength / 2) / levelLength * 2 * parralaxStrength,
            -transform.position.y * heightStrength / levelHeight, 10);
        transform.localPosition = parPos;
    }
}
