using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Cinemachine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;      // Instance of the Game Manager
    private static Vector2 checkPoint;       // What checkpoint the player is at
    private static bool reload;              // Has the level been reloaded?

    private GameObject pausePanel;           // The pause panel
    private bool paused;                     // Is the game paused?

    private GameObject[] virtualCams;        // The Cinemachine virtual camera objects

    private Text timer, gTimer;              // The timer objects
    private static float timerOffset;        // How much the timer is offset (from the last load ect)
    private GameObject levelCompletePanel;   // The level complete panel
    private bool inCompletePanel;            // Is the game on the level complete screen?
    private static int reboots;              // How many times has the player been reloaded in the current level?
    private static float totalTime;          // How much time have you played in the current run?
    void Awake()
    {
        // Set the singleton instance
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // Has the level been reloaded?
        if (reload)
        {
            GameObject player = GameObject.FindWithTag("Player");

            player.transform.position = checkPoint;
            GameObject.Find("LevelName").SetActive(false);

            reload = false;
            reboots++;

            if (GameObject.Find("CutsceneDrone") != null)
                GameObject.Find("CutsceneDrone").SetActive(false);
        }
        else
        {
            timerOffset = 0;
            reboots = 0;

            if (GameObject.Find("CutsceneDrone") != null)
                GameObject.Find("CutsceneDrone").GetComponent<CutsceneDrone>().EnableDrone();
        }

        // Get the pause panel
        GameObject pPanel = GameObject.Find("PauseMenu");
        // Does the pause panel exist?
        if (pPanel != null)
        {
            // If yes, then assign and hide the pause panel
            pausePanel = pPanel;
            pausePanel.SetActive(false);
        }

        // Get the level complete panel and hide it
        GameObject cPanel = GameObject.Find("LevelComplete");
        if (cPanel != null)
        {
            levelCompletePanel = cPanel;
            cPanel.SetActive(false);
        }
        inCompletePanel = false;

        timer = GameObject.Find("Timer").GetComponent<Text>();
        gTimer = GameObject.Find("GameTimer").GetComponent<Text>();

        // Show timer option
        if (!PlayerPrefs.GetInt("ShowTimer").Equals(1))
        {
            timer.color = new Color(0, 0, 0, 0);
            gTimer.color = new Color(0, 0, 0, 0);
        }

        // Get the virtual cameras
        virtualCams = GameObject.FindGameObjectsWithTag("VirtualCamera");

        // Unpause the game
        Time.timeScale = 1;
    }

    // Update is called once per frame
    void Update()
    {
        Keyboard kb = Keyboard.current;
        //Gamepad pad = Gamepad.current;

        if ((kb.escapeKey.wasPressedThisFrame /*|| pad.startButton.wasPressedThisFrame*/) && !inCompletePanel)
        {
            TogglePause();
        }

        UpdateTimer();
    }

    /// <summary>
    /// Updates the level timer.
    /// </summary>
    private void UpdateTimer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player.GetComponent<PlayerMovement>().IsInCutscene())
        {
            timerOffset -= Time.deltaTime;
            return;
        }

        string levelTime = CalculateTime(Time.timeSinceLevelLoad + timerOffset);
        string globalTime = CalculateTime(Time.timeSinceLevelLoad + timerOffset + totalTime);

        timer.text = levelTime;
        gTimer.text = globalTime;
    }

    private string CalculateTime(float time)
    {
        float seconds = (Mathf.Round(time * 100) / 100 % 60);
        float minutes = Mathf.Floor(time / 60);

        string minutePadding = "";
        if (minutes < 10)
        {
            minutePadding = "0";
        }

        string secondPadding = "";
        if (seconds < 10)
        {
            secondPadding = "0";
        }

        return minutePadding + minutes + ":" + secondPadding + seconds.ToString().Replace(",", ".");
    }

    /// <summary>
    /// Returns the player's checkpoint.
    /// </summary>
    /// <returns></returns>
    public Vector2 GetCheckpoint()
    {
        return checkPoint;
    }

    /// <summary>
    /// Updates the player's checkpoint. Should be used by checkpoint objects.
    /// </summary>
    /// <param name="value"></param>
    public void UpdateCheckpoint(Vector2 value)
    {
        checkPoint = value;
    }

    /// <summary>
    /// Returns true if the level has been reloaded this frame.
    /// </summary>
    /// <returns></returns>
    public bool IsLevelReloaded()
    {
        return reload;
    }

    /// <summary>
    /// Reloads the level, keeping all the player values, such as checkpoints.
    /// </summary>
    public void ReloadLevel()
    {
        // Reloads the level
        if (reload == false)
        {
            SoundManager.instance.PlaySound("lose");
        }

        reload = true;
        StartCoroutine(TransitionToLevel(SceneManager.GetActiveScene().name));
    }

    /// <summary>
    /// Restarts the level, resetting all the player values.
    /// </summary>
    public void RestartLevel()
    {
        // Resets the checkpoint
        checkPoint = Vector2.zero;

        SoundManager.instance.StopAllSounds();
        SoundManager.instance.PlaySound("race", 1, 1, true, true);

        StartCoroutine(TransitionToLevel(SceneManager.GetActiveScene().name));
    }

    /// <summary>
    /// Loads the specified level, resetting all the player values.
    /// </summary>
    public void LoadLevel(string levelName)
    {
        // Resets the checkpoint
        checkPoint = Vector2.zero;
        // Loads the level
        reload = false;
        StartCoroutine(TransitionToLevel(levelName));

        // Deals with the timer accordingly
        if (levelName.Equals("Title") || levelName.Equals("Level1"))
            totalTime = 0;
        else
            totalTime += Time.timeSinceLevelLoad + timerOffset;
    }

    /// <summary>
    /// Fade the screen to black and load the next level.
    /// </summary>
    private IEnumerator TransitionToLevel(string levelName)
    {
        if (GameObject.Find("Fade") != null)
        {
            yield return new WaitForSeconds(0.5f);

            GameObject fade = GameObject.Find("Fade");
            Animator fadeAnim = fade.GetComponent<Animator>();
            fadeAnim.SetTrigger("FadeIn");

            yield return new WaitForSeconds(1f);
        }
        
        SoundManager.instance.StopSound("boss");

        timerOffset += Time.timeSinceLevelLoad;
        SceneManager.LoadScene(levelName);
    }

    /// <summary>
    /// Loads a level instantly, without transition, resetting all the player values.
    /// </summary>
    /// <param name="levelName"></param>
    public void LoadLevelInstantly(string levelName)
    {
        // Resets the checkpoint
        checkPoint = Vector2.zero;
        // Loads the level
        reload = false;

        // Deals with the timer accordingly
        if (levelName.Equals("Title") || levelName.Equals("Level1"))
            totalTime = 0;
        else
            totalTime += Time.timeSinceLevelLoad + timerOffset;

        SceneManager.LoadScene(levelName);
    }

    /// <summary>
    /// Restarts the level instantly, without transition, resetting all the player values.
    /// </summary>
    /// <param name="levelName"></param>

    public void RestartLevelInstantly()
    {
        UnPause();

        // Resets the checkpoint
        checkPoint = Vector2.zero;

        SoundManager.instance.StopAllSounds();
        SoundManager.instance.PlaySound("race", 1, 1, true, true);

        totalTime += Time.timeSinceLevelLoad + timerOffset;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Toggle the game's paused state.
    /// </summary>
    public void TogglePause()
    {
        if (paused)
        {
            UnPause();
        }
        else
        {
            Pause();
        }
    }

    /// <summary>
    /// Pause the game and open the pause menu.
    /// </summary>
    public void Pause()
    {
        // Pause the game
        Time.timeScale = 0;
        // Open the pause panel
        pausePanel.SetActive(true);

        paused = true;

        SoundManager.instance.SetGlobalVolume(0.25f);

        // Disable all the annoying scripts
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            player.transform.GetChild(0).GetComponent<PlayerHookFire>().enabled = false;
            player.GetComponent<PlayerMovement>().enabled = false;
        }
    }

    /// <summary>
    /// Unpause the game and close the pause menu.
    /// </summary>
    public void UnPause()
    {
        // Unpause the game
        Time.timeScale = 1;
        // Close the pause panel
        pausePanel.SetActive(false);

        SoundManager.instance.SetGlobalVolume(4);

        paused = false;

        // Enable all the annoying scripts
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            player.transform.GetChild(0).GetComponent<PlayerHookFire>().enabled = true;
            player.GetComponent<PlayerMovement>().enabled = true;
        }
    }

    /// <summary>
    /// Triggers the screen shake effect in all the cameras
    /// </summary>
    /// <returns></returns>
    public void ScreenShake()
    {
        // Return if no cameras on scene or effects are disabled
        if (virtualCams.Length == 0 || !PlayerPrefs.GetInt("Effects").Equals(1)) { return; }

        foreach (GameObject virtualCamera in virtualCams)
        {
            Animator vAnim = virtualCamera.GetComponent<Animator>();
            vAnim.SetTrigger("ScreenShake");
        }
    }

    /// <summary>
    /// Shows the level complete panel
    /// </summary>
    public void ShowLevelCompletePanel()
    {
        // Get the level time
        float time = Time.timeSinceLevelLoad + timerOffset;
        time = Mathf.Round(time * 100) / 100;

        // Play victory sound
        SoundManager.instance.PlaySound("win");

        levelCompletePanel.SetActive(true);

        // Show completion time
        GameObject.Find("Time").GetComponent<Text>().text =
            "Time: " + timer.text;

        // Show reboots
        GameObject.Find("Reboots").GetComponent<Text>().text =
            "Reboots: " + reboots;

        // Show score
        PizzaScore score = GameObject.Find("Pizza").GetComponent<PizzaScore>();
        score.time = time;
        score.UpdateScore();

        // Calculate best time
        string levelName = SceneManager.GetActiveScene().name;

        UpdateBestTime(time, levelName);

        if (levelName.Equals("Level4"))
        {
            UpdateBestTime(totalTime, "Game");

            PlayerPrefs.SetInt("GameComplete", 1);
            GameObject.Find("TotalTime").GetComponent<Text>().text =
                "Total time: " + gTimer.text;
        }

        inCompletePanel = true;
    }

    /// <summary>
    /// Updates the specified level's best time based on the given value.
    /// </summary>
    /// <param name="time"></param>
    /// <param name="levelName"></param>
    void UpdateBestTime(float time, string levelName)
    {
        if (!PlayerPrefs.HasKey(levelName + "BestTime"))
        {
            PlayerPrefs.SetFloat(levelName + "BestTime", time);
        }
        else
        {
            if (PlayerPrefs.GetFloat(levelName + "BestTime") > time)
            {
                PlayerPrefs.SetFloat(levelName + "BestTime", time);
            }
        }

        PlayerPrefs.Save();
    }
}
