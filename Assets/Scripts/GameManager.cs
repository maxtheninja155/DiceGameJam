using System.Collections;
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
    public static bool IsEndlessMode { get; private set; } = false;

    [Header("Core References")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayerData playerData;
    [SerializeField] private BettingManager bettingManager;
    [SerializeField] private RepulsionAbility repulsionAbility;

    [Header("Player Control")]
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject ghostObject;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera ghostCamera;
    [SerializeField] private float transitionDuration = 1.5f;

    private Vector3 playerCameraOriginalLocalPos;
    [SerializeField] private List<DiceController> diceControllers = new List<DiceController>();
    [SerializeField] private GameState gameState;

    [Header("Round Timing")]
    public float roundEndTime = 10.0f;
    [Range(0f, 1f)] public float waitOnTableRatio = 0.6f;
    [Range(0f, 1f)] public float floatDurationRatio = 0.2f;

    private float roundEndTimer;
    private bool hasInitiatedFloat;
    private float timeFreezeTimer;
    private int stickyUsesRemaining;

    public bool CanUseStickyAbility() => stickyUsesRemaining > 0;
    public GameState GetGameState() => gameState;

    public enum GameState { Ready, Rolling, Results, TimeFreeze, End }

    void Start()
    {
        SetEndlessMode(false);

        playerObject.GetComponent<PlayerGamblerController>().enabled = true;
        playerCamera.enabled = true;
        ghostObject.SetActive(false);
        diceControllers = FindObjectsByType<DiceController>(FindObjectsSortMode.None).ToList();
        playerCameraOriginalLocalPos = playerCamera.transform.localPosition;


        // The coroutine is no longer needed, we can start the first round directly.
        StartNewRound();
    }

    void Update()
    {
        if (gameState == GameState.Rolling)
        {
            if (diceControllers.All(die => die.CurrentState == DiceController.DiceState.Stopped))
            {
                SetGameState(GameState.Results);
            }
        }
        else if (gameState == GameState.TimeFreeze)
        {
            timeFreezeTimer -= Time.deltaTime;
            if (timeFreezeTimer <= 0)
            {
                SetGameState(GameState.Rolling);
            }
        }
        else if (gameState == GameState.Results)
        {
            roundEndTimer -= Time.deltaTime;
            float floatStartTime = roundEndTime * (1.0f - waitOnTableRatio);
            float floatDuration = roundEndTime * floatDurationRatio;

            if (!hasInitiatedFloat && roundEndTimer <= floatStartTime)
            {
                foreach (DiceController die in diceControllers)
                {
                    die.StartFloatingBack(floatDuration);
                }
                hasInitiatedFloat = true;
            }

            if (roundEndTimer <= 0)
            {
                StartNewRound();
            }
        }
    }

    void LateUpdate()
    {
        // Check which camera is currently active and sync the listener's position to it.
        if (ghostCamera.enabled)
        {
            // If the ghost is active, the listener should be where the ghost is.
            this.transform.position = ghostCamera.transform.position;
            this.transform.rotation = ghostCamera.transform.rotation;
        }
        else
        {
            // Otherwise, the listener should be where the player is.
            this.transform.position = playerCamera.transform.position;
            this.transform.rotation = playerCamera.transform.rotation;
        }
    }

    public void StartNewRound()
    {
        SetGameState(GameState.Ready);
        bettingManager.StartBettingPhase();
        uiManager.UpdateGoldText(playerData.currentGold);
    }

    public void StartRollingPhase()
    {
        StartCoroutine(AstralProjectOutCoroutine());
    }

    public void SetGameState(GameState newState)
    {
        if (gameState == newState) return;
        gameState = newState;
        Debug.Log("Game state changed to: " + gameState);

        switch (newState)
        {
            case GameState.Results:
                roundEndTimer = roundEndTime;
                StartCoroutine(ReturnToBodyCoroutine());
                break;
            case GameState.TimeFreeze:
                SoundManager.PlaySound(SoundType.AbilitySFX, 0, 1.5f);
                SoundManager.PlayMusicStinger(SoundType.BackgroundMusic, 0.5f);
                FreezeDice();
                break;
            case GameState.Rolling:
                SoundManager.StopMusicStinger();
                UnfreezeDice();
                break;
            case GameState.End:
                SoundManager.StopMusicStinger();
                // This will be called if you run out of money OR if SuspicionSystem catches you.
                Time.timeScale = 0f; // Pause the game
                ghostObject.SetActive(false); // Make sure the ghost is hidden
                playerObject.SetActive(true); // Make sure the player is active
                playerCamera.enabled = true;
                playerObject.GetComponent<PlayerGamblerController>().enabled = false; // Disable controls

                // Check which end screen to show.
                if (playerData.currentGold >= playerData.startingGold * 15)
                {
                    uiManager.ShowWinScreen();
                }
                else
                {
                    uiManager.ShowLoseScreen();
                }
                break;
        }
    }

    private IEnumerator AstralProjectOutCoroutine()
    {
        Debug.Log("Astrally projecting OUT of body...");
        playerObject.GetComponent<PlayerGamblerController>().enabled = false;

        // --- NEW: Rotation Syncing Logic ---
        // Get the final rotation of the player's camera.
        Quaternion playerLookRotation = playerCamera.transform.rotation;

        // Decompose the player's look rotation into horizontal (body) and vertical (head) parts.
        // The horizontal rotation (left/right) is the rotation around the Y-axis.
        Quaternion ghostBodyRotation = Quaternion.Euler(0, playerLookRotation.eulerAngles.y, 0);
        // The vertical rotation (up/down) is the rotation around the X-axis.
        Quaternion ghostHeadRotation = Quaternion.Euler(playerLookRotation.eulerAngles.x, 0, 0);

        // --- Prepare the Ghost ---
        ghostObject.SetActive(true);
        // Apply the synced rotations BEFORE enabling the cameras.
        ghostObject.transform.rotation = ghostBodyRotation;
        ghostCamera.transform.localRotation = ghostHeadRotation;

        // Position the ghost camera exactly where the player's was to start the transition.
        ghostCamera.transform.position = playerCamera.transform.position;

        // Immediately switch active cameras for a seamless transition.
        playerCamera.enabled = false;
        ghostCamera.enabled = true;

        // --- The rest of the coroutine is unchanged ---
        Vector3 startPos = ghostCamera.transform.position;
        Vector3 endPos = ghostObject.transform.position;

        float elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            // Lerp the GHOST'S camera from the player's viewpoint to its own starting spot.
            ghostCamera.transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / transitionDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Finalize transition: Enable the ghost controller.
        ghostObject.GetComponent<GhostController>().enabled = true;

        // Now, roll the dice.
        hasInitiatedFloat = false;
        stickyUsesRemaining = 2;
        SetGameState(GameState.Rolling);
        foreach (DiceController die in diceControllers)
        {
            die.ResetRepulsion();
            die.RollDice();
        }
    }


    private IEnumerator ReturnToBodyCoroutine()
    {
        Debug.Log("Returning TO body...");

        GhostController ghostController = ghostObject.GetComponent<GhostController>();
        ghostController.enabled = false;

        Vector3 startPos = ghostCamera.transform.position;
        Vector3 endPos = playerCamera.transform.parent.TransformPoint(playerCameraOriginalLocalPos);

        ghostCamera.enabled = false;
        playerCamera.enabled = true;

        float elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            playerCamera.transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / transitionDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        playerCamera.transform.localPosition = playerCameraOriginalLocalPos;
        ghostObject.SetActive(false);
        playerObject.GetComponent<PlayerGamblerController>().enabled = true;

        repulsionAbility.ResetAll();
        
        CalculateResults();
    }

    public void CalculateResults()
    {
        if (playerData == null)
        {
            Debug.LogError("PlayerData reference not set in GameManager!");
            return;
        }

        int diceTotal = 0;
        int[] diceResults = new int[diceControllers.Count];
        for (int i = 0; i < diceControllers.Count; i++)
        {
            int topFace = diceControllers[i].GetTopFace();
            diceResults[i] = topFace;
            diceTotal += topFace;
        }

        PlayerData.BetType playerBet = playerData.currentBet;
        bool didWin = false;

        switch (playerBet)
        {
            case PlayerData.BetType.Over7: if (diceTotal > 7) didWin = true; break;
            case PlayerData.BetType.Under7: if (diceTotal < 7) didWin = true; break;
            case PlayerData.BetType.Exactly7: if (diceTotal == 7) didWin = true; break;
        }

        // CHANGED: Call the new, more detailed assessment method.
        SuspicionSystem.Instance?.AssessRound(
            playerBet,
            playerData.currentWager,
            playerData.currentGold + (didWin ? -playerData.currentWager : playerData.currentWager), // Pass gold *before* this round's win/loss
            didWin,
            diceResults[0],
            diceResults[1]
        );

        Debug.Log($"--- ROUND OVER ---");
        Debug.Log($"Player Bet: {playerBet} for {playerData.currentWager} gold.");
        uiManager.UpdateDiceOutcomeText(diceTotal);

        if (didWin)
        {
            Debug.Log("<color=green>RESULT: You won the bet!</color>");
            playerData.currentGold += playerData.currentWager;
            playerData.winsCount++;
        }
        else
        {
            Debug.Log("<color=red>RESULT: You lost the bet!</color>");
            playerData.currentGold -= playerData.currentWager;
        }

        uiManager.UpdateGoldText(playerData.currentGold);
        CheckEndGameConditions();
    }
    private void CheckEndGameConditions()
    {
        // CHANGED: The win condition is now ignored if IsEndlessMode is true.
        if (!IsEndlessMode && playerData.currentGold >= playerData.startingGold * 15)
        {
            SetGameState(GameState.End);
        }
        else if (playerData.currentGold <= 0)
        {
            SetGameState(GameState.End);
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

    public static void SetEndlessMode(bool isEndless)
    {
        IsEndlessMode = isEndless;
    }

    // NEW: This method is called by the UIManager to start the endless run.
    public void StartEndlessMode()
    {
        playerObject.GetComponent<PlayerGamblerController>().enabled = true;
        // Set the flag
        IsEndlessMode = true;

        // Reset suspicion for the new run
        SuspicionSystem.Instance?.ResetSuspicion();

        // Hide the win screen and start a new round.
        uiManager.HideEndScreens(); // We will add this helper method to UIManager
        Time.timeScale = 1f; // Make sure time is running
        uiManager.ResumeGame();
        StartNewRound();
    }
}
