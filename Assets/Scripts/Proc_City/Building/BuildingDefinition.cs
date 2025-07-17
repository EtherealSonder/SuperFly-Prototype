using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingDefinition", menuName = "City/Building Definition")]
public class BuildingDefinition : ScriptableObject
{
    public string buildingName;

    [Header("Spawn Weight (used for probability)")]
    [Range(0, 100)]
    public float spawnWeight=20f;

    [Header("Prefabs")]
    public GameObject basePrefab;
    public GameObject topPrefab;

    [Header("Stacking Properties")]
    public float baseHeight = 10f;  // Height of a single base segment
    public float yOffset = 0f;      // Optional additional vertical offset

    [Header("Base Count Range")]
    public int minBaseCount = 1;
    public int maxBaseCount = 3;
}
