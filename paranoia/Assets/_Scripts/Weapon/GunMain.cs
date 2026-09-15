using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

public class GunMain : MonoBehaviour
{
    public static GunMain Instance { get; private set; }

    [SerializeField] private TMP_Text ammoDisplay, fireModeDisplay; // UI text to display current ammo count

    [SerializeField] private PlayerInput playerInput;
    InputAction primaryAction; // 1 key to switch directly to primary weapon
    InputAction secondaryAction; // 2 key to switch directly to secondary weapon
    InputAction specialAction; // 3 key to switch directly to special weapon
    InputAction meleeAction; // x key to switch directly to melee weapon
    InputAction quickMeleeAction; // V key to melee
    InputAction switchWeaponAction; // Y on gamepad to switch to the next weapon in the loadout (primary -> secondary -> special -> primary)

    [SerializeField] Transform weaponSocket; // where the currently held gun is parented (e.g. hand/camera socket)
    [SerializeField] float switchDropDistance = 0.3f; // how far down (in local units) the gun drops while switching, purely a visual placeholder

    [Header("Loadout")]
    [SerializeField] GunSO primaryGunSO;
    [SerializeField] GunSO secondaryGunSO;
    [SerializeField] GunSO specialGunSO;

    // instantiated GunRuntime for each slot, created lazily the first time that slot is equipped.
    GunRuntime primaryGun, secondaryGun, specialGun;

    int currentSlot = 0; // 0 = melee/unarmed, 1 = primary, 2 = secondary, 3 = special
    public GunRuntime currentGun;

    bool isSwitching = false;

    private void Awake()
    {
        Instance = this;

        primaryAction = playerInput.actions["Primary"];
        secondaryAction = playerInput.actions["Secondary"];
        specialAction = playerInput.actions["Special"];
        meleeAction = playerInput.actions["Melee"];
        quickMeleeAction = playerInput.actions["QuickMelee"];
        switchWeaponAction = playerInput.actions["SwitchWeapon"];
    }

    private void Update()
    {
        if (Time.timeScale == 0) return; // Pause the game when timeScale is 0

        if (isSwitching) return; // ignore slot changes while a switch is already in progress

        if (primaryAction.triggered)
        {
            ChangeSlot(1);
        }
        else if (secondaryAction.triggered)
        {
            ChangeSlot(2);
        }
        else if (specialAction.triggered)
        {
            ChangeSlot(3);
        }
        //else if (meleeAction.triggered) // no MeleeSO exists yet.
        //{
        //    ChangeSlot(0);
        //}
        else if (quickMeleeAction.triggered)
        {
            // Logic for quick melee attack
            Debug.Log("Quick melee attack triggered");
        }
        else if (switchWeaponAction.triggered)
        {
            int nextSlot = (currentSlot + 1) % 4; // Cycle through slots 0-3
            if (GetGunSOForSlot(nextSlot) != null)
            {
                ChangeSlot(nextSlot);
            }
            else
            {
                // If the next slot is empty, find the next available slot
                for (int i = 1; i <= 3; i++)
                {
                    int checkSlot = (nextSlot + i) % 4;
                    if (GetGunSOForSlot(checkSlot) != null)
                    {
                        ChangeSlot(checkSlot);
                        break;
                    }
                }
            }
        }

        ammoDisplay.text = currentGun != null ? $"{currentGun.currentAmmo} / {currentGun.currentReserveAmmo}" : "0 / 0";
        fireModeDisplay.text = currentGun != null ? currentGun.currentFireMode.ToString() : "N/A";
    }

    public void ChangeSlot(int slot, bool forceRefresh = false)
    {
        if (slot == currentSlot && !forceRefresh) return; // already on this slot

        GunSO targetGunSO = GetGunSOForSlot(slot);
        if (slot != 0 && targetGunSO == null)
        {
            Debug.LogWarning($"No weapon assigned to slot: {slot}");
            return;
        }

        StartCoroutine(SwitchSlotRoutine(slot, targetGunSO));
    }

