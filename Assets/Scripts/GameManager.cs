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

    [SerializeField] private FriendlyGambler friendlyGambler;
    [SerializeField] private UIManager uiManager;


    [SerializeField] private List<DiceController> diceControllers = new List<DiceController>();
    [SerializeField] private GameState gameState; // Private but visible in Inspector for debugging

    [Header("Round Timing")]
    public float roundEndTime = 10.0f; // Total time from results to next roll
    [Range(0f, 1f)]
    public float waitOnTableRatio = 0.6f; // e.g., 60% of roundEndTime (6s)
    [Range(0f, 1f)]
    public float floatDurationRatio = 0.2f; // e.g., 20% of roundEndTime (2s)
    // The final "wait in air" time is the remaining 20% (2s)

    private float roundEndTimer;
    private bool hasInitiatedFloat;

    private float timeFreezeTimer;

    private int stickyUsesRemaining;
    public bool CanUseStickyAbility() => stickyUsesRemaining > 0;
    public GameState GetGameState() => gameState; // Public getter

    public enum GameState { Ready, Rolling, Results, TimeFreeze, End }

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

        if (gameState == GameState.Results)
        {
            roundEndTimer -= Time.deltaTime;

            // --- Calculate the time thresholds ---
            // When should the float START? After the "wait on table" time is over.
            float floatStartTime = roundEndTime * (1.0f - waitOnTableRatio);
            // How long should the float animation take?
            float floatDuration = roundEndTime * floatDurationRatio;

            // --- Trigger the float at the correct time ---
            if (!hasInitiatedFloat && roundEndTimer <= floatStartTime)
            {
                foreach (DiceController die in diceControllers)
                {
                    // Tell the die to start floating and how long it has to get there.
                    die.StartFloatingBack(floatDuration);
                }
                hasInitiatedFloat = true;
            }

            // --- Start the next round when the total time is up ---
            if (roundEndTimer <= 0)
            {
                ReRollDice();
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
                roundEndTimer = roundEndTime; // Reset the timer for the next round
                CalculateResults();
                break;
            case GameState.TimeFreeze:
                FreezeDice();
                break;
            case GameState.End:
                // Handle game over logic here, add a function to detect if 
                    // the player has been caught cheating or not (Win or Loss?).
                break;
        }
    }

    public void CalculateResults()
    {

        if (friendlyGambler == null)
        {
            Debug.LogError("FriendlyGambler reference not set in GameManager!");
            return;
        }

        int[] diceResults = new int[diceControllers.Count];
        int diceTotal = 0;

        // This method can be used to calculate the results based on the top faces of the dice
        foreach (DiceController die in diceControllers)
        {
            int topFace = die.GetTopFace();
            diceResults[diceControllers.IndexOf(die)] = topFace;
            diceTotal += topFace;
        }

        if (diceResults.Length == 2)
        {
            SuspicionSystem.Instance.AssessRoll(diceResults[0], diceResults[1]);
        }

        FriendlyGambler.BetType playerBet = friendlyGambler.CurrentBet;
        bool didWin = false;

        // Check the outcome based on the gambler's bet
        switch (playerBet)
        {
            case FriendlyGambler.BetType.NoBet:
                Debug.LogWarning("No bet was made by the gambler. A bet will be made when the die settle");
                break;
            case FriendlyGambler.BetType.Over7:
                if (diceTotal > 7) didWin = true;
                break;
            case FriendlyGambler.BetType.Under7:
                if (diceTotal < 7) didWin = true;
                break;
            case FriendlyGambler.BetType.Exactly7:
                if (diceTotal == 7) didWin = true;
                break;
        }

        // Print the final result to the console
        Debug.Log($"--- ROUND OVER ---");
        if (playerBet != FriendlyGambler.BetType.NoBet)
            Debug.Log($"Gambler Bet: {playerBet}");
        uiManager.UpdateDiceOutcomeText(diceTotal);
        if (playerBet != FriendlyGambler.BetType.NoBet)
        {
            if (didWin)
            {
                Debug.Log("<color=green>RESULT: The gambler won! Good job rigging.</color>");
                friendlyGambler.winsCount++;
                uiManager.UpdateWinsCountText(friendlyGambler.winsCount);
            }
            else
            {
                Debug.Log("<color=red>RESULT: The gambler lost! Try again.</color>");
            }
        }

        friendlyGambler.MakeBet();
        
        uiManager.UpdateOutcomeText(friendlyGambler.CurrentBet);
        
    }

    public void ReRollDice()
    {
        // Can only reroll from the Ready or Results states
        if (gameState != GameState.Ready && gameState != GameState.Results) return;

        hasInitiatedFloat = false;

        stickyUsesRemaining = 2;

        SetGameState(GameState.Rolling);
        foreach (DiceController die in diceControllers)
        {
            die.ResetRepulsion();
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
        if (stickyUsesRemaining > 0)
        {
            stickyUsesRemaining--;
        }
    }
}
