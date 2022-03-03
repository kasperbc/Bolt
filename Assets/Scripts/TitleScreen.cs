using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TitleScreen : MonoBehaviour
{
    [SerializeField]
    string startingLevel;    // What level the game starts at
    [SerializeField]
    GameObject optionsMenu;  // Options menu
    [SerializeField]
    GameObject infoMenu;
    [SerializeField]
    GameObject levelSelectMenu, levelSelectButton;

    // Options
    [SerializeField]
    Toggle showTimer;        // Show timer toggle
    [SerializeField]
    Toggle camEffects;       // Camera effects toggle
    [SerializeField]
    Slider sfxVolume;        // Sound volume
    [SerializeField]
    Slider musicVolume;      // Music volume
    [SerializeField]
    Text sfxValue, musicValue;      // Sfx and Music percentage values
    void Start()
    {
        // Does the player have any prefrences stored?
        if (!PlayerPrefs.HasKey("ShowTimer"))
        {
            // If not, set default prefrences
            SetDefaultPrefs();
        }

        Application.runInBackground = true;

        SoundManager.instance.StopAllSounds();
        SoundManager.instance.PlaySound("title", 1, 1, true, true);

        levelSelectButton.GetComponent<Button>().interactable = PlayerPrefs.GetInt("GameComplete").Equals(1);
    }

    /// <summary>
    /// Starts the game and loads the first level.
    /// </summary>
    public void StartGame(string level)
    {
        SoundManager.instance.StopAllSounds();
        SoundManager.instance.PlaySound("race", 1, 1, true, true);

        SceneManager.LoadScene(level);
    }

    public void StartGame()
    {
        StartGame(startingLevel);
    }

    /// <summary>
    /// Exits the game.
    /// </summary>
    public void ExitGame()
    {
        Application.Quit();
    }

    public void ToggleInfo()
    {
        infoMenu.SetActive(!infoMenu.activeInHierarchy);
    }

    public void ToggleLevelSelect()
    {
        levelSelectMenu.SetActive(!levelSelectMenu.activeInHierarchy);

        if (!levelSelectMenu.activeInHierarchy)
        {
            return;
        }

        SetBestTime(GameObject.Find("Level1BT").GetComponent<Text>(), PlayerPrefs.GetFloat("Level1BestTime"));
        SetBestTime(GameObject.Find("Level2BT").GetComponent<Text>(), PlayerPrefs.GetFloat("Level2BestTime"));
        SetBestTime(GameObject.Find("Level3BT").GetComponent<Text>(), PlayerPrefs.GetFloat("Level3BestTime"));
        SetBestTime(GameObject.Find("Level4BT").GetComponent<Text>(), PlayerPrefs.GetFloat("Level4BestTime"));
    }

    private void SetBestTime(Text textObj, float time)
    {;
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

        textObj.text = "Best time: " + minutePadding + minutes + ":" + secondPadding + seconds.ToString().Replace(",", ".");
    }

    public void OpenOptions()
    {
        // Set option values
        showTimer.isOn = PlayerPrefs.GetInt("ShowTimer").Equals(1);
        camEffects.isOn = PlayerPrefs.GetInt("Effects").Equals(1);
        sfxVolume.value = PlayerPrefs.GetFloat("SoundVolume");
        musicVolume.value = PlayerPrefs.GetFloat("MusicVolume");
        
        optionsMenu.SetActive(true);

        // Show music and sfx volume percentages
        SetSfxMusicPercentage();
    }

    public void CloseOptions()
    {
        // Close options
        optionsMenu.SetActive(false);
    }

    public void SavePrefs()
    {
        // Show timer
        PlayerPrefs.SetInt("ShowTimer", showTimer.isOn.GetHashCode());
        // Camera effects
        PlayerPrefs.SetInt("Effects", camEffects.isOn.GetHashCode());
        // Volume
        PlayerPrefs.SetFloat("SoundVolume", sfxVolume.value);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume.value);

        // Print prefs
        print("Options saved. (ShowTimer: " + PlayerPrefs.GetInt("ShowTimer") + ", Effects: " + PlayerPrefs.GetInt("Effects") + ", Sound: " +
            PlayerPrefs.GetFloat("SoundVolume") + ", Music: " + PlayerPrefs.GetFloat("MusicVolume") + ")");

        // Save prefs
        PlayerPrefs.Save();

        // Restart title music
        SoundManager.instance.StopAllSounds();
        SoundManager.instance.PlaySound("title", 1, 1, true, true);

        // Close options
        CloseOptions();
    }

    public void ResetPrefs()
    {
        Keyboard kb = Keyboard.current;

        // Set default prefs (clear prefs if holding down alt)
        if (!kb.leftAltKey.isPressed)
            SetDefaultPrefs();
        else
            ClearPrefs();

        // Close options
        CloseOptions();
    }

    public void ClearPrefs()
    {
        // Clear prefrences
        PlayerPrefs.DeleteAll();

        levelSelectButton.GetComponent<Button>().interactable = PlayerPrefs.GetInt("GameComplete").Equals(1);
    }

    private void SetDefaultPrefs()
    {
        // Show timer
        PlayerPrefs.SetInt("ShowTimer", 0);
        // Camera effects
        PlayerPrefs.SetInt("Effects", 1);
        // Volume
        PlayerPrefs.SetFloat("SoundVolume", 1);
        PlayerPrefs.SetFloat("MusicVolume", 1);

        // Save default prefs
        PlayerPrefs.Save();
    }

    public void SetSfxMusicPercentage()
    {
        sfxValue.text = Mathf.Round(sfxVolume.value * 100).ToString() + "%";
        musicValue.text = Mathf.Round(musicVolume.value * 100).ToString() + "%";
    }
}
