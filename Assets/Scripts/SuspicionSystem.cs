// Create a new script named SuspicionSystem.cs
using UnityEngine;

public class SuspicionSystem : MonoBehaviour
{
    // --- Singleton Pattern for easy access ---
    public static SuspicionSystem Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }
    }

    // Assign your UIManager in the Inspector
    [SerializeField] private UIManager uiManager;

    // The different levels of suspicion
    public enum SuspicionLevel
    {
        NotSuspicious,
        CuriousGlance,
        Suspicious,
        GettingAngry,
        Accusatory,
        CaughtCheating
    }
    
    private SuspicionLevel currentSuspicionLevel;

    // Variables to remember the last roll's outcome
    private int lastDie1Result = 0;
    private int lastDie2Result = 0;

    void Start()
    {
        // Initialize the system at the start of the game
        currentSuspicionLevel = SuspicionLevel.NotSuspicious;
        // Update the UI with the starting level
        uiManager.UpdateSuspicionText(currentSuspicionLevel);
    }

    // This is called by the GameManager after a roll is complete.
    public void AssessRoll(int die1Result, int die2Result)
    {
        // --- Suspicion Check: Repeat Roll ---
        // Check if the current roll is an exact match of the last one (order doesn't matter)
        bool isRepeatRoll = (die1Result == lastDie1Result && die2Result == lastDie2Result) ||
                            (die1Result == lastDie2Result && die2Result == lastDie1Result);

        if (isRepeatRoll && lastDie1Result != 0) // Don't trigger on the very first roll
        {
            Debug.LogWarning("SUSPICION EVENT: Exact same roll as last time!");
            IncreaseSuspicion();
        }

        // --- Update History ---
        // Remember this roll's results for the next round's check.
        lastDie1Result = die1Result;
        lastDie2Result = die2Result;
    }

    private void IncreaseSuspicion()
    {
        // Don't increase if we've already caught the player
        if (currentSuspicionLevel == SuspicionLevel.CaughtCheating) return;

        // Move to the next level
        currentSuspicionLevel++;
        Debug.Log("Suspicion has increased to: " + currentSuspicionLevel);

        // Tell the UI to update
        uiManager.UpdateSuspicionText(currentSuspicionLevel);

        // Check for game over condition
        if (currentSuspicionLevel == SuspicionLevel.CaughtCheating)
        {
            Debug.LogError("GAME OVER: You have been caught cheating!");
            GameManager.Instance.SetGameState(GameManager.GameState.End);
        }
    }
}