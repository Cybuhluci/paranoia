using UnityEngine;
using UnityEngine.InputSystem;

public enum WeaponSlot
{
    Primary,
    Secondary
}

public class GunMain : MonoBehaviour
{
    // this script is used to handle the main behavior of the gun, such as switching between different guns, and managing the gun's state.
    // it will be attached to the player and will be responsible for managing the gun's behavior during gameplay.

    [Header("These Things")] // set in the inspector.
    // below: references to other scripts, in-case they are needed.
    [SerializeField] private PlayerController playerController; // reference to the PlayerController script, in-case it is needed.
    // below: required inspector references.
    [SerializeField] private PlayerInput playerInput; // reference to the PlayerInput component for input handling
    [SerializeField] private Transform weaponParent; // child of the camera, where the gun prefab gets instantiated.
    [SerializeField] private GunSO startingGun; // the colt 1911 for v1.0.0.

    private const int MaxInventorySize = 2; // primary and secondary.
    private readonly GunSO[] inventory = new GunSO[MaxInventorySize];

    private GameObject currentGunInstance;
    private GunRuntime gunRuntime;

    public GunSO CurrentGun { get; private set; }
    public WeaponSlot CurrentSlot { get; private set; }

    private void Awake()
    {
        if (startingGun != null)
        {
            AddGun(startingGun, WeaponSlot.Primary);
            EquipSlot(WeaponSlot.Primary);
        }
    }

    public bool AddGun(GunSO gun, WeaponSlot slot)
    {
        if (gun == null)
        {
            return false;
        }

        inventory[(int)slot] = gun;
        return true;
    }

    public void EquipSlot(WeaponSlot slot)
    {
        GunSO gun = inventory[(int)slot];

        if (gun == null || gun.gunPrefab == null || weaponParent == null)
        {
            return;
        }

        CurrentSlot = slot;
        CurrentGun = gun;

        if (currentGunInstance != null)
        {
            Destroy(currentGunInstance);
        }

        currentGunInstance = Instantiate(gun.gunPrefab, weaponParent);
        currentGunInstance.transform.localPosition = Vector3.zero;
        currentGunInstance.transform.localRotation = Quaternion.identity;

        gunRuntime = currentGunInstance.GetComponent<GunRuntime>();

        if (gunRuntime != null)
        {
            gunRuntime.EquipGun(gun);
        }
    }

    public GunSO GetGunInSlot(WeaponSlot slot)
    {
        return inventory[(int)slot];
    }
}
