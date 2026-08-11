using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum WeaponSlot
{
    Primary,
    Secondary,
    Tertiary // added for future expansion, but not currently used in v1.0.0
}

public class GunMain : MonoBehaviour
{
    public static GunMain Instance { get; private set; } // singleton instance
    // this script is used to handle the main behavior of the gun, such as switching between different guns, and managing the gun's state.
    // it will be attached to the player and will be responsible for managing the gun's behavior during gameplay.

    [Header("These Things")] // set in the inspector.
    // below: references to other scripts, in-case they are needed.
    [SerializeField] private PlayerController playerController; // reference to the PlayerController script, in-case it is needed.
    // below: required inspector references.
    [SerializeField] private PlayerInput playerInput; // reference to the PlayerInput component for input handling
    [SerializeField] private Transform weaponParent; // child of the camera, where the gun prefab gets instantiated.
    [SerializeField] private GunSO startingGun; // the colt 1911 for v1.0.0.
    [SerializeField] private TMP_Text ammoDisplay; // reference to the ammo display text component.

    private const int MaxInventorySize = 2; // primary and secondary.
    [SerializeField] private GunSO[] inventory = new GunSO[MaxInventorySize]; // readonly property removed temporarily for testing purposes, will be re-added later.

    private readonly GameObject[] gunInstances = new GameObject[MaxInventorySize]; // one instantiated prefab per slot, kept alive (just hidden) once created.
    private readonly GunRuntime[] gunRuntimes = new GunRuntime[MaxInventorySize];

    private GameObject currentGunInstance;
    private GunRuntime gunRuntime;

    private bool isSwitching;

    private InputAction previousWeaponAction;
    private InputAction nextWeaponAction;

    public GunSO CurrentGun { get; private set; }
    public WeaponSlot CurrentSlot { get; private set; }

    private bool isDisabled; // true once weapons have been stripped from the player (e.g. downed/game over) - blocks switching and hides the held gun.

    private void Awake()
    {
        previousWeaponAction = playerInput.actions["Previous"];
        nextWeaponAction = playerInput.actions["Next"];

        if (startingGun != null)
        {
            AddGun(startingGun);
            DeploySlot(WeaponSlot.Primary);
        }

        Instance = this;
    }

    public bool AddGun(GunSO gun)
    {
        if (gun == null)
        {
            return false;
        }

        // prefer an empty slot first.
        for (int i = 0; i < MaxInventorySize; i++)
        {
            if (inventory[i] == null)
            {
                inventory[i] = gun;
                SpawnGunInstance(i, gun);
                DeploySlot((WeaponSlot)i);
                return true;
            }
        }

        // no empty slots - replace whatever gun is currently equipped/in-hand.
        int slotIndex = (int)CurrentSlot;
        inventory[slotIndex] = gun;

        if (gunInstances[slotIndex] != null)
        {
            Destroy(gunInstances[slotIndex]);
            gunInstances[slotIndex] = null;
            gunRuntimes[slotIndex] = null;
        }

        SpawnGunInstance(slotIndex, gun);
        DeploySlot(CurrentSlot);
        return true;
    }

    private void SpawnGunInstance(int slotIndex, GunSO gun)
    {
        if (gun.gunPrefab == null || weaponParent == null)
        {
            return;
        }

        GameObject instance = Instantiate(gun.gunPrefab, weaponParent);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.SetActive(false);

        GunRuntime runtime = instance.GetComponent<GunRuntime>();

        if (runtime != null)
        {
            runtime.EquipGun(gun);
        }

        gunInstances[slotIndex] = instance;
        gunRuntimes[slotIndex] = runtime;
    }

    public void DeploySlot(WeaponSlot slot)
    {
        if (isSwitching || slot == CurrentSlot && currentGunInstance != null)
        {
            return;
        }

        int slotIndex = (int)slot;
        GunSO gun = inventory[slotIndex];

        if (gun == null || gunInstances[slotIndex] == null)
        {
            return;
        }

        StartCoroutine(SwitchWeaponRoutine(slot));
    }

    private IEnumerator SwitchWeaponRoutine(WeaponSlot slot)
    {
        isSwitching = true;

        int newSlotIndex = (int)slot;
        GunSO newGun = inventory[newSlotIndex];
        GunRuntime newGunRuntime = gunRuntimes[newSlotIndex];

        if (newGunRuntime != null)
        {
            newGunRuntime.SetSwitching(true);
        }

        // undeploy the currently held gun, if any, using its own undeployTime.
        if (currentGunInstance != null)
        {
            float undeployTime = CurrentGun != null ? CurrentGun.undeployTime : 0f;

            if (gunRuntime != null)
            {
                gunRuntime.SetSwitching(true);
                gunRuntime.CancelReload();
            }

            if (undeployTime > 0f)
            {
                yield return new WaitForSeconds(undeployTime);
            }

            currentGunInstance.SetActive(false);
        }

        CurrentSlot = slot;
        CurrentGun = newGun;
        currentGunInstance = gunInstances[newSlotIndex];
        gunRuntime = newGunRuntime;

        currentGunInstance.SetActive(true);

        // deploy the newly equipped gun using its own deployTime before it can be used.
        float deployTime = newGun != null ? newGun.deployTime : 0f;

        if (deployTime > 0f)
        {
            yield return new WaitForSeconds(deployTime);
        }

        if (gunRuntime != null)
        {
            gunRuntime.SetSwitching(false);
        }

        isSwitching = false;
    }

    public GunSO GetGunInSlot(WeaponSlot slot)
    {
        return inventory[(int)slot];
    }

    // called when the player is downed/game-over'd - hides whatever gun is currently held and blocks switching until re-enabled.
    public void DisableWeapons()
    {
        isDisabled = true;
        StopAllCoroutines();
        isSwitching = false;

        if (currentGunInstance != null)
        {
            currentGunInstance.SetActive(false);
        }

        gunRuntime = null;
    }

    private void Update()
    {
        ammoDisplay.text = gunRuntime != null ? $"{gunRuntime.CurrentAmmo}/{gunRuntime.ReserveAmmo}" : "No Gun";

        if (isDisabled || isSwitching)
        {
            return;
        }

        if (previousWeaponAction.WasPressedThisFrame())
        {
            if (CurrentSlot == WeaponSlot.Primary && inventory[(int)WeaponSlot.Secondary] != null)
            {
                DeploySlot(WeaponSlot.Secondary);
            }
            else if (CurrentSlot == WeaponSlot.Secondary && inventory[(int)WeaponSlot.Primary] != null)
            {
                DeploySlot(WeaponSlot.Primary);
            }
        }
        else if (nextWeaponAction.WasPressedThisFrame())
        {
            if (CurrentSlot == WeaponSlot.Secondary && inventory[(int)WeaponSlot.Primary] != null)
            {
                DeploySlot(WeaponSlot.Primary);
            }
            else if (CurrentSlot == WeaponSlot.Primary && inventory[(int)WeaponSlot.Secondary] != null)
            {
                DeploySlot(WeaponSlot.Secondary);
            }
        }
    }
}
