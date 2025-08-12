using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public enum BetType { NoBet, Over7, Under7, Exactly7 }

    [Header("Player Stats")]
    public int startingGold = 1000; // The amount of gold you start with
    public int currentGold;
    public int winsCount = 0;

    [Header("Current Round")]
    public BetType currentBet;
    public int currentWager;

    private void Awake()
    {
        // Set the current gold to the starting amount when the game begins.
        currentGold = startingGold;
    }
}