using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static GameManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }
    }

    [SerializeField] private List<DiceController> diceControllers = new List<DiceController>();
    [SerializeField] private GameState gameState; // Private but visible in Inspector for debugging

    private float timeFreezeTimer;

    private bool hasUsedStickyAbilityThisRound;
    public bool CanUseStickyAbility() => !hasUsedStickyAbilityThisRound;
    public GameState GetGameState() => gameState; // Public getter

    public enum GameState { Ready, Rolling, Results, TimeFreeze }

    void Start()
    {
        // Find all dice controllers in the scene at the start
        diceControllers = Object.FindObjectsByType<DiceController>(FindObjectsSortMode.None).ToList();
        SetGameState(GameState.Ready);
        ReRollDice(); // Automatically roll the dice when the game starts
    }

    void Update()
    {
        // NEW: Check if the rolling phase is over
        if (gameState == GameState.Rolling)
        {
            // If all dice have stopped rolling, change the state to Results.
            if (diceControllers.All(die => die.CurrentState == DiceController.DiceState.Stopped))
            {
                SetGameState(GameState.Results);
            }
        }

        // Handle the time freeze countdown
        if (gameState == GameState.TimeFreeze)
        {
            timeFreezeTimer -= Time.deltaTime;
            if (timeFreezeTimer <= 0)
            {
                SetGameState(GameState.Rolling); // Return to rolling state
            }
        }
    }

    public void SetGameState(GameState newState)
    {
        if (gameState == newState) return;
        gameState = newState;
        Debug.Log("Game state changed to: " + gameState);

        switch (newState)
        {
            case GameState.Rolling:
                UnfreezeDice(); // Ensure dice are not kinematic before rolling
                break;
            case GameState.Results:
                CalculateResults();
                break;
            case GameState.TimeFreeze:
                FreezeDice();
                break;
        }
    }

    public void CalculateResults()
    {
        // This method can be used to calculate the results based on the top faces of the dice
        foreach (DiceController die in diceControllers)
        {
            // Ensure any spring joints are removed before calculating results
            int topFace = die.GetTopFace();
            Debug.Log($"{die.name} shows: {topFace}");
        }
    }

    public void ReRollDice()
    {
        // Can only reroll from the Ready or Results states
        if (gameState != GameState.Ready && gameState != GameState.Results) return;

        hasUsedStickyAbilityThisRound = false;

        SetGameState(GameState.Rolling);
        foreach (DiceController die in diceControllers)
        {
            die.RollDice();
        }
    }

    public void StartTimeFreeze(float duration)
    {
        // Can only freeze time while the dice are rolling
        if (gameState != GameState.Rolling) return;
        timeFreezeTimer = duration;
        SetGameState(GameState.TimeFreeze);
    }

    private void FreezeDice()
    {
        foreach (DiceController die in diceControllers)
        {
            Rigidbody rb = die.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Store the current velocity
                die.storedVelocity = rb.linearVelocity;
                die.storedAngularVelocity = rb.angularVelocity;

                // Make the Rigidbody kinematic to freeze it in place
                rb.isKinematic = true;
            }
        }
    }

    private void UnfreezeDice()
    {
        foreach (DiceController die in diceControllers)
        {
            Rigidbody rb = die.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Make the Rigidbody dynamic again
                rb.isKinematic = false;

                // Reapply the stored velocity to resume motion
                rb.linearVelocity = die.storedVelocity;
                rb.angularVelocity = die.storedAngularVelocity;
            }
        }
    }

    public void NotifyStickyAbilityUsed()
    {
        hasUsedStickyAbilityThisRound = true;
    }
}
