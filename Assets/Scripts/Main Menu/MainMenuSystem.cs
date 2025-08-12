using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes

public class MainMenuSystem : MonoBehaviour
{
    [Header("Camera Control")]
    public Transform lookAtTarget; // The empty object in the center of the table
    public float rotationSpeed = 5.0f;
    public Vector3 lookAtOffset = new Vector3(1.5f, 0, 0); // Shifts the view slightly to the right


    [Header("UI Panels")]
    [SerializeField] private GameObject mainPanel; // Assign your pause menu panel here
    [SerializeField] private GameObject settingsPanel;

    void Update()
    {
        // Check if a target has been assigned to prevent errors
        if (lookAtTarget != null)
        {
            // 1. Slowly rotate the camera around the target object
            transform.RotateAround(lookAtTarget.position, Vector3.up, rotationSpeed * Time.deltaTime);

            // 2. Always look at the target, but with the added offset
            transform.LookAt(lookAtTarget.position + lookAtOffset);
        }
    }

    // --- UI Button Functions ---

    public void StartGame()
    {
        GameManager.SetEndlessMode(false);

        SceneManager.LoadScene("MainGame");
    }

    public void OpenSettings()
    {
        // Hide the pause panel and show the settings panel.
        mainPanel?.SetActive(false);
        settingsPanel?.SetActive(true);
        Debug.Log("Settings button clicked!");
    }

    public void CloseSettings()
    {
        settingsPanel?.SetActive(false);
        mainPanel?.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();

        // This line is for quitting the editor, you can remove it for the final build
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}