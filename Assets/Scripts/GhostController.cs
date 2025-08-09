using UnityEngine;

public class GhostController : MonoBehaviour
{
    // --- Public Variables for tweaking in the Inspector ---
    public float speed = 5.0f;
    public float mouseSensitivity = 100.0f;
    public Transform cameraTransform; // Assign your child camera to this in the Inspector

    [Header("Crouch Settings")]
    public Transform objectToCrouch; // Assign your player's Camera or body transform
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float crouchSpeed = 10f; // Adjust this to change how fast you crouch/stand
    public float standingHeight = 2.0f;
    public float crouchHeight = 1.0f;

    private bool isCrouching;

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

        // Get keyboard input
        float moveX = Input.GetAxis("Horizontal"); // A/D keys
        float moveZ = Input.GetAxis("Vertical");   // W/S keys

        // Create a movement vector based on the direction the player is facing
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        // Apply the movement to the GhostPlayer's position
        transform.Translate(move * speed * Time.deltaTime, Space.World);

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
    }


}