using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class GunRuntime : MonoBehaviour
// this script handles: shooting, reloading, ADS, and other gun-specific behaviors
{
    GunSO gunSO; // this gun's scriptable object / statistics
    public GunSO GunSO => gunSO;
    CalibreSO calibreSO => gunSO.calibre; // the calibre of the gun, which determines what ammo it uses.
    GameObject calibrePrefab => calibreSO.calibrePrefab; // the bullet prefab for this gun's calibre

    [SerializeField] private Animator gunAnimator; // list: Reload - trigger, IsAiming - bool, IsSprinting - bool.

    [SerializeField] private Transform muzzleTransform; // the transform of the muzzle of the gun, where the bullets will be spawned from.

    [SerializeField] private PlayerInput playerInput;
    InputAction fireAction; // left mouse button or right trigger on gamepad
    InputAction reloadAction; // R key or X button on XBOX
    InputAction aimAction; // right mouse button or left trigger on gamepad
    InputAction selectFireAction; // B key or DPAD down on XBOX
    // - select fire function is locked behind the select fire attachment, but for now just let the gun use it freely as no attachments exist yet.

    [SerializeField] private PlayerController playerController; // used to read stance (stand/crouch/prone) and movement state for spread calculations.
    [SerializeField] private PlayerCamera playerCamera; // used to apply recoil (camera kick) after firing.

    bool isAiming;
    float currentDynamicDispersion; // degrees, builds up per shot and recovers over time

    public int currentAmmo { get; private set; } // current ammo in the magazine
    public int currentReserveAmmo { get; private set; } // current ammo in reserve (not in the magazine)
                                                        // - this is just going to be changed soon, so dont get too attached to it.

    public bool isReloading { get; private set; }
    public FireMode currentFireMode { get; private set; }

    int currentFireModeIndex;
    float timeBetweenShots; // seconds between individual shots, derived from fireRate (RPM)
    float nextFireTime; // Time.time value at which the gun is allowed to fire again
    bool isBursting;

    [SerializeField] float burstShotInterval = 0.08f; // seconds between individual shots within a burst, separate from fireRate

    #region all of the GunSO variables made local so the script doesnt need to reference the scriptable object every time it needs a value

    int magazineCapacity;
    bool usesMagazines;
    float tacReloadTime; // non-empty magazine reload time (seconds)
    float emptyReloadTime; // empty magazine reload time (seconds)
    int fireRate; // RPM - rounds per minute
    FireModeData[] fireModeData;

    float semiautoDynamicDispersionMultiplier;

    float adsMechanicalDispersion;
    float adsDynamicDispersion;
    float adsDynamicDispersionRecoveryRate;
    SpreadData adsStandMults;
    SpreadData adsCrouchMults;
    SpreadData adsProneMults;

    float hipMechanicalDispersion;
    float hipDynamicDispersion;
    float hipDynamicDispersionRecoveryRate;
    SpreadData hipStandMults;
    SpreadData hipCrouchMults;
    SpreadData hipProneMults;

    float verticalKick;
    float horizontalKickDirectionBias;
    float horizontalKickDirectionVariation;
    float ADSReductionMultiplier;

    void updateLocalVariables()
    {
        magazineCapacity = gunSO.magazineCapacity;
        usesMagazines = gunSO.usesMagazines;
        tacReloadTime = gunSO.tacReloadTime;
        emptyReloadTime = gunSO.emptyReloadTime;
        fireRate = gunSO.fireRate;
        fireModeData = gunSO.fireModes;

        timeBetweenShots = fireRate > 0 ? 60f / fireRate : 0f;

        currentFireModeIndex = 0;
        currentFireMode = (fireModeData != null && fireModeData.Length > 0) ? fireModeData[0].fireMode : FireMode.SemiAuto;

        currentAmmo = magazineCapacity;
        currentReserveAmmo = usesMagazines ? magazineCapacity * gunSO.maxReserveMags : gunSO.maxReserveMags;

        semiautoDynamicDispersionMultiplier = gunSO.semiautoDynamicDispersionMultiplier;

        adsMechanicalDispersion = gunSO.adsMechanicalDispersion;
        adsDynamicDispersion = gunSO.adsDynamicDispersion;
        adsDynamicDispersionRecoveryRate = gunSO.adsDynamicDispersionRecoveryRate;
        adsStandMults = gunSO.adsStandMults;
        adsCrouchMults = gunSO.adsCrouchMults;
        adsProneMults = gunSO.adsProneMults;

        hipMechanicalDispersion = gunSO.hipMechanicalDispersion;
        hipDynamicDispersion = gunSO.hipDynamicDispersion;
        hipDynamicDispersionRecoveryRate = gunSO.hipDynamicDispersionRecoveryRate;
        hipStandMults = gunSO.hipStandMults;
        hipCrouchMults = gunSO.hipCrouchMults;
        hipProneMults = gunSO.hipProneMults;

        verticalKick = gunSO.verticalKick;
        horizontalKickDirectionBias = gunSO.horizontalKickDirectionBias;
        horizontalKickDirectionVariation = gunSO.horizontalKickDirectionVariation;
        ADSReductionMultiplier = gunSO.ADSReductionMultiplier;
    }

    #endregion

    private void Start()
    {
        if (gunAnimator == null)
            gunAnimator = GetComponent<Animator>();

        if (muzzleTransform == null)
            muzzleTransform = transform.Find("Model").Find("MuzzleTransform");

        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>();

        if (playerCamera == null)
            playerCamera = GetComponentInParent<PlayerCamera>();
    }

    public void Initialize(GunSO gunSO, PlayerInput input)
    {
        this.gunSO = gunSO;
        playerInput = input;
        fireAction = playerInput.actions["Attack"];
        reloadAction = playerInput.actions["Reload"];
        aimAction = playerInput.actions["ADS"];
        selectFireAction = playerInput.actions["SelectFire"];
        updateLocalVariables();
    }

    private void Update()
    {
        UpdateDynamicDispersion();

        if (aimAction != null)
            isAiming = aimAction.IsPressed();

        if (isReloading) return;

        if (selectFireAction != null && selectFireAction.triggered)
        {
            CycleFireMode();
        }

        if (reloadAction != null && reloadAction.triggered && currentAmmo < magazineCapacity && currentReserveAmmo > 0)
        {
            StartCoroutine(ReloadRoutine());
            return;
        }

        if (fireAction == null) return;

        switch (currentFireMode)
        {
            case FireMode.SemiAuto:
                if (fireAction.triggered) TryFire();
                break;
            case FireMode.FullAuto:
                if (fireAction.IsPressed()) TryFire();
                break;
            case FireMode.Burst:
                if (fireAction.triggered && !isBursting && Time.time >= nextFireTime) StartCoroutine(BurstRoutine(3));
                break;
            case FireMode.Hyperburst:
                if (fireAction.triggered && !isBursting && Time.time >= nextFireTime) StartCoroutine(BurstRoutine(2));
                break;
        }
    }

    void CycleFireMode()
    {
        if (fireModeData == null || fireModeData.Length <= 1) return;

        currentFireModeIndex = (currentFireModeIndex + 1) % fireModeData.Length;
        currentFireMode = fireModeData[currentFireModeIndex].fireMode;
    }

    IEnumerator BurstRoutine(int shotCount)
    {
        isBursting = true;

        for (int i = 0; i < shotCount; i++)
        {
            if (currentAmmo <= 0) break;

            ForceFire();
            yield return new WaitForSeconds(burstShotInterval);
        }

        nextFireTime = Time.time + timeBetweenShots;
        isBursting = false;
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;

        if (gunAnimator != null)
            gunAnimator.SetTrigger("Reload");

        bool isEmptyReload = currentAmmo <= 0;
        float reloadTime = isEmptyReload ? emptyReloadTime : tacReloadTime;

        if (reloadTime > 0f)
            yield return new WaitForSeconds(reloadTime);

        int ammoNeeded = magazineCapacity - currentAmmo;
        int ammoToLoad = Mathf.Min(ammoNeeded, currentReserveAmmo);

        currentAmmo += ammoToLoad;
        currentReserveAmmo -= ammoToLoad;

        isReloading = false;
    }

    void TryFire()
    {
        if (Time.time < nextFireTime) return;
        if (currentAmmo <= 0) return;

        Fire();

        currentAmmo--;
        nextFireTime = Time.time + timeBetweenShots;
    }

    // Fires a single shot without checking/updating the RPM-based fire-rate gate.
    // Used inside burst/hyperburst sequences, which have their own internal shot spacing (burstShotInterval).
    void ForceFire()
    {
        if (currentAmmo <= 0) return;

        Fire();

        currentAmmo--;
    }

    public void Fire()
    {
        if (calibrePrefab == null || muzzleTransform == null) return;

        float spreadAngle = CalculateSpreadAngle();
        Vector3 fireDirection = ApplySpread(muzzleTransform.forward, spreadAngle);

        GameObject bulletObject = Instantiate(calibrePrefab, muzzleTransform.position, Quaternion.LookRotation(fireDirection));
        CalibreRuntime calibreRuntime = bulletObject.GetComponent<CalibreRuntime>();

        if (calibreRuntime == null)
        {
            calibreRuntime = bulletObject.AddComponent<CalibreRuntime>();
        }

        calibreRuntime.Launch(fireDirection, gunSO.muzzleVelocity, calibreSO);

        AddDynamicDispersion();
        ApplyRecoil();
    }

    // Kicks the camera's rotation after firing. Purely visual - does not touch the gun model at all.
    void ApplyRecoil()
    {
        if (playerCamera == null) return;

        float recoilMultiplier = isAiming ? ADSReductionMultiplier : 1f;

        float verticalRecoil = verticalKick * recoilMultiplier;
        float horizontalRecoil = (horizontalKickDirectionBias + Random.Range(-horizontalKickDirectionVariation, horizontalKickDirectionVariation)) * recoilMultiplier;

        playerCamera.ApplyRecoil(verticalRecoil, horizontalRecoil);
    }

    // spread calc: MD + (DD/shot * multipliers)
    float CalculateSpreadAngle()
    {
        float mechanicalDispersion = isAiming ? adsMechanicalDispersion : hipMechanicalDispersion;
        SpreadData stanceMults = GetStanceMultipliers();
        float movementMultiplier = (playerController != null && playerController.IsMoving) ? stanceMults.movingMultiplier : stanceMults.stillMultiplier;

        return mechanicalDispersion + (currentDynamicDispersion * movementMultiplier);
    }

    SpreadData GetStanceMultipliers()
    {
        SpreadData standMults = isAiming ? adsStandMults : hipStandMults;
        SpreadData crouchMults = isAiming ? adsCrouchMults : hipCrouchMults;
        SpreadData proneMults = isAiming ? adsProneMults : hipProneMults;

        if (playerController == null) return standMults;

        if (playerController.IsProne) return proneMults;
        if (playerController.IsCrouching) return crouchMults;
        return standMults;
    }

    void AddDynamicDispersion()
    {
        float dynamicDispersionPerShot = isAiming ? adsDynamicDispersion : hipDynamicDispersion;

        if (currentFireMode == FireMode.SemiAuto)
            dynamicDispersionPerShot *= semiautoDynamicDispersionMultiplier;

        currentDynamicDispersion += dynamicDispersionPerShot;
    }

    void UpdateDynamicDispersion()
    {
        float recoveryRate = isAiming ? adsDynamicDispersionRecoveryRate : hipDynamicDispersionRecoveryRate;
        currentDynamicDispersion = Mathf.Max(0f, currentDynamicDispersion - (recoveryRate * Time.deltaTime));
    }

    // Rotates baseDirection by a random amount within a cone of the given angle (in degrees).
    Vector3 ApplySpread(Vector3 baseDirection, float spreadAngleDegrees)
    {
        if (spreadAngleDegrees <= 0f) return baseDirection;

        Vector2 randomOffset = Random.insideUnitCircle * spreadAngleDegrees;

        // build the spread rotation relative to baseDirection's own local frame (not world space),
        // so pitch (vertical) and yaw (horizontal) offsets are always correct regardless of aim direction.
        Quaternion baseRotation = Quaternion.LookRotation(baseDirection);
        Quaternion spreadOffset = Quaternion.Euler(-randomOffset.y, randomOffset.x, 0f);

        return baseRotation * spreadOffset * Vector3.forward;
    }
}
