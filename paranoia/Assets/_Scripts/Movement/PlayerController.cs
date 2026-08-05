using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.UI; // version 3.1.7

public class PlayerController : MonoBehaviour
{
    [Header("These Things")] // they are always set in the inspector, so they don't need to be set in code - don't change them in code.
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform playerCameraLookAtTransform; // rotate this for camera movement.
    [SerializeField] private CinemachineCamera playerCamera; // do not change the transform of ever.
    [SerializeField] private PlayerInput playerInput;
    // player input actions: "Move", "Look", "Jump", "Sprint", "Crouch"
    [SerializeField] private Slider tempStaminaSlider; // this is a temporary slider for testing stamina, it will be removed later.

    // any more of these "set in inspector" variables should be added here.

    [Header("Movement Settings")]
    //(1 Hammer Unit = 1 / 16 of a foot(about 2cm))
    private float walkSpeed = 175f; // in Hammer Units per second 
    private float runSpeed = 210f; // in Hammer Units per second
    private float sprintSpeed = 275f; // in Hammer Units per second
    private float crouchSpeed = 100f; // in Hammer Units per second
    private float jumpPower = 100f;
    private float gravity = -9.81f;

    private float sensitivityBase = 50; // the "base sensitivity" for the mouse, which is used when calculating the actual sensitivity.
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

    private Vector3 velocity;
    private float currentStamina;
    private float timeSinceLastSprint;
    [SerializeField] private bool isCrouching;
    [SerializeField] private bool isSprinting;

    // Look
    private float pitch;

    // Recoil
    private float recoilPitch;
    private float recoilYaw;

    private float recoilRecoveryDelay;
    private float recoilRecoveryRate;
    private float timeSinceLastRecoil;

    // True once we've fired at least one shot in the current firing sequence.
    private bool hasRecoilStarted;

