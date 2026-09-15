using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GunSO", menuName = "Scriptable Objects/GunSO")]
public class GunSO : ScriptableObject
{
    public GameObject gunPrefab;
    public string gunName;

    [Header("General Stats")]
    public WeaponClass weaponClass; 
    public CalibreSO calibre; // the calibre of the gun, which determines what ammo it uses.
    public int magazineCapacity; // number of rounds per magazine
    public bool usesMagazines; // if the gun uses magazines or not (if not, it uses a single ammo pool)
    public int maxReserveMags; // maximum number of magazines the player can carry for this gun
    public FireModeData[] fireModes; // the firemodes the gun has, and if they are select-fire or not.

    [Header("Handling Stats")]
    public float weight; // in kilograms
    public int fireRate; // rounds per minute
    public float tacReloadTime; // in seconds
    public float emptyReloadTime; // in seconds
    public float deployTime; // in seconds
    public float undeployTime; // in seconds
    public float ADSTime; // in seconds
    public float sprintToFireTime; // in seconds
    public float ADSSway; // in degrees
    public float headshotMultiplier; // how much damage is multiplied by when hitting the head

    [Header("Ballistics Stats")]
    public int muzzleVelocity; // in meters per second - how fast the bullet leaves the barrel of the gun

    [Header("Stability Stats")]
    public float verticalKick; // in degrees, how much the gun kicks up when firing
    public float horizontalKickDirectionBias; // in degrees, the centrepoint of the horizontal kick, where the variation changes it.
    public float horizontalKickDirectionVariation; // in degrees, how much the horizontal kick can vary from the centrepoint (this is a +- value)
    // example: bias = 1, variation = 2, then the horizontal kick can be anywhere from -1 (1deg left) to +3 (3deg right) degrees.
    public float ADSReductionMultiplier; // the multiplier for the recoil when aiming down sights (ADS)

    [Header("Spread Stats")] // spread calc: MD + (DD/shot * multipliers)
    public float semiautoDynamicDispersionMultiplier; 
    // ADS zone:
    public float adsMechanicalDispersion; // in degrees, the "base spread" for the first shot for ADS
    public float adsDynamicDispersion; // in degrees, the "base spread" for the first shot for ADS, but this is added to the mechanical dispersion to give a total spread value.
    public float adsDynamicDispersionRecoveryRate; // in degrees per second, how fast the dynamic dispersion recovers after the last shot for ADS
    public SpreadData adsStandMults;
    public SpreadData adsCrouchMults;
    public SpreadData adsProneMults;

    // Hip-Fire (HIP) zone:
    public float hipMechanicalDispersion; // in degrees, the "base spread" for the first shot for HIP
    public float hipDynamicDispersion; // in degrees, the "base spread" for the first shot for HIP, but this is added to the mechanical dispersion to give a total spread value.
    public float hipDynamicDispersionRecoveryRate; // in degrees per second, how fast the dynamic dispersion recovers after the last shot for HIP
    public SpreadData hipStandMults;
    public SpreadData hipCrouchMults;
    public SpreadData hipProneMults;
}

[Serializable]
public class FireModeData
{
    public FireMode fireMode;
    public bool isSelectFire; // says if the firemode is only accessible with the "select fire" attachment.
}

[Serializable]
public class SpreadData // in degrees.
{
    public float stillMultiplier; // the multiplier for the spread when the player is standing still
    public float movingMultiplier; // the multiplier for the spread when the player is moving
}