    IEnumerator SwitchSlotRoutine(int slot, GunSO targetGunSO)
    {
        isSwitching = true;

        // undeploy the currently held weapon, if any
        if (currentGun != null)
        {
            float undeployTime = currentGun.GunSO != null ? currentGun.GunSO.undeployTime : 0f;
            yield return AnimateLocalY(currentGun.transform, 0f, -switchDropDistance, undeployTime);
            currentGun.gameObject.SetActive(false);
        }

        currentSlot = slot;

        if (slot == 0 || targetGunSO == null)
        {
            currentGun = null;
        }
        else
        {
            currentGun = GetOrCreateGunRuntime(slot, targetGunSO);
            currentGun.transform.localPosition = new Vector3(0f, -switchDropDistance, 0f);
            currentGun.gameObject.SetActive(true);

            float deployTime = targetGunSO.deployTime;
            yield return AnimateLocalY(currentGun.transform, -switchDropDistance, 0f, deployTime);
        }

        isSwitching = false;
    }

    // Quick and dirty placeholder animation: moves a transform's local Y position from one value to another over time.
    IEnumerator AnimateLocalY(Transform target, float fromY, float toY, float duration)
    {
        if (duration <= 0f)
        {
            Vector3 instantPos = target.localPosition;
            instantPos.y = toY;
            target.localPosition = instantPos;
            yield break;
        }

        float elapsed = 0f;
        Vector3 pos = target.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            pos.y = Mathf.Lerp(fromY, toY, elapsed / duration);
            target.localPosition = pos;
            yield return null;
        }

        pos.y = toY;
        target.localPosition = pos;
    }

    GunSO GetGunSOForSlot(int slot)
    {
        switch (slot)
        {
            case 1: return primaryGunSO;
            case 2: return secondaryGunSO;
            case 3: return specialGunSO;
            default: return null;
        }
    }

    // Called when the player picks up a weapon (e.g. via int_WeaponPickup).
    // Assigns the gun to the appropriate slot based on its WeaponClass, replacing any existing gun in that slot.
    public void AddGun(GunSO gunSO)
    {
        if (gunSO == null) return;

        int slot = GetSlotForWeaponClass(gunSO.weaponClass);

        switch (slot)
        {
            case 1:
                if (primaryGun != null) Destroy(primaryGun.gameObject);
                primaryGun = null;
                primaryGunSO = gunSO;
                break;
            case 2:
                if (secondaryGun != null) Destroy(secondaryGun.gameObject);
                secondaryGun = null;
                secondaryGunSO = gunSO;
                break;
            case 3:
                if (specialGun != null) Destroy(specialGun.gameObject);
                specialGun = null;
                specialGunSO = gunSO;
                break;
            default:
                Debug.LogWarning($"Unable to determine a slot for weapon class: {gunSO.weaponClass}");
                return;
        }

        if (slot == currentSlot)
        {
            currentGun = null; // the gun we were holding for this slot was just destroyed above.
        }

        ChangeSlot(slot, forceRefresh: true);
    }

    int GetSlotForWeaponClass(WeaponClass weaponClass)
    {
        switch (weaponClass)
        {
            case WeaponClass.Pistol:
                return 2; // pistols go in the secondary slot
            case WeaponClass.Special:
            case WeaponClass.Wunderwaffe:
                return 3; // specials/wunderwaffe go in the special slot
            default:
                return 1; // everything else goes in the primary slot
        }
    }

    GunRuntime GetOrCreateGunRuntime(int slot, GunSO gunSO)
    {
        switch (slot)
        {
            case 1:
                if (primaryGun == null) primaryGun = InstantiateGun(gunSO);
                return primaryGun;
            case 2:
                if (secondaryGun == null) secondaryGun = InstantiateGun(gunSO);
                return secondaryGun;
            case 3:
                if (specialGun == null) specialGun = InstantiateGun(gunSO);
                return specialGun;
            default:
                return null;
        }
    }

    GunRuntime InstantiateGun(GunSO gunSO)
    {
        if (gunSO.gunPrefab == null)
        {
            Debug.LogError($"GunSO '{gunSO.gunName}' has no gunPrefab assigned.");
            return null;
        }

        GameObject gunObject = Instantiate(gunSO.gunPrefab, weaponSocket);
        gunObject.transform.localPosition = Vector3.zero;
        gunObject.transform.localRotation = Quaternion.identity;

        GunRuntime gunRuntime = gunObject.GetComponent<GunRuntime>();
        if (gunRuntime == null)
        {
            gunRuntime = gunObject.AddComponent<GunRuntime>();
        }

        gunRuntime.Initialize(gunSO, playerInput);
        gunObject.SetActive(false);

        return gunRuntime;
    }
}
