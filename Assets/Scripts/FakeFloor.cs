using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakeFloor : MonoBehaviour
{
    public bool stopMusic;      // Does the floor stop all sounds?

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Is the collided object a player?
        if (collision.gameObject.CompareTag("Player"))
        {
            // Stun the player for a short while
            PlayerMovement pBeh = collision.gameObject.GetComponent<PlayerMovement>();
            pBeh.StunPlayer(0.5f);

            pBeh.PullBackAllHooks();
        }
        else
        {
            return;
        }

        // Trigger a screen shake
        GameManager.instance.ScreenShake();

        if (stopMusic)
        {
            SoundManager.instance.StopAllSounds();
            SoundManager.instance.PlaySound("bossalarm", 1, 1, true, true);
        }
        SoundManager.instance.PlaySound("boom");

        // Disable the floor
        GetComponent<Collider2D>().enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<ParticleSystem>().Play();
    }
}
