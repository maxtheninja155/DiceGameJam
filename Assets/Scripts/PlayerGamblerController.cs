using UnityEngine;

public class PlayerGamblerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private BettingManager bettingManager;

    [Header("Raycast Settings")]
    public LayerMask clickableLayers;

    [Header("Look Settings")]
    public float mouseSensitivity = 100f;
    public float horizontalLookLimit = 45f; // NEW: Limit for looking left and right

    // Private variables to store vertical and horizontal rotation
    private float xRotation = 0f;
    private float yRotation = 0f; // NEW

    void Start()
    {
        // Lock the cursor to the center of the screen and hide it
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // --- UPDATED: LOOKING LOGIC ---

        // Get mouse input values
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Calculate and clamp vertical rotation (up/down)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // NEW: Calculate and clamp horizontal rotation (left/right)
        yRotation += mouseX;
        yRotation = Mathf.Clamp(yRotation, -horizontalLookLimit, horizontalLookLimit);

        // CHANGED: Apply both rotations to the camera/head transform
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);

        // REMOVED: The line that rotated the entire player body is gone.
        // transform.Rotate(Vector3.up * mouseX);


        // --- BETTING LOGIC

        if (GameManager.Instance.GetGameState() != GameManager.GameState.Ready) return;

        if (Input.GetMouseButtonDown(0)) // Left-Click
        {
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, clickableLayers))
            {
                BettingCube cube = hit.collider.GetComponent<BettingCube>();
                if (cube != null)
                {
                    if (cube.isBetCube)
                    {
                        bettingManager.OnBetSelected(cube.betType);
                    }
                    else
                    {
                        bettingManager.OnWagerSelected(cube.wagerType);
                    }
                }
            }
        }
    }
}