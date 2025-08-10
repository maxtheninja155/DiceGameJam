using UnityEngine;

public class FriendlyGambler : MonoBehaviour
{
    // Enum to represent the three possible bets
    public enum BetType { NoBet, Over7, Under7, Exactly7 }

    public int winsCount = 0;

    // Public property so the GameManager can ask what the current bet is
    public BetType CurrentBet { get; private set; }

    void Start()
    {
        CurrentBet = BetType.NoBet; // Initialize with no bet
        winsCount = 0; // Initialize wins count
    }

    // This method makes a random, weighted choice for the bet.
    public void MakeBet()
    {
        // Generate a random number between 0.0 and 1.0
        float randomValue = Random.value;

        // 45% chance for Under 7
        if (randomValue < 0.45f)
        {
            CurrentBet = BetType.Under7;
        }
        // 45% chance for Over 7 (from 0.45 to 0.90)
        else if (randomValue < 0.90f)
        {
            CurrentBet = BetType.Over7;
        }
        // 10% chance for Exactly 7 (the remaining range)
        else
        {
            CurrentBet = BetType.Exactly7;
        }

        Debug.Log("Friendly Gambler bets on: " + CurrentBet);
    }
}