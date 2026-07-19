using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.UI; // version 3.1.7

public class PlayerController : MonoBehaviour
{
    [Header("These Things")] // they are always set in the inspector, so they don't need to be set in code - don't change them in code.
    [SerializeField] private CharacterController controller;
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private PlayerInput playerInput;
    // player input actions: "Move", "Look", "Jump", "Sprint", "Crouch"
    [SerializeField] private Slider tempStaminaSlider; // this is a temporary slider for testing stamina, it will be removed later.

    // any more of these "set in inspector" variables should be added here.

    [Header("Movement Settings")]
    //(1 Hammer Unit = 1 / 16 of a foot(about 2cm))
    private float walkSpeed = 175f; // in Hammer Units per second 
    private float runSpeed = 210f; // in Hammer Units per second
    private float sprintSpeed = 250f; // in Hammer Units per second
    private float crouchSpeed = 100f; // in Hammer Units per second
    private float jumpPower = 100f;
    private float gravity = -9.81f;

    private float sensitivityBase = 100; // the "base sensitivity" for the mouse, which is used when calculating the actual sensitivity.
    private float sensitivity = 4; // the number that actually controls the sensitivity of the mouse.
                                   // 4 is the default sensitivity in CoD Black Ops, of which the movement system is based on.
                                   // the lowest number is 1, and the highest number is 14.

    private int LungCapacity = 1000; // this is the diegetic name for stamina.
    private int sprintDrain = 250; // how much stamina is drained per second while sprinting.
    private float sprintRegenDelay = 3f; // seconds of no sprinting before stamina starts regenerating.
    private int sprintRegen = 333; // how much stamina is regenerated per second once regen kicks in.

    private float crouchHeight = 0.9f;
    private float standHeight = 1.8f;
    private float crouchTransitionSpeed = 8f;

    // movement system explained:

    // sprinting happens when the player is holding down the sprint button and moving forward, and has enough stamina to sprint.
    // when running out of sprint, the player enters the run state, where speed is reduced to the run speed, and stamina is NOT drained.
    // stamina only begins to regenerate after sprinting has stopped for a certain amount of time (3 seconds).

    // the player can crouch at any time, which reduces speed to the crouch speed obivously.
    // if the player was sprinting and crouches, the player will be in a sort of running crouch state, where you move at the run speed, but you are crouched.
    // This is useful for sneaking around, but it is not as fast as sprinting.
    // sprint-crouching uses the same drain rate as sprinting, and the player will be at run speed while doing so.

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction crouchAction;

    private Transform cameraTransform;

    private float pitch;
    private Vector3 velocity;
    private float currentStamina;
    private float timeSinceLastSprint;
    [SerializeField] private bool isCrouching;
    [SerializeField] private bool isSprinting;

    private void Awake()
    {
        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        jumpAction = playerInput.actions["Jump"];
        sprintAction = playerInput.actions["Sprint"];
        crouchAction = playerInput.actions["Crouch"];

        cameraTransform = playerCamera.transform;
        currentStamina = LungCapacity;
        timeSinceLastSprint = sprintRegenDelay;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        HandleLook();
        HandleCrouch();
        HandleMovement();
    }

    private void HandleLook()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        float mouseSensitivity = sensitivity * (sensitivityBase / 1000f);

        float yaw = lookInput.x * mouseSensitivity;
        pitch -= lookInput.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        transform.Rotate(Vector3.up * yaw);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleCrouch()
    {
        bool crouchHeld = crouchAction.IsPressed();
        isCrouching = crouchHeld;

        float targetHeight = isCrouching ? crouchHeight : standHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        controller.center = new Vector3(0f, controller.height / 2f, 0f);
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 moveDirection = (transform.right * moveInput.x) + (transform.forward * moveInput.y);
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        bool sprintHeld = sprintAction.IsPressed() && moveInput.y > 0f;
        bool wantsToSprint = sprintHeld && currentStamina > 0f;
        bool outOfStaminaWhileSprinting = sprintHeld && currentStamina <= 0f;

        float speed;
        if (isCrouching)
        {
            // sprint-crouching moves at run speed but still drains stamina like sprinting.
            speed = wantsToSprint ? runSpeed : crouchSpeed;
            isSprinting = wantsToSprint;
        }
        else if (wantsToSprint)
        {
            speed = sprintSpeed;
            isSprinting = true;
        }
        else if (outOfStaminaWhileSprinting)
        {
            // still holding sprint but ran out of stamina - reduced to run speed, no drain.
            speed = runSpeed;
            isSprinting = false;
        }
        else
        {
            speed = walkSpeed;
            isSprinting = false;
        }

        HandleStamina();

        // convert Hammer Units per second to Unity units per second (1 HU = 1/16 foot ? 0.01905m).
        const float hammerUnitToMeters = 0.01905f;
        Vector3 finalMove = moveDirection * (speed * hammerUnitToMeters);

        if (controller.isGrounded)
        {
            velocity.y = -2f; // small downward force to keep the controller grounded.

            if (jumpAction.WasPressedThisFrame())
            {
                velocity.y = Mathf.Sqrt(jumpPower * hammerUnitToMeters * -2f * gravity);
            }
        }

        velocity.y += gravity * Time.deltaTime;

        Vector3 totalMove = (finalMove + new Vector3(0f, velocity.y, 0f)) * Time.deltaTime;
        controller.Move(totalMove);
    }

    private void HandleStamina()
    {
        tempStaminaSlider.value = currentStamina;
        if (isSprinting)
        {
            currentStamina -= sprintDrain * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);
            timeSinceLastSprint = 0f;
        }
        else
        {
            timeSinceLastSprint += Time.deltaTime;

            if (timeSinceLastSprint >= sprintRegenDelay)
            {
                currentStamina += sprintRegen * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, LungCapacity);
            }
        }
    }
}
