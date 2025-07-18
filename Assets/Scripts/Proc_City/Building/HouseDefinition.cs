using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "HouseDefinition", menuName = "City/House Definition", order = 1)]
public class HouseDefinition : ScriptableObject
{
    public string houseName = "DefaultHouse";
    public GameObject housePrefab;
    public List<Material> materials;
    public float spawnWeight = 1f; // Optional for variety later
}
