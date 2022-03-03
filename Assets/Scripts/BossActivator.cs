using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.Rendering.PostProcessing;

public class BossActivator : MonoBehaviour
{
    [SerializeField]
    private bool defeater;                  // Does the activator defeat the boss?
    [SerializeField]
    private Vector2 camPos;                 // Where should the virtual camera move during the cutscene?
    [SerializeField]
    private GameObject[] deletableObjects;  // Which objects should be deleted upon activation?
    [SerializeField]
    private ParticleSystem activateParticle;    // Which particle systems should be activated?
    [SerializeField]
    private string sound;                   // What sound does the activator cutscene make?

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Is the collided object a player?
        if (collision.gameObject.CompareTag("Player"))
        {
            // If yes, activate or defeat the boss drone
            GameObject boss = GameObject.FindWithTag("Boss");

            if (!defeater)
            {
                StartCoroutine(BossCutscene(boss, collision.gameObject));
            }
            else
            {
                camPos = boss.transform.position;
                StartCoroutine(BossCutscene(boss, collision.gameObject));
            }
        }
    }

    private IEnumerator BossCutscene(GameObject boss, GameObject player)
    {
        // Create an empty GameObject for the camera to follow
        GameObject cutScenePoint = Instantiate(new GameObject(), camPos, transform.rotation);

        // Set the camera to the cutscene position and freeze the player
        CinemachineVirtualCamera cam = GameObject.FindGameObjectWithTag("VirtualCamera").GetComponent<CinemachineVirtualCamera>();
        Transform prevFollow = cam.Follow;
        cam.Follow = cutScenePoint.transform;
        PlayerMovement pMov = player.GetComponent<PlayerMovement>();
        pMov.SetCutscene(true);

        // Deactivate the boss if it's active
        if (boss.GetComponent<BossDrone>().IsActivated())
        {
            boss.GetComponent<BossDrone>().DeActivate();
        }

        // Delete deletable objects
        if (deletableObjects.Length > 0)
        {
            foreach (GameObject gObject in deletableObjects)
            {
                Destroy(gObject);
            }
        }

        // Post processing effects
        Vignette vingetteLayer = null;
        PostProcessVolume v = GameObject.Find("Post Processing").GetComponent<PostProcessVolume>();
        v.profile.TryGetSettings(out vingetteLayer);
        vingetteLayer.intensity.value = 0.5f;

        // Wait for 1 second
        yield return new WaitForSeconds(1);

        SoundManager.instance.StopAllSounds();

        // Activate/Destroy the boss
        if (!defeater)
        {
            boss.transform.position = camPos;
            boss.GetComponent<BossDrone>().Activate();
            SoundManager.instance.PlaySound("boss", 1, 1, true, true);
        }
        else
        {
            Rigidbody2D bossRb = boss.AddComponent<Rigidbody2D>();
            bossRb.AddForce(new Vector2(-0.5f, 1) * 5, ForceMode2D.Impulse);
        }

        SoundManager.instance.PlaySound(sound);

        // Play activated particle system
        if (activateParticle != null)
            activateParticle.Play();

        // Trigger a screen shake
        GameManager.instance.ScreenShake();
        
        // Wait for 2 seconds
        yield return new WaitForSeconds(2);

        // Unfreeze the player
        pMov.SetCutscene(false);
        cam.Follow = prevFollow;

        // Post processing
        vingetteLayer.intensity.value = 0.3f;

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawSphere(camPos, 1);
    }
}
