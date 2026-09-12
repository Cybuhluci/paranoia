using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine; // version 3.1.7
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] PlayerCamera playerCamera; // Reference to the PlayerCamera script, which handles camera movement and rotation
    Transform playerCameraTransform; // this is NOT the main camera, but the parent of the CinemachineCamera
                                     // - the [transform].forward of this transform is where the player is looking.

    [SerializeField] PlayerInput playerInput;
    [SerializeField] CharacterController characterController;

    InputAction moveAction; // WASD keys to move
    InputAction jumpAction; // Space key to jump
    InputAction runSprintAction; // Left Shift key to run, double tap to sprint
    InputAction crouchAction; // C key to from any-stance to crouch, or from crouch to standing
    InputAction proneAction; // Z key to from any-stance to prone, or from prone to standing
    InputAction stanceCycleAction; // Left CTRL key to cycle through stances (prone, crouch, stand)
    InputAction slideAction; // Left ALT key to slide while running or sprinting - slide ends in crouch stance

    // Movement speeds in Unity-Units per second (m/s)
    float proneSpeed = 0.75f;
    float crouchSpeed = 1.5f;
    float walkSpeed = 3f;
    float runSpeed = 4.5f;
    float sprintSpeed = 6f;

    float sprintStamina = 100f;
    float sprintStaminaDrainRate = 15f; // Stamina drained per second while sprinting

    // stance states
    float proneCameraHeight = 0.45f; float proneCharacterHeight = 0.5f;
    float crouchCameraHeight = 0.9f; float crouchCharacterHeight = 1.0f;
    float standingCameraHeight = 1.8f; float standingCharacterHeight = 2f;

    // Phase 1: basic movement
    [SerializeField] float jumpHeight = 1.2f; // max height of jump in Unity units
    [SerializeField] float gravity = -9.81f; // gravity applied to the player, negative because it pulls down
    [SerializeField] float groundedGravity = -2f; // small downward force applied while grounded to keep the CharacterController "snapped" to the ground

    [SerializeField] Vector3 velocity; // current velocity of the player, only vertical component (y) is used in Phase 1
    [SerializeField] Vector2 moveInput; // current input from WASD keys, x = A/D, y = W/S
    bool isGrounded;

    private void Awake()
    {
        playerCameraTransform = playerCamera.playerCameraTransform; // get the transform of the PlayerCamera script's GameObject

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        runSprintAction = playerInput.actions["Sprint"];
        crouchAction = playerInput.actions["Crouch"];
        proneAction = playerInput.actions["Prone"];
        stanceCycleAction = playerInput.actions["Stance"];
        slideAction = playerInput.actions["Slide"];
    }

    /*
    how it works as a system:
    walking is the default state, which is walkSpeed.
    the player can hold down the Left Shift key to increase speed to runSpeed.
    if they player were to double tap the Left Shift key, they would go into sprinting mode, which is sprintSpeed.
    sprinting is a limited action - which means the player can only sprint for a certain amount of time before they go back to running.

    if running and crouching, the player will move at wallkSpeed.

    players cannot run nor sprint while prone. They can only walk while prone.

    the three stance states are: prone, crouch, and standing. The player can cycle through these stances using the Left CTRL key. 
    The player can also use the C key to toggle between crouch and standing, and the Z key to toggle between prone and standing.

    when running or sprinting, the player can press the Left ALT key to slide. Sliding will end in crouch stance.
    when sprinting, the player can press Z to dive into prone stance, which will end in prone stance. 

    Phase 1: WASD + Space (walk + jump)
    Phase 2: + Running (Left Shift) + Sprinting (double tap Left Shift)
    Phase 3: + Stances (Crouch, Prone, Stand) 
    Phase 4: + Sliding (Left ALT)
    Phase 5: + Diving (Z)
     */

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        if (Time.timeScale == 0f) // If the game is paused, do not process movement
            return;

        HandleMovement();
    }

    void HandleMovement()
    {
        isGrounded = characterController.isGrounded;

        // keep the player grounded (a small constant downward force instead of 0, so isGrounded stays true)
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = groundedGravity;
        }

        // read WASD input (Vector2: x = A/D, y = W/S)
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        this.moveInput = moveInput;

        // movement direction is relative to where the player is looking (playerCameraTransform)
        Vector3 moveDirection = playerCameraTransform.right * moveInput.x + playerCameraTransform.forward * moveInput.y;
        moveDirection.y = 0f; // ignore any vertical component from the camera's forward/right vectors
        moveDirection.Normalize();

        float currentSpeed = walkSpeed;

        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        // jumping
        if (jumpAction.WasPressedThisFrame() && isGrounded)
        {
            // v = sqrt(h * -2 * g)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // apply gravity over time
        velocity.y += gravity * Time.deltaTime;

        characterController.Move(velocity * Time.deltaTime);
    }

    // FixedUpdate is called every fixed framerate frame, and is used for physics calculations
    private void FixedUpdate()
    {

    }

    // LateUpdate is called every frame, after all Update functions have been called. It is used to follow up on calculations that were done in Update.
    private void LateUpdate()
    {
        
    }
}