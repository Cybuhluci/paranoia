using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine; // version 3.1.7

public class PlayerCamera : MonoBehaviour
{
    public Transform playerCameraTransform; // this is NOT the main camera, but the parent of the CinemachineCamera
    public PlayerInput playerInput; // Reference to the PlayerInput component, which handles input actions

    InputAction lookAction; // Mouse movement to look around

    [Range(1, 14)]
    public int sensitivitySetting = 4; // User-facing sensitivity option, from 1 (slow) to 14 (insanely fast). 4 is the default.

    const float sensitivityPerUnit = 0.025f; // At a setting of 4, this results in a multiplier of 0.1
    float lookSensitivity => sensitivitySetting * sensitivityPerUnit; // Actual multiplier used for rotation

    public float minPitch = -80f; // Minimum vertical look angle
    public float maxPitch = 80f; // Maximum vertical look angle

    float yaw; // Rotation around the Y axis (left/right)
    float pitch; // Rotation around the X axis (up/down)

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        lookAction = playerInput.actions["Look"];
        Cursor.lockState = CursorLockMode.Locked; // Lock the cursor to the center of the screen
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Vector3 currentEuler = playerCameraTransform.eulerAngles;
        yaw = currentEuler.y;
        pitch = currentEuler.x;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.timeScale == 0f) // If the game is paused, do not process camera movement
            return;

        int sensCheck = PlayerPrefs.GetInt("Sensitivity", 4);
        if (sensCheck != sensitivitySetting)
            sensitivitySetting = sensCheck;
        // ^ this system here could be improved by using a unityevent for sensitivity changes, but for now this is fine.

        int sensivityMultiplier = 1; // default multiplier for mouse input
        if (lookAction.activeControl != null && lookAction.activeControl.device is Gamepad)
            sensivityMultiplier = 3; // triple the sensitivity for gamepad input
        else
            sensivityMultiplier = 1; // normal sensitivity for mouse input

        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        yaw += lookInput.x * lookSensitivity * sensivityMultiplier;
        pitch -= lookInput.y * lookSensitivity * sensivityMultiplier;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        playerCameraTransform.eulerAngles = new Vector3(pitch, yaw, 0f);
    }
}