using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GunSOGenerator : EditorWindow
{
    private TextAsset csvFile;
    private string targetFolderPath = "Assets/ScriptableObjects/Guns";

    [MenuItem("Tools/Gun SO Batch Generator")]
    public static void ShowWindow()
    {
        GetWindow<GunSOGenerator>("Gun SO Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Generate GunSO Assets", EditorStyles.boldLabel);
        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);
        targetFolderPath = EditorGUILayout.TextField("Output Folder", targetFolderPath);

        if (GUILayout.Button("Generate / Update Guns"))
        {
            if (csvFile == null)
            {
                Debug.LogError("Assign a CSV file first!");
                return;
            }
            GenerateGuns();
        }
    }

    private void GenerateGuns()
    {
        if (!Directory.Exists(targetFolderPath))
        {
            Directory.CreateDirectory(targetFolderPath);
        }

        string[] lines = csvFile.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) return; // Only headers or empty

        string[] headers = lines[0].Split(',');

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            if (values.Length < headers.Length) continue;

            Dictionary<string, string> row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Length; j++)
            {
                row[headers[j].Trim()] = values[j].Trim();
            }

            string assetName = row["fileName"];
            string assetPath = $"{targetFolderPath}/{assetName}.asset";

            GunSO gun = AssetDatabase.LoadAssetAtPath<GunSO>(assetPath);
            if (gun == null)
            {
                gun = ScriptableObject.CreateInstance<GunSO>();
                AssetDatabase.CreateAsset(gun, assetPath);
            }

            // General Stats
            gun.gunName = GetStr(row, "gunName");
            gun.weaponClass = Enum.TryParse(GetStr(row, "weaponClass"), true, out WeaponClass wClass) ? wClass : default;
            gun.magazineCapacity = GetInt(row, "magazineCapacity");
            gun.usesMagazines = GetBool(row, "usesMagazines");
            gun.maxReserveMags = GetInt(row, "maxReserveMags");

            // Handling Stats
            gun.weight = GetFloat(row, "weight");
            gun.fireRate = GetInt(row, "fireRate");
            gun.tacReloadTime = GetFloat(row, "tacReloadTime");
            gun.emptyReloadTime = GetFloat(row, "emptyReloadTime");
            gun.deployTime = GetFloat(row, "deployTime");
            gun.undeployTime = GetFloat(row, "undeployTime");
            gun.ADSTime = GetFloat(row, "ADSTime");
            gun.sprintToFireTime = GetFloat(row, "sprintToFireTime");
            gun.ADSSway = GetFloat(row, "ADSSway");
            gun.headshotMultiplier = GetFloat(row, "headshotMultiplier");

            // Ballistics Stats
            gun.muzzleVelocity = GetInt(row, "muzzleVelocity");

            // Stability Stats
            gun.verticalKick = GetFloat(row, "verticalKick");
            gun.horizontalKickDirectionBias = GetFloat(row, "horizontalKickDirectionBias");
            gun.horizontalKickDirectionVariation = GetFloat(row, "horizontalKickDirectionVariation");
            gun.ADSReductionMultiplier = GetFloat(row, "ADSReductionMultiplier");

            // Spread Base
            gun.semiautoDynamicDispersionMultiplier = GetFloat(row, "semiautoDynamicDispersionMultiplier");
            gun.adsMechanicalDispersion = GetFloat(row, "adsMechanicalDispersion");
            gun.adsDynamicDispersion = GetFloat(row, "adsDynamicDispersion");
            gun.adsDynamicDispersionRecoveryRate = GetFloat(row, "adsDynamicDispersionRecoveryRate");
            gun.hipMechanicalDispersion = GetFloat(row, "hipMechanicalDispersion");
            gun.hipDynamicDispersion = GetFloat(row, "hipDynamicDispersion");
            gun.hipDynamicDispersionRecoveryRate = GetFloat(row, "hipDynamicDispersionRecoveryRate");

            // Spread Data Structures
            gun.adsStandMults = new SpreadData { stillMultiplier = GetFloat(row, "adsStand_Still"), movingMultiplier = GetFloat(row, "adsStand_Move") };
            gun.adsCrouchMults = new SpreadData { stillMultiplier = GetFloat(row, "adsCrouch_Still"), movingMultiplier = GetFloat(row, "adsCrouch_Move") };
            gun.adsProneMults = new SpreadData { stillMultiplier = GetFloat(row, "adsProne_Still"), movingMultiplier = GetFloat(row, "adsProne_Move") };

            gun.hipStandMults = new SpreadData { stillMultiplier = GetFloat(row, "hipStand_Still"), movingMultiplier = GetFloat(row, "hipStand_Move") };
            gun.hipCrouchMults = new SpreadData { stillMultiplier = GetFloat(row, "hipCrouch_Still"), movingMultiplier = GetFloat(row, "hipCrouch_Move") };
            gun.hipProneMults = new SpreadData { stillMultiplier = GetFloat(row, "hipProne_Still"), movingMultiplier = GetFloat(row, "hipProne_Move") };

            // Fire Modes (Format: "Auto:false|Burst:true")
            string rawFireModes = GetStr(row, "fireModes");
            if (!string.IsNullOrEmpty(rawFireModes))
            {
                string[] modePairs = rawFireModes.Split('|');
                gun.fireModes = new FireModeData[modePairs.Length];
                for (int m = 0; m < modePairs.Length; m++)
                {
                    string[] parts = modePairs[m].Split(':');
                    FireMode mode = Enum.TryParse(parts[0], true, out FireMode fm) ? fm : default;
                    bool selectFire = parts.Length > 1 && bool.Parse(parts[1]);
                    gun.fireModes[m] = new FireModeData { fireMode = mode, isSelectFire = selectFire };
                }
            }

            // Assign assets automatically
            string prefabPath = $"Assets/Prefabs/Guns/{assetName}.prefab";
            if (File.Exists(prefabPath)) gun.gunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            string calibrePath = $"Assets/ScriptableObjects/Calibres/{GetStr(row, "calibreName")}.asset";
            if (File.Exists(calibrePath)) gun.calibre = AssetDatabase.LoadAssetAtPath<CalibreSO>(calibrePath);

            EditorUtility.SetDirty(gun);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Successfully generated/updated {lines.Length - 1} GunSO assets!");
    }

    private string GetStr(Dictionary<string, string> d, string k) => d.ContainsKey(k) ? d[k] : "";
    private float GetFloat(Dictionary<string, string> d, string k) => float.TryParse(GetStr(d, k), out float v) ? v : 0f;
    private int GetInt(Dictionary<string, string> d, string k) => int.TryParse(GetStr(d, k), out int v) ? v : 0;
    private bool GetBool(Dictionary<string, string> d, string k) => bool.TryParse(GetStr(d, k), out bool v) ? v : false;
}