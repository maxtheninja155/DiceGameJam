using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class GhostController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerGamblerController PlayerGamblerController;

    // --- Public Variables for tweaking in the Inspector ---
    public float speed = 5.0f;
    public float mouseSensitivity = 100.0f;
    public Transform cameraTransform; // Assign your child camera to this in the Inspector

    private bool movementEnabled = true;

    [Header("Crouch Settings")]
    public Transform objectToCrouch; // Assign your player's Camera or body transform
    public float crouchSpeed = 10f; // Adjust this to change how fast you crouch/stand
    public float standingHeight = 2.0f;
    public float crouchHeight = 1.0f;

    [Header("Input Settings")]
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode timeFreezeKey = KeyCode.T;
    public KeyCode setInitStickyAbilityKey = KeyCode.Mouse0;
    public KeyCode setFinalStickyAbilityKey = KeyCode.Mouse1;
    public KeyCode repelKey = KeyCode.E;

    private bool isCrouching;


    [Header("Ability Settings")]
    public float timeFreezeMaximumDuration = 5.0f;
    [SerializeField] private StickyAbility stickyAbility; // Reference to the StickyAbility script
    [SerializeField] private RepulsionAbility repulsionAbility; // NEW


    // --- Private Variables ---
    private float xRotation = 0f;
    private Transform playerTransform;


    void Start()
    {
        // Lock the cursor to the center of the screen for a clean FPS look
        Cursor.lockState = CursorLockMode.Locked;
        playerTransform = transform;
        standingHeight = playerTransform.position.y;
    }

    void Update()
    {
        // --- LOOKING (Mouse) ---

        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Calculate vertical rotation (for looking up/down)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Prevents flipping upside down

        // Apply rotation to the camera (up/down) and the player body (left/right)
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);


        // --- MOVEMENT (Keyboard) ---

        if (movementEnabled)
        {
            // Get keyboard input
            float moveX = Input.GetAxis("Horizontal"); // A/D keys
            float moveZ = Input.GetAxis("Vertical");   // W/S keys

            // Create a movement vector based on the direction the player is facing
            Vector3 move = transform.right * moveX + transform.forward * moveZ;

            // Apply the movement to the GhostPlayer's position
            transform.Translate(move * speed * Time.deltaTime, Space.World);
        }

        // --- CROUCHING ---
        // When the crouch key is pressed, set the state to crouching
        if (Input.GetKeyDown(crouchKey))
        {
            isCrouching = true;
        }

        // When the crouch key is released, set the state to standing
        if (Input.GetKeyUp(crouchKey))
        {
            isCrouching = false;
        }

        // --- Movement Logic ---
        // Determine the target height based on the current state
        float targetHeight = isCrouching ? this.crouchHeight : this.standingHeight;

        // Get the object's current position
        Vector3 currentPosition = objectToCrouch.localPosition;

        // Calculate the new Y position smoothly
        float newY = Mathf.Lerp(currentPosition.y, targetHeight, crouchSpeed * Time.deltaTime);

        // Apply the new position
        objectToCrouch.localPosition = new Vector3(currentPosition.x, newY, currentPosition.z);

        // --- 1. TIME FREEZE TOGGLE ---
        // This block only handles turning the time freeze ON and OFF.
        if (Input.GetKeyDown(timeFreezeKey))
        {
            // If we are currently rolling, start the time freeze
            if (GameManager.Instance.GetGameState() == GameManager.GameState.Rolling)
            {
                GameManager.Instance.StartTimeFreeze(timeFreezeMaximumDuration);
            }
            // If time is already frozen, pressing the key again will unfreeze it
            else if (GameManager.Instance.GetGameState() == GameManager.GameState.TimeFreeze)
            {
                // Return to the Rolling state when unfreezing
                GameManager.Instance.SetGameState(GameManager.GameState.Rolling);
            }
        }

        // --- 2. STICKY ABILITY USAGE ---
        // This block checks for ability inputs ONLY when the game is already in the TimeFreeze state.
        if (GameManager.Instance.GetGameState() == GameManager.GameState.TimeFreeze)
        {
            // Check with the GameManager if the ability hasn't been used yet this round
            if (GameManager.Instance.CanUseStickyAbility())
            {
                // On Left-Click, try to set the anchor point on the table
                if (Input.GetKeyDown(setInitStickyAbilityKey))
                {
                    Ray cameraRay = new Ray(cameraTransform.position, cameraTransform.forward);
                    stickyAbility.TrySetAnchorPoint(cameraRay, PlayerGamblerController.clickableLayers);
                }

                // On Right-Click, try to attach the spring to a die
                if (Input.GetKeyDown(setFinalStickyAbilityKey))
                {
                    Ray cameraRay = new Ray(cameraTransform.position, cameraTransform.forward);
                    stickyAbility.TryAttachSpringToDie(cameraRay, PlayerGamblerController.clickableLayers);
                }

                // --- REPULSION ABILITY INPUT ---
                if (Input.GetKeyDown(repelKey))
                {
                    Ray cameraRay = new Ray(cameraTransform.position, cameraTransform.forward);
                    repulsionAbility.TryMarkFaceForRepulsion(cameraRay);
                }
            }
        }
    }

    public void SetMovementEnabled(bool isEnabled)
    {
        movementEnabled = isEnabled;
    }

}