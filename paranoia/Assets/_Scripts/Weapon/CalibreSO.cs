using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CalibreSO", menuName = "Scriptable Objects/CalibreSO")]
public class CalibreSO : ScriptableObject
{
    public GameObject calibrePrefab; // the bullet that is fired from the guns.

    public string calibreName;
    public int damage; // damage per pellet
    public int minDamage; // minimum damage per pellet (after falloff)
    public float weight; // in grammes
    public int penetration; // what level of object can the bullet penetrate at the right speed.
    public float headshotMultiplier; // how much damage is multiplied by when hitting the head.
    public int pellets; // how many pellets are fired per shot (for shotguns)

    public ImpactPrefabs[] impactPrefabs; // different prefabs for different impact types (e.g. metal, wood, flesh, etc.)
}

[Serializable]
public class ImpactPrefabs
{
    public ImpactType impactType;
    public GameObject impactPrefab;
}