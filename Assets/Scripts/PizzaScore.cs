using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PizzaScore : MonoBehaviour
{
    public float bestTime;      // What is the best expected time to beat the level
    public float time;          // The time in which the player beat the level
    public int reboots;         // The amount of reboots
    [SerializeField]
    private Sprite[] sprites = new Sprite[8];   // The pizza slice sprites

    // Update is called once per frame
    public void UpdateScore()
    {
        int score = 8;

        time *= reboots / 10 + 1;
        for(int i = 1; i <= 8; i++)
        {
            if (bestTime * i > time)
            {
                break;
            }
            score--;
        }

        if (score < 1)
        {
            score = 1;
        }

        print(score);

        GetComponent<Image>().sprite = sprites[score - 1];
    }
}
