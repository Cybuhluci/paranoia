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
    float proneCameraHeight = 0.25f; float proneCharacterHeight = 0.35f;
    float crouchCameraHeight = 0.9f; float crouchCharacterHeight = 1.0f;
    float standingCameraHeight = 1.8f; float standingCharacterHeight = 2f;

    // Phase 1: basic movement
    [SerializeField] float jumpHeight = 1.2f; // max height of jump in Unity units
    [SerializeField] float gravity = -9.81f; // gravity applied to the player, negative because it pulls down
    [SerializeField] float groundedGravity = -2f; // small downward force applied while grounded to keep the CharacterController "snapped" to the ground

    [SerializeField] Vector3 velocity; // current velocity of the player, only vertical component (y) is used in Phase 1
    [SerializeField] Vector2 moveInput; // current input from WASD keys, x = A/D, y = W/S
    bool isGrounded;

    // Phase 2: running + sprinting
    [SerializeField] float doubleTapWindow = 0.3f; // max time (seconds) between taps to count as a double tap
    float lastSprintTapTime = -1f; // Time.time of the last Sprint key press, used for double-tap detection
    bool isRunning; // true while the Sprint action is held down
    bool isSprinting; // true while actively sprinting (after a double tap, and stamina remains)

    public bool toggleRunSprint; // if true, Sprint key toggles running on/off instead of requiring hold. Read from PlayerPrefs.
    public bool runToggledOn; // current toggle state of running, only used when toggleRunSprint is true

    // Phase 3: crouching
    [SerializeField] float crouchTransitionSpeed = 8f; // how quickly the character/camera height lerps between stances
    [SerializeField] LayerMask standUpObstacleMask = ~0; // layers checked to see if something is blocking the player from standing up
    bool isCrouching;
    bool isProne;

    public bool IsCrouching => isCrouching;
    public bool IsProne => isProne;
    public bool IsMoving => moveInput.sqrMagnitude > 0.01f;

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

        toggleRunSprint = PlayerPrefs.GetInt("RunSprintToggle", 0) == 1;

        runSprintAction.performed += OnSprintTap;
        crouchAction.performed += OnCrouchToggle;
        proneAction.performed += OnProneToggle;
    }

    private void OnDestroy()
    {
        runSprintAction.performed -= OnSprintTap;
        crouchAction.performed -= OnCrouchToggle;
        proneAction.performed -= OnProneToggle;
    }

    void OnSprintTap(InputAction.CallbackContext context)
    {
        // detect double tap of the Sprint key to toggle sprinting mode
        if (Time.time - lastSprintTapTime <= doubleTapWindow)
        {
            isSprinting = !isSprinting;
        }
        else if (toggleRunSprint)
        {
            // single tap while in toggle mode flips the running state on/off
            runToggledOn = !runToggledOn;
        }

        lastSprintTapTime = Time.time;
    }

    void OnCrouchToggle(InputAction.CallbackContext context)
    {
        if (isCrouching)
        {
            // only stand up if there's nothing blocking the player from above
            if (!IsObstructedAbove(standingCharacterHeight))
            {
                isCrouching = false;
            }
        }
        else
        {
            isCrouching = true;
            isProne = false; // leaving prone, if we were in it
            isSprinting = false; // can't sprint while crouching
        }
    }

    void OnProneToggle(InputAction.CallbackContext context)
    {
        if (isProne)
        {
            // from prone, Z always attempts to go straight to standing
            if (!IsObstructedAbove(standingCharacterHeight))
            {
                isProne = false;
            }
        }
        else
        {
            isProne = true;
            isCrouching = false; // leaving crouch, if we were in it
            isSprinting = false; // can't sprint while prone
        }
    }

    bool IsObstructedAbove(float targetCharacterHeight)
    {
        if (targetCharacterHeight <= characterController.height)
            return false; // already at or above target height, nothing to check

        float radius = characterController.radius * 0.9f;

        // capsule spanning from the top of the current capsule up to the top of the target (taller) capsule
        Vector3 bottom = transform.position + Vector3.up * (characterController.height - radius);
        Vector3 top = transform.position + Vector3.up * (targetCharacterHeight - radius);

        // CheckCapsule (unlike SphereCast) correctly detects colliders we're already overlapping at the start,
        // which SphereCast would otherwise miss since the player is often already touching the obstruction above.
        return Physics.CheckCapsule(bottom, top, radius, standUpObstacleMask, QueryTriggerInteraction.Ignore);
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

        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        // check for live changes to the toggle-run-sprint setting (e.g. changed in an options menu)
        bool toggleCheck = PlayerPrefs.GetInt("RunSprintToggle", 0) == 1;
        if (toggleCheck != toggleRunSprint)
        {
            toggleRunSprint = toggleCheck;
            runToggledOn = false; // reset toggle state when the setting changes to avoid getting stuck running
        }

        isRunning = toggleRunSprint ? runToggledOn : runSprintAction.IsPressed();

        // sprinting is cancelled if the player stops running, stops moving, runs out of stamina, or is crouching/prone
        if (!isRunning || !isMoving || sprintStamina <= 0f || isCrouching || isProne)
        {
            isSprinting = false;
        }

        float currentSpeed;
        if (isSprinting)
        {
            currentSpeed = sprintSpeed;
            sprintStamina -= sprintStaminaDrainRate * Time.deltaTime;
            sprintStamina = Mathf.Max(sprintStamina, 0f);
        }
        else
        {
            currentSpeed = isRunning ? runSpeed : walkSpeed;

            // regenerate stamina while not sprinting
            sprintStamina += sprintStaminaDrainRate * Time.deltaTime;
            sprintStamina = Mathf.Min(sprintStamina, 100f);
        }

        if (isProne)
        {
            // players can only walk (at proneSpeed) while prone - no running/sprinting
            currentSpeed = proneSpeed;
        }
        else if (isCrouching)
        {
            // running while crouched is capped to walkSpeed, otherwise use crouchSpeed
            currentSpeed = isRunning ? walkSpeed : crouchSpeed;
        }

        HandleStanceTransition();

        // movement direction is relative to where the player is looking (playerCameraTransform)
        Vector3 moveDirection = playerCameraTransform.right * moveInput.x + playerCameraTransform.forward * moveInput.y;
        moveDirection.y = 0f; // ignore any vertical component from the camera's forward/right vectors
        moveDirection.Normalize();

        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        // jumping
        if (jumpAction.WasPressedThisFrame() && isGrounded && !isCrouching && !isProne)
        {
            // v = sqrt(h * -2 * g)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // apply gravity over time
        velocity.y += gravity * Time.deltaTime;

        characterController.Move(velocity * Time.deltaTime);
    }

    void HandleStanceTransition()
    {
        float targetCharacterHeight = standingCharacterHeight;
        float targetCameraHeight = standingCameraHeight;

        if (isProne)
        {
            targetCharacterHeight = proneCharacterHeight;
            targetCameraHeight = proneCameraHeight;
        }
        else if (isCrouching)
        {
            targetCharacterHeight = crouchCharacterHeight;
            targetCameraHeight = crouchCameraHeight;
        }

        characterController.height = Mathf.Lerp(characterController.height, targetCharacterHeight, crouchTransitionSpeed * Time.deltaTime);
        characterController.center = new Vector3(characterController.center.x, characterController.height / 2f, characterController.center.z);

        Vector3 camLocalPos = playerCameraTransform.localPosition;
        camLocalPos.y = Mathf.Lerp(camLocalPos.y, targetCameraHeight, crouchTransitionSpeed * Time.deltaTime);
        playerCameraTransform.localPosition = camLocalPos;
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