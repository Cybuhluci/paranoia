using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GunSO", menuName = "Scriptable Objects/GunSO")]
public class GunSO : ScriptableObject
{
    public GameObject gunPrefab;

    public string gunName;
    public CalibreSO calibre;
    public int magazineCapacity;
    public int maxReserveMags; // maximum number of magazines the player can carry for this gun
    public FireModeData[] fireModes;

    [Header("Handling Stats")]
    public int fireRate; // rounds per minute
    public float tacReloadTime; // in seconds
    public float emptyReloadTime; // in seconds
    public float deployTime; // in seconds
    public float undeployTime; // in seconds
    public float ADSTime; // in seconds
    public float sprintToFireTime; // in seconds
    public float ADSSway; // in degrees

    [Header("Ballistics Stats")]
    public int muzzleVelocity; // in meters per second
    public int falloutMin; // in metres (the point where the damage starts to fall off)
    public int falloffMax; // in metres (the point where the damage goes to the minimum damage)

    [Header("Stability Stats")] 
    public float verticalRecoil; // in degrees, how much the gun kicks up when firing on every shot
    public RecoilDirection recoilDirectionBias; // the direction of the first shot's recoil.
    // as in, when firing, all varations for recoil are calculated from bias+direction first.
    // so if we had 5deg right bias, and a 5deg variation - the first shot always goes 5deg right, and the next shots can go anywhere between 0deg and 10deg
    // (with the centre point being that 5deg right)
    public float horizontalRecoilDirection; // how far the bias is, in degrees (e.g: 10 degrees means the first shot moves 10 degrees to the right always)
    public float horizontalRecoilDirectionVariation; // how much the bias can vary up and down when firing
    // - this is random variation added to recoil after the first shot, so the first shot is always the bias, and the next shots can vary from that bias by this amount up or down.
    // (e.g: 10 degrees bias with 5 degrees variation means the bias can be between 5 and 15 degrees)
    // in degrees.
    public float recoveryDelay; // in seconds
    public float recoveryRate; // in degrees per second

    [Header("Accuracy/Spread Stats")] // assume everything is in degrees
    // not to be confused with recoil, which is the movement of the gun when firing, spread is the accuracy of the gun when firing.
    // ADS zone:
    public SpreadData ads_standing;
    public SpreadData ads_crouching;
    public SpreadData ads_prone;
    public float spreadGrowthADS; // how much the spread grows when firing in ADS, in degrees per shot
    public float spreadRecoveryADS; // how much the spread recovers when not firing in ADS, in degrees per second

    // Hip-Fire zone:
    public SpreadData hip_standing;
    public SpreadData hip_crouching;
    public SpreadData hip_prone;
    public float spreadGrowthHip; // how much the spread grows when firing in Hip-Fire, in degrees per shot
    public float spreadRecoveryHip; // how much the spread recovers when not firing in Hip-Fire, in degrees per second
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
    public float stillSpread; // in degrees, how much the spread is when not moving
    public float movingSpread; // in degrees, how much the spread is when moving
}