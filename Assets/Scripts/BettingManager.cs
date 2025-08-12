using UnityEngine;

public class BettingManager : MonoBehaviour
{
    [Header("Object References")]
    [SerializeField] private GameObject betSelectionCubes;  // A parent object holding the 3 bet cubes
    [SerializeField] private GameObject wagerSelectionCubes; // A parent object holding the 3 wager cubes
    [SerializeField] private PlayerData playerData;         // Reference to the player's data

    // Called by GameManager to start the betting phase
    public void StartBettingPhase()
    {
        playerData.currentBet = PlayerData.BetType.NoBet;
        playerData.currentWager = 0;

        betSelectionCubes.SetActive(true);
        wagerSelectionCubes.SetActive(false);
    }

    // Called by the PlayerGamblerController when a bet cube is clicked
    public void OnBetSelected(BettingCube.BetChoice bet)
    {
        switch (bet)
        {
            case BettingCube.BetChoice.Over7:
                playerData.currentBet = PlayerData.BetType.Over7;
                break;
            case BettingCube.BetChoice.Under7:
                playerData.currentBet = PlayerData.BetType.Under7;
                break;
            case BettingCube.BetChoice.Exactly7:
                playerData.currentBet = PlayerData.BetType.Exactly7;
                break;
        }

        Debug.Log("Player selected bet: " + playerData.currentBet);

        // Swap to the wager selection
        betSelectionCubes.SetActive(false);
        wagerSelectionCubes.SetActive(true);
    }

    // Called by the PlayerGamblerController when a wager cube is clicked
    public void OnWagerSelected(BettingCube.WagerChoice wager)
    {
        float wagerPercentage = 0;
        switch (wager)
        {
            case BettingCube.WagerChoice.Percent25:
                wagerPercentage = 0.25f;
                break;
            case BettingCube.WagerChoice.Percent50:
                wagerPercentage = 0.50f;
                break;
            case BettingCube.WagerChoice.Percent100:
                wagerPercentage = 1.0f;
                break;
        }

        // Calculate the wager and hide the cubes
        playerData.currentWager = (int)(playerData.currentGold * wagerPercentage);
        wagerSelectionCubes.SetActive(false);

        Debug.Log("Player wagered: " + playerData.currentWager);

        // Tell the GameManager it's time to roll!
        GameManager.Instance.StartRollingPhase();
    }
}