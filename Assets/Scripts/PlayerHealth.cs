using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    // Health
    private float pizzaHealth;        // How much health the pizza currently has
    [SerializeField]
    private float maxPizzaHealth;            // The starting health of the pizza
    [SerializeField]
    private float pizzaDamageFrequency;      // How fast the pizza takes damage
    private float damageTime;                // How much time has passed since the pizza has taken damage
    private bool pizzaStolen;                // Has the player's pizza been stolen?
    private bool dead;                       // Is the player "dead"?

    // Components
    private PlayerMovement pMov;                         // The player's movement script
    private Animator anim;                               // The player's animator script
    void Start()
    {
        // Set the pizza health to the maximum
        if (!GameManager.instance.IsLevelReloaded())
            pizzaHealth = maxPizzaHealth;

        // Set the player's movement component
        pMov = GetComponent<PlayerMovement>();
        // Set the animator component
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        // Has the player fallen out of the stage?
        if (transform.position.y < -15 && !dead)
        {
            // If yes, place the player at the last checkpoint
            dead = true;
            RestartAtCheckpoint();
        }

        anim.SetBool("Pizzaless", pizzaStolen);
    }

    /// <summary>
    /// Resets the level and places the player at the last checkpoint.
    /// </summary>
    void RestartAtCheckpoint()
    {
        // Set the player to be non-movable
        pMov.SetCutscene(true);

        // Reload the level
        GameManager.instance.ReloadLevel();
    }

    /// <summary>
    /// Sets the pizza to be stolen or unstolen.
    /// </summary>
    /// <param name="value"></param>
    public void SetPizzaStolen(bool value)
    {
        pizzaStolen = value;
    }

    /// <summary>
    /// Returns whether or not the player has the pizza.
    /// </summary>
    /// <returns></returns>
    public bool GetPizzaStatus()
    {
        return pizzaStolen;
    }
}
