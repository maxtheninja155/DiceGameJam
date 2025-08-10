// In UIManager.cs
using UnityEngine;
using TMPro; // Add this line to use TextMeshPro

public class UIManager : MonoBehaviour
{
    // Assign your TextMeshPro text object to this slot in the Inspector
    [SerializeField] private TMP_Text outcomeToAchieveText;
    [SerializeField] private TMP_Text diceOutcomeText;

    [SerializeField] private TMP_Text suspicionLevelText;
    [SerializeField] private TMP_Text winsCountText;

    // A public method the GameManager can call to update the text.
    public void UpdateOutcomeText(FriendlyGambler.BetType bet)
    {
        string goalText = "Gamblers Bet: ";
        switch (bet)
        {
            case FriendlyGambler.BetType.Over7:
                goalText += "Over 7";
                break;
            case FriendlyGambler.BetType.Under7:
                goalText += "Under 7";
                break;
            case FriendlyGambler.BetType.Exactly7:
                goalText += "Exactly 7";
                break;
        }

        outcomeToAchieveText.text = goalText;
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
    
    public void UpdateWinsCountText(int winsCount)
    {
        // Update the wins count text
        winsCountText.text = "Wins: " + winsCount.ToString();
    }
}