using UnityEngine;
using System.Collections.Generic;

public class BuildingSpawner : MonoBehaviour
{
    [Header("Building Pool")]
    public List<BuildingDefinition> buildingDefs = new List<BuildingDefinition>();

    [Header("Simulated Block Size (in grid units)")]
    public int blockSizeX = 1; // Merge size X
    public int blockSizeZ = 1; // Merge size Z

    [Header("Spawn Controls")]
    public Vector3 spawnCenter = Vector3.zero;
    public float cellSize = 100f; // Default block unit size

    [Header("Building Cell")]
    public float buildingCellSize = 100f; // Includes spacing
    public float buildingYOffset = 0f;

    // Debug info
    [System.Serializable]
    public struct LeftoverArea
    {
        public Vector3 position;
        public Vector2 size;
    }

    public List<LeftoverArea> leftoverZones = new List<LeftoverArea>();
    private GameObject currentRoot;

    public void Generate()
    {
        // Clear previous
        if (currentRoot != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(currentRoot);
#else
        Destroy(currentRoot);
#endif
        }
        leftoverZones.Clear();

        if (buildingDefs == null || buildingDefs.Count == 0)
        {
            Debug.LogWarning("No building definitions assigned.");
            return;
        }

        // Compute total block world size
        float totalWidth = blockSizeX * cellSize;
        float totalDepth = blockSizeZ * cellSize;

        // Compute how many buildings fit
        int cols = Mathf.FloorToInt(totalWidth / buildingCellSize);
        int rows = Mathf.FloorToInt(totalDepth / buildingCellSize);

        float usedWidth = cols * buildingCellSize;
        float usedDepth = rows * buildingCellSize;

        float leftoverX = totalWidth - usedWidth;
        float leftoverZ = totalDepth - usedDepth;

        // Origin offset to center the grid
        float startX = -usedWidth / 2f + buildingCellSize / 2f;
        float startZ = -usedDepth / 2f + buildingCellSize / 2f;

        // Create root parent
        currentRoot = new GameObject($"Block_{blockSizeX}x{blockSizeZ}_Mixed");
        currentRoot.transform.position = spawnCenter;
        currentRoot.transform.parent = this.transform;

        // Spawn buildings in grid
        for (int x = 0; x < cols; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                float offsetX = startX + x * buildingCellSize;
                float offsetZ = startZ + z * buildingCellSize;
                Vector3 basePos = spawnCenter + new Vector3(offsetX, 0f, offsetZ);

                // Pick a different building per cell based on weights
                BuildingDefinition def = GetWeightedRandomBuilding();
                SpawnBuilding(def, basePos);
            }
        }

        // Record leftover zones (left, right, front, back strips)
        if (leftoverX > 0)
        {
            float halfGap = leftoverX / 2f;

            // Left strip
            leftoverZones.Add(new LeftoverArea
            {
                position = spawnCenter + new Vector3(-totalWidth / 2f + halfGap / 2f, 0f, 0f),
                size = new Vector2(halfGap, totalDepth)
            });

            // Right strip
            leftoverZones.Add(new LeftoverArea
            {
                position = spawnCenter + new Vector3(totalWidth / 2f - halfGap / 2f, 0f, 0f),
                size = new Vector2(halfGap, totalDepth)
            });
        }

        if (leftoverZ > 0)
        {
            float halfGap = leftoverZ / 2f;

            // Front strip
            leftoverZones.Add(new LeftoverArea
            {
                position = spawnCenter + new Vector3(0f, 0f, totalDepth / 2f - halfGap / 2f),
                size = new Vector2(totalWidth, halfGap)
            });

            // Back strip
            leftoverZones.Add(new LeftoverArea
            {
                position = spawnCenter + new Vector3(0f, 0f, -totalDepth / 2f + halfGap / 2f),
                size = new Vector2(totalWidth, halfGap)
            });
        }

        Debug.Log($"Spawned {cols * rows} buildings with weighted distribution. Leftover zones: {leftoverZones.Count}");
    }

    private void SpawnBuilding(BuildingDefinition def, Vector3 basePosition)
    {
        int baseCount = Random.Range(def.minBaseCount, def.maxBaseCount + 1);
        float currentY = basePosition.y;

        GameObject buildingRoot = new GameObject(def.buildingName);
        buildingRoot.transform.parent = currentRoot.transform;
        buildingRoot.transform.position = basePosition;

        // Spawn bases
        for (int i = 0; i < baseCount; i++)
        {
            Vector3 segmentPos = basePosition + new Vector3(0, currentY, 0);
            GameObject baseObj = Instantiate(def.basePrefab, segmentPos, Quaternion.identity, buildingRoot.transform);
            currentY += def.baseHeight + def.yOffset;
        }

        // Spawn top
        Vector3 topPos = basePosition + new Vector3(0, currentY, 0);
        GameObject topObj = Instantiate(def.topPrefab, topPos, Quaternion.identity, buildingRoot.transform);
    }

    private BuildingDefinition GetWeightedRandomBuilding()
    {
        float totalWeight = 0f;
        foreach (var def in buildingDefs)
        {
            totalWeight += def.spawnWeight;
        }

        float randomPoint = Random.value * totalWeight;

        float cumulative = 0f;
        foreach (var def in buildingDefs)
        {
            cumulative += def.spawnWeight;
            if (randomPoint <= cumulative)
                return def;
        }

        return buildingDefs[buildingDefs.Count - 1]; // fallback
    }

}
