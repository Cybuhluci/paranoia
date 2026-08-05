using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GunRuntime : MonoBehaviour
{
    // this script is used to handle the runtime behavior of the gun, such as shooting, reloading, and aiming.
    // it will be attached to the gun prefab and will be responsible for handling the gun's behavior during gameplay.

    [SerializeField] private Transform gunModelTransorm; // the transform of the gun model, used for animations and positioning - this is set in inspector.
    [SerializeField] private Transform muzzleTransform; // the transform of the gun's muzzle, where the bullets will be spawned from - this is set in inspector.
    [SerializeField] private Transform aimTransform; // the camera transform, used to raycast where the player is actually looking - this is set in inspector.
    [SerializeField] private float maxAimDistance = 500f; // how far the aim raycast checks for a target point.
    [SerializeField] LayerMask ignoreLayers;

    [SerializeField] private PlayerInput playerInput; // reference to the PlayerInput component for input handling
    // player input actions: "Fire", "Reload", "ADS"
    [SerializeField] private GunMain gunMain; // reference to the GunMain script, in-case it is needed.

    [SerializeField] private Animator gunAnimator; // reference to the Animator component for handling gun animations
    [SerializeField] private PlayerController playerController; // reference to the PlayerController script, in-case it is needed.

    private InputAction fireAction;
    private InputAction reloadAction;

    public GunSO GunData { get; private set; }
    public bool IsReloading { get; private set; }

    private FireMode currentFireMode;
    private float fireCooldown; // time between shots, based on fire rate (RPM).
    private float nextFireTime;

    #region Runtime/Changing Stats
    // these are the stats that change during gameplay, mainly such as ammo count.
    public int CurrentAmmo { get; private set; }
    public int ReserveAmmo { get; private set; }
    #endregion

    #region GunStats-FromGunSO
    // this is where we'd store things from the GunSO that dont change, and that we will access lots of times, so we dont have to keep accessing the ScriptableObject.
    private CalibreSO calibre;
    private int magazineCapacity;
    private int fireRate; // rounds per minute
    private float tacReloadTime;
    private float emptyReloadTime;
    private GameObject calibrePrefab;
    private float muzzleVelocity;
    private float bulletWeight;

    private float verticalRecoil;
    private RecoilDirection recoilDirectionBias;
    private float horizontalRecoilDirection;
    private float horizontalRecoilDirectionVariation;
    private float recoveryDelay;
    private float recoveryRate;
    #endregion

    private void Awake()
    {
        playerInput = FindAnyObjectByType<PlayerInput>();
        gunMain = FindAnyObjectByType<GunMain>();
        aimTransform = Camera.main.transform;
        fireAction = playerInput.actions["Attack"];
        reloadAction = playerInput.actions["Reload"];
        gunAnimator = GetComponent<Animator>() ? GetComponent<Animator>() : null; // get the animator component if it exists, otherwise null.
        playerController = FindAnyObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (GunData == null || IsReloading)
        {
            return;
        }

        // (HandleSprintingAnimation();)
        // section here to handle sprinting animation.
        if (playerController.currentMovementState == MovementState.Sprinting || playerController.currentMovementState == MovementState.Running) 
            // soon enough, all guns will have animations, and we can remove the physical rep.
        {
            if (gunAnimator == null)
            {
                // no animator, use a physical representation of sprinting by moving and rotating the gun downwards.
            }
            else
            {
                gunAnimator.SetBool("IsSprinting", true);
            }
        }
        else
        {
            if (gunAnimator == null)
            {
                // no animator, use a physical representation of sprinting by moving and rotating the gun upwards.
            }
            else
            {
                gunAnimator.SetBool("IsSprinting", false);
            }
        }

        // cannot reload nor fire while sprinting, so we will not handle those inputs while sprinting.
        if (playerController.currentMovementState == MovementState.Sprinting || playerController.currentMovementState == MovementState.Running)
        {
            return;
        }
        HandleReloadInput();
        HandleFireInput();
    }

    public void EquipGun(GunSO gun)
    {
        GunData = gun;
        IsReloading = false;
        nextFireTime = 0f;

        magazineCapacity = gun.magazineCapacity;
        fireRate = gun.fireRate;
        tacReloadTime = gun.tacReloadTime;
        emptyReloadTime = gun.emptyReloadTime;
        muzzleVelocity = gun.muzzleVelocity;
        calibrePrefab = gun.calibre != null ? gun.calibre.calibrePrefab : null;
        calibre = gun.calibre;

        verticalRecoil = gun.verticalRecoil;
        recoilDirectionBias = gun.recoilDirectionBias;
        horizontalRecoilDirection = gun.horizontalRecoilDirection;
        horizontalRecoilDirectionVariation = gun.horizontalRecoilDirectionVariation;
        recoveryDelay = gun.recoveryDelay;
        recoveryRate = gun.recoveryRate;

        CurrentAmmo = magazineCapacity;

        currentFireMode = (gun.fireModes != null && gun.fireModes.Length > 0)
            ? gun.fireModes[0].fireMode
            : FireMode.SemiAuto;

        fireCooldown = fireRate > 0 ? 60f / fireRate : 0f;
    }

    private void HandleFireInput()
    {
        if (Time.time < nextFireTime)
        {
            return;
        }

        bool wantsToFire = currentFireMode == FireMode.FullAuto
            ? fireAction.IsPressed()
            : fireAction.WasPressedThisFrame();

        if (!wantsToFire)
        {
            return;
        }

        if (CurrentAmmo <= 0)
        {
            return;
        }

        Fire();
    }

    private void Fire()
    {
        CurrentAmmo--;
        nextFireTime = Time.time + fireCooldown;

        if (calibrePrefab != null && muzzleTransform != null)
        {
            Vector3 fireDirection = GetAimDirection();

            GameObject bulletInstance = Instantiate(calibrePrefab, muzzleTransform.position, Quaternion.LookRotation(fireDirection));
            BulletRuntime bulletRuntime = bulletInstance.GetComponent<BulletRuntime>();

            if (bulletRuntime != null)
            {
                bulletRuntime.Launch(fireDirection, muzzleVelocity, calibre);
            }
        }

        // recoil, muzzle flash, etc. go here in a later phase.
        // most of this would take place before spawning the bullet mind you.

        // recoil zone: // must be after spawning the bullet, to simluate kickback of the gun.
        if (playerController != null)
        {
            playerController.ApplyRecoil(verticalRecoil, horizontalRecoilDirection, horizontalRecoilDirectionVariation, recoilDirectionBias, recoveryDelay, recoveryRate);
        }
        // uses variables in the header of "Stability" to determine how much recoil is applied, and in what direction. (in GunSO)

        // spread zone: // must be before spawning the bullet, as it will affect the direction of the bullet.
        // [spread code]
    }

    private Vector3 GetAimDirection()
    {
        if (aimTransform == null)
        {
            return muzzleTransform.forward;
        }

        Vector3 targetPoint;

        if (Physics.Raycast(aimTransform.position, aimTransform.forward, out RaycastHit hit, maxAimDistance, ~ignoreLayers))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = aimTransform.position + aimTransform.forward * maxAimDistance;
        }

        return (targetPoint - muzzleTransform.position).normalized;
    }

    private void HandleReloadInput()
    {
        if (!reloadAction.WasPressedThisFrame())
        {
            return;
        }

        if (CurrentAmmo >= magazineCapacity)
        {
            return;
        }

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        IsReloading = true;

        float reloadTime = CurrentAmmo <= 0 ? emptyReloadTime : tacReloadTime;
        yield return new WaitForSeconds(reloadTime);

        CurrentAmmo = magazineCapacity;
        IsReloading = false;
    }
}
