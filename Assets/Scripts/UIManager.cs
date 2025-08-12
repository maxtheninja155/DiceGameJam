using TMPro; 
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

    [Header("UI Panels")]
    [SerializeField] private GameObject inGameHUDPanel;

    [SerializeField] private GameObject pausePanel; // Assign your pause menu panel here
    [SerializeField] private GameObject settingsPanel; // NEW: Assign your settings panel here
    [SerializeField] private GameObject howToPlayPanel;

    [SerializeField] private GameObject winScreenPanel;  // NEW: Assign your Win Screen panel
    [SerializeField] private GameObject loseScreenPanel; // NEW: Assign your Lose Screen panel


    [SerializeField] private GameObject crosshairImg;

    [Header("UI Text Fields")]
    [SerializeField] private TMP_Text diceOutcomeText;
    [SerializeField] private TMP_Text suspicionLevelText;
    [SerializeField] private TMP_Text goldCountText;

    private bool isPaused = false;

    private void Start()
    {
        // Ensure the pause panel is hidden at the start of the game
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // --- NEW: Public methods to show the end screens ---
    public void ShowWinScreen()
    {
        inGameHUDPanel.SetActive(false); // Hide the main game UI
        winScreenPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowLoseScreen()
    {
        inGameHUDPanel.SetActive(false); // Hide the main game UI
        loseScreenPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // This freezes all time-based movement and physics
        crosshairImg.SetActive(false); // Hide the crosshair when paused
        pausePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None; // Unlock the cursor
        Cursor.visible = true; // Make the cursor visible to click buttons
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Resume normal time
        crosshairImg.SetActive(true); // Show the crosshair again
        inGameHUDPanel.SetActive(true); // Show the in-game HUD
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
        howToPlayPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked; // Re-lock the cursor for gameplay
        Cursor.visible = false; // Hide the cursor again
    }

    // --- NEW: Button Functions ---

    public void RestartGame()
    {
        // IMPORTANT: Always reset time scale before loading a new scene
        Time.timeScale = 1f;
        // Reload the currently active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OpenSettings()
    {
        // Hide the pause panel and show the settings panel.
        pausePanel?.SetActive(false);
        settingsPanel?.SetActive(true);
        Debug.Log("Settings button clicked!");
    }

    public void CloseSettings()
    {
        settingsPanel?.SetActive(false);
        pausePanel?.SetActive(true);
    }

    public void OpenHowToPlay()
    {
        pausePanel?.SetActive(false);
        howToPlayPanel?.SetActive(true);
    }

    public void CloseHowToPlay()
    {
        howToPlayPanel?.SetActive(false);
        pausePanel?.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();

        // This line is for quitting the editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void UpdateDiceOutcomeText(int diceTotal)
    {
        diceOutcomeText.text = $"The Dice Rolled: {diceTotal}";
    }

    public void UpdateSuspicionText(SuspicionSystem.SuspicionLevel level)
    {
        // The .ToString() method on an enum is great for display purposes
        suspicionLevelText.text = "Suspicion: " + level.ToString();
    }
    
    public void UpdateGoldText(int currentGold)
    {
        // Update the wins count text
        goldCountText.text = "Gold: " + currentGold.ToString();
    }

    public void OnEndlessModeClicked()
    {
        Time.timeScale = 1f;
        GameManager.Instance.StartEndlessMode();
    }

    // NEW: A helper function to easily hide both end screens.
    public void HideEndScreens()
    {
        winScreenPanel?.SetActive(false);
        loseScreenPanel?.SetActive(false);
    }
}