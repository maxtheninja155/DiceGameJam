// In SuspicionSystem.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SuspicionSystem : MonoBehaviour
{
    // --- Singleton Pattern ---
    public static SuspicionSystem Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }
    }

    [SerializeField] private UIManager uiManager;

    public enum SuspicionLevel { NotSuspicious, CuriousGlance, Suspicious, GettingAngry, Accusatory, CaughtCheating }
    private SuspicionLevel currentSuspicionLevel;

    // --- State Tracking Variables ---
    private int lastDie1Result = 0;
    private int lastDie2Result = 0;

    // Streak Tracking
    private int consecutiveWins = 0;
    private int consecutiveLosses = 0;
    private List<float> winStreakWagerPercentages = new List<float>();
    private List<float> lossStreakWagerPercentages = new List<float>();

    // Bet Consistency Tracking
    private PlayerData.BetType lastBetType = PlayerData.BetType.NoBet;
    private int consecutiveBetTypeCount = 0;

    // "Exactly 7" Win History
    private Queue<bool> recentExactly7Wins = new Queue<bool>();
    private const int EXACTLY_7_HISTORY_LENGTH = 6;

    void Start()
    {
        currentSuspicionLevel = SuspicionLevel.NotSuspicious;
        uiManager.UpdateSuspicionText(currentSuspicionLevel);
    }

    // This is now the main entry point, called by GameManager.
    public void AssessRound(PlayerData.BetType bet, int wager, int totalGold, bool didWin, int die1, int die2)
    {
        float wagerPercentage = (float)wager / totalGold;

        // --- 1. Update all historical data based on the round's outcome ---
        UpdateStreakData(didWin, wagerPercentage);
        UpdateBetConsistencyData(bet);
        UpdateExactly7History(didWin, bet);

        // --- 2. Run all suspicion checks ---
        CheckWinningStreaks();
        CheckLosingStreaks();
        CheckBetConsistency();
        CheckExactly7Jackpot();
        CheckForRepeatRoll(die1, die2); // The original check

        // --- 3. Update dice history for the next round ---
        lastDie1Result = die1;
        lastDie2Result = die2;
    }

    // --- Update Methods ---
    private void UpdateStreakData(bool didWin, float percentage)
    {
        if (didWin)
        {
            consecutiveWins++;
            consecutiveLosses = 0;
            winStreakWagerPercentages.Add(percentage);
            lossStreakWagerPercentages.Clear();
        }
        else
        {
            consecutiveLosses++;
            consecutiveWins = 0;
            lossStreakWagerPercentages.Add(percentage);
            winStreakWagerPercentages.Clear();
        }
    }

    private void UpdateBetConsistencyData(PlayerData.BetType bet)
    {
        if (bet == lastBetType && bet != PlayerData.BetType.NoBet)
        {
            consecutiveBetTypeCount++;
        }
        else
        {
            consecutiveBetTypeCount = 1;
        }
        lastBetType = bet;
    }

    private void UpdateExactly7History(bool didWin, PlayerData.BetType bet)
    {
        recentExactly7Wins.Enqueue(didWin && bet == PlayerData.BetType.Exactly7);
        if (recentExactly7Wins.Count > EXACTLY_7_HISTORY_LENGTH)
        {
            recentExactly7Wins.Dequeue();
        }
    }

    // --- Check Methods ---
    private void CheckWinningStreaks()
    {
        if (consecutiveWins == 3 && winStreakWagerPercentages.All(p => p == 1.0f))
        {
            Debug.LogWarning("SUSPICION EVENT: 3 wins in a row at 100% wager!");
            IncreaseSuspicion(2);
            consecutiveWins = 0; winStreakWagerPercentages.Clear(); // Reset streak after triggering
        }
        else if (consecutiveWins == 4 && winStreakWagerPercentages.All(p => p == 0.5f))
        {
            Debug.LogWarning("SUSPICION EVENT: 4 wins in a row at 50% wager!");
            IncreaseSuspicion(2);
            consecutiveWins = 0; winStreakWagerPercentages.Clear();
        }
        else if (consecutiveWins == 5 && winStreakWagerPercentages.All(p => p == 0.25f))
        {
            Debug.LogWarning("SUSPICION EVENT: 5 wins in a row at 25% wager!");
            IncreaseSuspicion(2);
            consecutiveWins = 0; winStreakWagerPercentages.Clear();
        }
    }

    private void CheckLosingStreaks()
    {
        if (consecutiveLosses == 2 && lossStreakWagerPercentages.All(p => p == 0.25f))
        {
            Debug.Log("<color=lightblue>SUSPICION DECREASED:</color> Lost 2 in a row at 25% wager.");
            DecreaseSuspicion(2);
            consecutiveLosses = 0; lossStreakWagerPercentages.Clear();
        }
        // Check for a single 50% loss
        else if (consecutiveLosses > 0 && lossStreakWagerPercentages.Last() == 0.5f)
        {
            Debug.Log("<color=lightblue>SUSPICION DECREASED:</color> Lost a round at 50% wager.");
            DecreaseSuspicion(2);
            consecutiveLosses = 0; lossStreakWagerPercentages.Clear();
        }
    }

    private void CheckBetConsistency()
    {
        if (consecutiveBetTypeCount >= 4)
        {
            Debug.LogWarning("SUSPICION EVENT: Same bet type chosen 4 times in a row!");
            IncreaseSuspicion(1);
            consecutiveBetTypeCount = 0; // Reset counter
        }
    }

    private void CheckExactly7Jackpot()
    {
        if (recentExactly7Wins.Count(isWin => isWin) >= 2)
        {
            Debug.LogWarning("SUSPICION EVENT: Won 'Exactly 7' twice in the last 6 rounds!");
            IncreaseSuspicion(4);
            recentExactly7Wins.Clear(); // Reset history to prevent re-triggering
        }
    }

    private void CheckForRepeatRoll(int die1, int die2)
    {
        bool isRepeatRoll = (die1 == lastDie1Result && die2 == lastDie2Result) || (die1 == lastDie2Result && die2 == lastDie1Result);
        if (isRepeatRoll && lastDie1Result != 0)
        {
            Debug.LogWarning("SUSPICION EVENT: Exact same roll as last time!");
            IncreaseSuspicion(1);
        }
    }

    // --- Modify Suspicion Level ---
    public void IncreaseSuspicion(int amount)
    {
        if (currentSuspicionLevel == SuspicionLevel.CaughtCheating) return;
        currentSuspicionLevel = (SuspicionLevel)Mathf.Min((int)currentSuspicionLevel + amount, (int)SuspicionLevel.CaughtCheating);
        uiManager.UpdateSuspicionText(currentSuspicionLevel);
        if (currentSuspicionLevel == SuspicionLevel.CaughtCheating)
        {
            GameManager.Instance.SetGameState(GameManager.GameState.End);
        }
    }

    public void DecreaseSuspicion(int amount)
    {
        if (currentSuspicionLevel == SuspicionLevel.NotSuspicious) return;
        currentSuspicionLevel = (SuspicionLevel)Mathf.Max((int)currentSuspicionLevel - amount, (int)SuspicionLevel.NotSuspicious);
        uiManager.UpdateSuspicionText(currentSuspicionLevel);
    }

    public void ResetSuspicion()
    {
        currentSuspicionLevel = SuspicionLevel.NotSuspicious;
        lastDie1Result = 0;
        lastDie2Result = 0;
        consecutiveWins = 0;
        consecutiveLosses = 0;
        winStreakWagerPercentages.Clear();
        lossStreakWagerPercentages.Clear();
        lastBetType = PlayerData.BetType.NoBet;
        consecutiveBetTypeCount = 0;
        recentExactly7Wins.Clear();

        // Update the UI to reflect the reset
        uiManager.UpdateSuspicionText(currentSuspicionLevel);
    }
}