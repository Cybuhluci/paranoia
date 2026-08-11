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
    private InputAction adsAction;

    public GunSO GunData { get; private set; }
    public bool IsReloading { get; private set; }
    public bool IsSwitching { get; private set; } // set by GunMain while this gun is being deployed/undeployed - blocks firing.

    public void SetSwitching(bool switching)
    {
        IsSwitching = switching;
    }

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

    private SpreadData adsStanding;
    private SpreadData adsCrouching;
    private SpreadData adsProne;
    private float spreadGrowthADS;
    private float spreadRecoveryADS;

    private SpreadData hipStanding;
    private SpreadData hipCrouching;
    private SpreadData hipProne;
    private float spreadGrowthHip;
    private float spreadRecoveryHip;
    #endregion

    private float currentSpread; // in degrees, accumulates while firing and recovers over time.

    private Vector3 gunModelDefaultPosition;
    private Quaternion gunModelDefaultRotation;

    [SerializeField] private Vector3 sprintPositionOffset = new Vector3(0f, -0.15f, 0f);
    [SerializeField] private Vector3 sprintRotationOffset = new Vector3(35f, -15f, 0f);
    [SerializeField] private float sprintPoseSpeed = 8f;

    private bool isReloadSpinning;
    private Coroutine reloadCoroutine;

    private void Awake()
    {
        playerInput = FindAnyObjectByType<PlayerInput>();
        gunMain = FindAnyObjectByType<GunMain>();
        aimTransform = Camera.main.transform;
        fireAction = playerInput.actions["Attack"];
        reloadAction = playerInput.actions["Reload"];
        adsAction = playerInput.actions["ADS"];
        gunAnimator = GetComponent<Animator>() ? GetComponent<Animator>() : null; // get the animator component if it exists, otherwise null.
        playerController = FindAnyObjectByType<PlayerController>();

        if (gunModelTransorm != null)
        {
            gunModelDefaultPosition = gunModelTransorm.localPosition;
            gunModelDefaultRotation = gunModelTransorm.localRotation;
        }
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
                // use gunModelTransform to move and rotate the gun downwards, and then back up when not sprinting.
                if (gunModelTransorm != null && !isReloadSpinning)
                {
                    Vector3 targetPosition = gunModelDefaultPosition + sprintPositionOffset;
                    Quaternion targetRotation = gunModelDefaultRotation * Quaternion.Euler(sprintRotationOffset);

                    gunModelTransorm.localPosition = Vector3.Lerp(gunModelTransorm.localPosition, targetPosition, sprintPoseSpeed * Time.deltaTime);
                    gunModelTransorm.localRotation = Quaternion.Slerp(gunModelTransorm.localRotation, targetRotation, sprintPoseSpeed * Time.deltaTime);
                }
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
                // use gunModelTransform to move and rotate the gun downwards, and then back up when not sprinting.
                if (gunModelTransorm != null && !isReloadSpinning)
                {
                    gunModelTransorm.localPosition = Vector3.Lerp(gunModelTransorm.localPosition, gunModelDefaultPosition, sprintPoseSpeed * Time.deltaTime);
                    gunModelTransorm.localRotation = Quaternion.Slerp(gunModelTransorm.localRotation, gunModelDefaultRotation, sprintPoseSpeed * Time.deltaTime);
                }
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

        HandleAimInput();
        HandleSpreadRecovery();
        HandleReloadInput();
        HandleFireInput();
    }

    private void HandleAimInput()
    {
        if (playerController == null)
        {
            return;
        }

        playerController.SetAiming(adsAction.IsPressed());
    }

    private void HandleSpreadRecovery()
    {
        float recoveryRateToUse = playerController != null && playerController.IsAiming ? spreadRecoveryADS : spreadRecoveryHip;
        currentSpread = Mathf.MoveTowards(currentSpread, 0f, recoveryRateToUse * Time.deltaTime);
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

        adsStanding = gun.ads_standing;
        adsCrouching = gun.ads_crouching;
        adsProne = gun.ads_prone;
        spreadGrowthADS = gun.spreadGrowthADS;
        spreadRecoveryADS = gun.spreadRecoveryADS;

        hipStanding = gun.hip_standing;
        hipCrouching = gun.hip_crouching;
        hipProne = gun.hip_prone;
        spreadGrowthHip = gun.spreadGrowthHip;
        spreadRecoveryHip = gun.spreadRecoveryHip;

        currentSpread = 0f;

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

        // cannot fire while the weapon is being deployed/undeployed.
        if (IsSwitching)
        {
            return;
        }

        // cannot fire while downed.
        if (playerController != null && playerController.IsDowned)
        {
            return;
        }

        // cannot fire while moving in prone.
        if (playerController != null && playerController.currentStanceState == StanceState.Proning && playerController.IsMoving)
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

        // spread zone: // must be before spawning the bullet, as it will affect the direction of the bullet.
        // uses variables in the header of "Accuracy/Spread" to determine how much spread is applied, and in what direction. (in GunSO)
        // all this needs to actually end up doing changing the rotation of the muzzleTransform for every bullet.
        float baseSpread = GetBaseSpread();
        float totalSpread = baseSpread + currentSpread;

        // bullet spawning zone: // must be after spread, as it will affect the direction of the bullet.
        if (calibrePrefab != null && muzzleTransform != null)
        {
            Vector3 fireDirection = ApplySpread(GetAimDirection(), totalSpread);

            GameObject bulletInstance = Instantiate(calibrePrefab, muzzleTransform.position, Quaternion.LookRotation(fireDirection));
            BulletRuntime bulletRuntime = bulletInstance.GetComponent<BulletRuntime>();

            if (bulletRuntime != null)
            {
                bulletRuntime.Launch(fireDirection, muzzleVelocity, calibre);
            }
        }

        // every shot grows accumulated spread, which recovers over time in HandleSpreadRecovery().
        bool isAiming = playerController != null && playerController.IsAiming;
        currentSpread += isAiming ? spreadGrowthADS : spreadGrowthHip;

        // recoil, muzzle flash, etc. go here in a later phase.
        // most of this would take place before spawning the bullet mind you.

        // recoil zone: // must be after spawning the bullet, to simluate kickback of the gun.
        if (playerController != null)
        {
            playerController.ApplyRecoil(verticalRecoil, horizontalRecoilDirection, horizontalRecoilDirectionVariation, recoilDirectionBias, recoveryDelay, recoveryRate);
        }
        // uses variables in the header of "Stability" to determine how much recoil is applied, and in what direction. (in GunSO)
    }

    private float GetBaseSpread()
    {
        bool isAiming = playerController != null && playerController.IsAiming;
        bool isMoving = playerController != null && playerController.IsMoving;
        StanceState stance = playerController != null ? playerController.currentStanceState : StanceState.Standing;

        SpreadData spreadData = isAiming
            ? stance switch
            {
                StanceState.Crouching => adsCrouching,
                StanceState.Proning => adsProne,
                _ => adsStanding
            }
            : stance switch
            {
                StanceState.Crouching => hipCrouching,
                StanceState.Proning => hipProne,
                _ => hipStanding
            };

        if (spreadData == null)
        {
            return 0f;
        }

        return isMoving ? spreadData.movingSpread : spreadData.stillSpread;
    }

    private Vector3 ApplySpread(Vector3 direction, float spreadDegrees)
    {
        if (spreadDegrees <= 0f)
        {
            return direction;
        }

        // pick a random point within a cone of "spreadDegrees" around the direction.
        float randomAngle = Random.Range(0f, spreadDegrees);
        float randomRotation = Random.Range(0f, 360f);

        Quaternion spreadRotation = Quaternion.AngleAxis(randomRotation, direction) * Quaternion.AngleAxis(randomAngle, Vector3.Cross(direction, Vector3.up).normalized == Vector3.zero ? Vector3.right : Vector3.Cross(direction, Vector3.up).normalized);

        return spreadRotation * direction;
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
    // temporary reload animation: spin gun on z-axis for as long as the reload time is
    // - the gun does a full 360 degree spin in that time, and then the ammo is refilled.
    // we use "gunModelTransform" to rotate the gun model, and then reset it back to its original rotation after the reload is done.
    {
        if (!reloadAction.WasPressedThisFrame())
        {
            return;
        }

        if (CurrentAmmo >= magazineCapacity)
        {
            return;
        }

        reloadCoroutine = StartCoroutine(ReloadRoutine());
    }

    // Called by GunMain when the player switches away from this gun mid-reload -
    // the reload is cancelled entirely and must be started again from scratch.
    public void CancelReload()
    {
        if (!IsReloading)
        {
            return;
        }

        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        IsReloading = false;
        isReloadSpinning = false;

        if (gunModelTransorm != null)
        {
            gunModelTransorm.localRotation = gunModelDefaultRotation;
        }
    }

    private IEnumerator ReloadRoutine()
    {
        IsReloading = true;

        float reloadTime = CurrentAmmo <= 0 ? emptyReloadTime : tacReloadTime;

        if (gunModelTransorm != null && gunAnimator == null)
        {
            yield return ReloadSpinRoutine(reloadTime);
        }
        else
        {
            yield return new WaitForSeconds(reloadTime);
        }

        CurrentAmmo = magazineCapacity;
        IsReloading = false;
        reloadCoroutine = null;
    }

    private IEnumerator ReloadSpinRoutine(float reloadTime)
    {
        isReloadSpinning = true;

        float elapsed = 0f;

        while (elapsed < reloadTime)
        {
            elapsed += Time.deltaTime;
            float spinProgress = Mathf.Clamp01(elapsed / reloadTime);
            float yawAngle = spinProgress * 360f;

            gunModelTransorm.localRotation = gunModelDefaultRotation * Quaternion.Euler(0f, 0f, yawAngle);

            yield return null;
        }

        gunModelTransorm.localRotation = gunModelDefaultRotation;
        isReloadSpinning = false;
    }
}