    public MovementState currentMovementState;
    public StanceState currentStanceState;

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
        HandleRecoilRecovery();
        HandleLook();
        HandleCrouch();
        HandleMovement();
    }

    private void HandleLook()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        float mouseSensitivity =
            sensitivity * (sensitivityBase / 1000f);

        // ------------------------------------------------------------
        // NORMAL MOUSE LOOK
        // ------------------------------------------------------------

        float mouseYaw = lookInput.x * mouseSensitivity;
        float mousePitch = lookInput.y * mouseSensitivity;

        // ------------------------------------------------------------
        // MANUAL RECOIL CORRECTION
        // ------------------------------------------------------------

        // If the player moves the mouse in the direction that opposes
        // the currently accumulated recoil, treat that movement as the
        // player manually "fighting" the recoil - consume it from the
        // recoil accumulators directly instead of just changing the
        // player's normal aim. This way, whatever the player corrects
        // themselves is remembered, and the automatic recovery system
        // won't try to recover recoil that's already been dealt with.

        // Vertical: recoilPitch positive kicks the camera up, so moving
        // the mouse down (negative mousePitch) is the correcting motion.
        if (recoilPitch > 0f && mousePitch < 0f)
        {
            float verticalCorrection = Mathf.Min(recoilPitch, -mousePitch);
            recoilPitch -= verticalCorrection;
            mousePitch += verticalCorrection;
        }

        // Horizontal: moving the mouse opposite to the sign of recoilYaw
        // is the correcting motion.
        if (recoilYaw != 0f && Mathf.Sign(mouseYaw) == -Mathf.Sign(recoilYaw))
        {
            float horizontalCorrection = Mathf.Min(Mathf.Abs(recoilYaw), Mathf.Abs(mouseYaw));
            float signedCorrection = horizontalCorrection * Mathf.Sign(mouseYaw);
            recoilYaw += signedCorrection;
            mouseYaw -= signedCorrection;
        }

        // Horizontal mouse movement rotates the player.
        transform.Rotate(Vector3.up * mouseYaw);

        // Vertical mouse movement changes the player's intended pitch.
        pitch -= mousePitch;

        pitch = Mathf.Clamp(pitch, -89f, 89f);


        // ------------------------------------------------------------
        // RECOIL
        // ------------------------------------------------------------

        // Recoil is an offset from the player's intended aim.
        //
        // recoilPitch is positive when the weapon kicks upwards,
        // therefore it is subtracted from the camera pitch.
        float finalPitch = pitch - recoilPitch;

        // Prevent recoil from pushing the camera beyond its limits.
        finalPitch = Mathf.Clamp(finalPitch, -89f, 89f);

        // recoilYaw is the accumulated horizontal kick from ApplyRecoil,
        // applied on top of the camera's local rotation (does not affect
        // the player body's yaw, only where the camera is looking).
        float finalYaw = recoilYaw;


        // Apply final aim.
        playerCameraLookAtTransform.localRotation =
            Quaternion.Euler(finalPitch, finalYaw, 0f);
    }


    public void ApplyRecoil(
        float verticalRecoil,
        float horizontalDirection,
        float horizontalVariation,
        RecoilDirection biasDirection,
        float recoveryDelay,
        float recoveryRate)
    {
        // ------------------------------------------------------------
        // DETERMINE HORIZONTAL BIAS
        // ------------------------------------------------------------

        float horizontalBias = Mathf.Abs(horizontalDirection);

        switch (biasDirection)
        {
            case RecoilDirection.Left:
                horizontalBias = -horizontalBias;
                break;

            case RecoilDirection.Right:
                break;

            case RecoilDirection.Neutral:
                // Neutral recoil has no preferred direction.
                horizontalBias = 0f;
                break;
        }


        // ------------------------------------------------------------
        // HORIZONTAL RECOIL
        // ------------------------------------------------------------

        float horizontalKick;

        if (!hasRecoilStarted)
        {
            // FIRST SHOT
            //
            // The first shot always lands exactly on the weapon's
            // horizontal bias.
            horizontalKick = horizontalBias;
        }
        else
        {
            // SUBSEQUENT SHOTS
            //
            // Random variation is added around the weapon's bias.
            //
            // Example:
            // Bias = 5°
            // Variation = 5°
            //
            // Possible results:
            // 0°, 2°, 5°, 7°, 10°, etc.
            float variation =
                Random.Range(
                    -horizontalVariation,
                    horizontalVariation
                );

            horizontalKick = horizontalBias + variation;
        }


        // Add this shot's horizontal recoil to the accumulated
        // horizontal recoil.
        recoilYaw += horizontalKick;


        // ------------------------------------------------------------
        // VERTICAL RECOIL
        // ------------------------------------------------------------

        // Every shot adds vertical recoil.
        //
        // Example with 0.31° vertical recoil:
        //
        // Shot 1 = 0.31°
        // Shot 2 = 0.62°
        // Shot 3 = 0.93°
        // Shot 4 = 1.24°
        //
        // This is what produces the vertical climb of an automatic
        // weapon.
        recoilPitch += verticalRecoil;


        // ------------------------------------------------------------
        // RECOVERY
        // ------------------------------------------------------------

        recoilRecoveryDelay = recoveryDelay;
        recoilRecoveryRate = recoveryRate;

        // Every shot resets the recovery timer.
        timeSinceLastRecoil = 0f;

        // The weapon is now in a firing/recoil sequence.
        hasRecoilStarted = true;
    }


    private void HandleRecoilRecovery()
    {
        // Nothing to recover.
        if (Mathf.Approximately(recoilPitch, 0f) &&
            Mathf.Approximately(recoilYaw, 0f))
        {
            // Once all recoil has been recovered, the next shot
            // becomes the first shot of a new recoil sequence.
            hasRecoilStarted = false;

            return;
        }


        // ------------------------------------------------------------
        // RECOVERY DELAY
        // ------------------------------------------------------------

        timeSinceLastRecoil += Time.deltaTime;

        if (timeSinceLastRecoil < recoilRecoveryDelay)
        {
            return;
        }


        // ------------------------------------------------------------
        // RECOVER RECOIL
        // ------------------------------------------------------------

        float recoveryStep =
            recoilRecoveryRate * Time.deltaTime;

        // Recover vertical recoil.
        recoilPitch = Mathf.MoveTowards(
            recoilPitch,
            0f,
            recoveryStep
        );

        // Recover horizontal recoil.
        recoilYaw = Mathf.MoveTowards(
            recoilYaw,
            0f,
            recoveryStep
        );


        // If everything has returned to zero, this firing sequence
        // is finished.
        if (Mathf.Approximately(recoilPitch, 0f) &&
            Mathf.Approximately(recoilYaw, 0f))
        {
            hasRecoilStarted = false;
        }
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

        // Use the camera's forward/right instead of the player body's,
        // flattened onto the horizontal plane so pitch doesn't affect
        // movement speed/direction. This keeps movement aligned with
        // wherever the camera is actually looking (including recoil
        // offsets), and will also make it trivial to reuse this same
        // logic for swimming later on.
        Vector3 cameraForward = playerCameraLookAtTransform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = playerCameraLookAtTransform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraRight * moveInput.x) + (cameraForward * moveInput.y);
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
public enum MovementState
{
    Walking,
    Running,
    Sprinting
}

public enum StanceState
{
    Standing,
    Crouching,
    Proning
}