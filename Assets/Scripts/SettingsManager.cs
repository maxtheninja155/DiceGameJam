using UnityEngine;
using UnityEngine.UI; // Required for the Toggle UI element

public class SettingsManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Toggle musicToggle;

    // A key to save our setting. Using a const prevents typos.
    private const string MUSIC_ENABLED_KEY = "MusicEnabled";

    void Start()
    {
        // Add a listener to the toggle so our function is called when it's clicked.
        musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);

        // Load the saved preference when the game starts.
        // PlayerPrefs.GetInt defaults to 1 (true) if the key doesn't exist yet.
        bool isMusicEnabled = PlayerPrefs.GetInt(MUSIC_ENABLED_KEY, 1) == 1;

        // Update the toggle's visual state to match the loaded setting.
        musicToggle.isOn = isMusicEnabled;

        // Apply the setting to the SoundManager.
        ApplyMusicSetting(isMusicEnabled);
    }

    // This function is called every time the toggle is clicked.
    private void OnMusicToggleChanged(bool isEnabled)
    {
        // Apply the new setting immediately.
        ApplyMusicSetting(isEnabled);

        // Save the preference to the player's device.
        PlayerPrefs.SetInt(MUSIC_ENABLED_KEY, isEnabled ? 1 : 0);
        PlayerPrefs.Save(); // Good practice to save immediately.
    }

    private void ApplyMusicSetting(bool isEnabled)
    {
        // Tell the SoundManager to mute or unmute its music sources.
        SoundManager.SetMusicMuted(!isEnabled);
        Debug.Log("Music is now " + (isEnabled ? "ON" : "OFF"));
    }
}