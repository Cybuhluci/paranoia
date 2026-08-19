using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CalibreSO", menuName = "Scriptable Objects/CalibreSO")]
public class CalibreSO : ScriptableObject
{
    public GameObject calibrePrefab; // the bullet that is fired from the guns.

    public string calibreName;
    public int baseDam; // damage per pellet
    public int falloffDam; // minimum damage per pellet (after falloff)
    public float weight; // in grammes
    public int penetration; // what level of object can the bullet penetrate at the right speed.
    public int Dynamics; // how aerodynamic is the bullet.
    public int pellets; // how many pellets are fired per shot (for shotguns)

    public ImpactPrefabs[] impactPrefabs; // different prefabs for different impact types (e.g. metal, wood, flesh, etc.)
}

[Serializable]
public class ImpactPrefabs
{
    public ImpactType impactType;
    public GameObject impactPrefab;
}