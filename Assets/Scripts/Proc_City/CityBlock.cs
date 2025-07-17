using UnityEngine;
using System.Collections.Generic;

public enum ZoneType { Urban, Residential }

public class CityBlock : MonoBehaviour
{
    [Header("City Block Properties")]
    public ZoneType zone;
    public int sizeX = 1;
    public int sizeZ = 1;

    [Header("Settings")]
    public float cellSize = 100f;
    public float buildingYOffset = 0f;

    [System.Serializable]
    public struct LeftoverArea
    {
        public Vector3 position;
        public Vector2 size;
    }

    public List<LeftoverArea> leftoverZones = new List<LeftoverArea>();

    private GameObject currentRoot;

    public void GenerateUrban(List<BuildingDefinition> buildingDefs, float blockSize)
    {
        if (zone != ZoneType.Urban) return;

        // Cleanup if already generated
        if (currentRoot != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(currentRoot);
#else
            Destroy(currentRoot);
#endif
        }

        leftoverZones.Clear();
        cellSize = blockSize;

        if (buildingDefs == null || buildingDefs.Count == 0)
        {
            Debug.LogWarning("No building definitions provided.");
            return;
        }

        // Decide building cell size
        float buildingCellSize;
        if (sizeX == 1 || sizeZ == 1)
        {
            buildingCellSize = 100f;
        }
        else
        {
            float[] choices = { 100f, 150f, 200f };
            int r = Random.Range(0, choices.Length);
            buildingCellSize = choices[r];
        }

        // Block world size
        float totalWidth = sizeX * cellSize;
        float totalDepth = sizeZ * cellSize;

        int cols = Mathf.FloorToInt(totalWidth / buildingCellSize);
        int rows = Mathf.FloorToInt(totalDepth / buildingCellSize);

        float usedWidth = cols * buildingCellSize;
        float usedDepth = rows * buildingCellSize;

        float leftoverX = totalWidth - usedWidth;
        float leftoverZ = totalDepth - usedDepth;

        float startX = -usedWidth / 2f + buildingCellSize / 2f;
        float startZ = -usedDepth / 2f + buildingCellSize / 2f;

        // Root object for this block's buildings
        currentRoot = new GameObject($"UrbanBlock_{sizeX}x{sizeZ}");
        currentRoot.transform.parent = this.transform;
        currentRoot.transform.localPosition = Vector3.zero;

        for (int x = 0; x < cols; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                float offsetX = startX + x * buildingCellSize;
                float offsetZ = startZ + z * buildingCellSize;
                Vector3 basePos = transform.position + new Vector3(offsetX, 0f, offsetZ);

                BuildingDefinition def = GetWeightedRandomBuilding(buildingDefs);
                SpawnBuilding(def, basePos);
            }
        }

        // Leftovers (edges)
        if (leftoverX > 0)
        {
            float halfGap = leftoverX / 2f;

            leftoverZones.Add(new LeftoverArea
            {
                position = transform.position + new Vector3(-totalWidth / 2f + halfGap / 2f, 0f, 0f),
                size = new Vector2(halfGap, totalDepth)
            });

            leftoverZones.Add(new LeftoverArea
            {
                position = transform.position + new Vector3(totalWidth / 2f - halfGap / 2f, 0f, 0f),
                size = new Vector2(halfGap, totalDepth)
            });
        }

        if (leftoverZ > 0)
        {
            float halfGap = leftoverZ / 2f;

            leftoverZones.Add(new LeftoverArea
            {
                position = transform.position + new Vector3(0f, 0f, totalDepth / 2f - halfGap / 2f),
                size = new Vector2(totalWidth, halfGap)
            });

            leftoverZones.Add(new LeftoverArea
            {
                position = transform.position + new Vector3(0f, 0f, -totalDepth / 2f + halfGap / 2f),
                size = new Vector2(totalWidth, halfGap)
            });
        }

        Debug.Log($"[CityBlock] Spawned {cols * rows} buildings in {sizeX}x{sizeZ} urban block. CellSize: {buildingCellSize}, Leftovers: {leftoverZones.Count}");
    }

    private void SpawnBuilding(BuildingDefinition def, Vector3 basePosition)
    {
        int baseCount = Random.Range(def.minBaseCount, def.maxBaseCount + 1);
        float currentY = basePosition.y;

        GameObject buildingRoot = new GameObject(def.buildingName);
        buildingRoot.transform.parent = currentRoot.transform;
        buildingRoot.transform.position = basePosition;

        for (int i = 0; i < baseCount; i++)
        {
            Vector3 segmentPos = basePosition + new Vector3(0, currentY - basePosition.y, 0);
            Instantiate(def.basePrefab, segmentPos, Quaternion.identity, buildingRoot.transform);
            currentY += def.baseHeight + def.yOffset;
        }

        Vector3 topPos = basePosition + new Vector3(0, currentY - basePosition.y, 0);
        Instantiate(def.topPrefab, topPos, Quaternion.identity, buildingRoot.transform);
    }

    private BuildingDefinition GetWeightedRandomBuilding(List<BuildingDefinition> defs)
    {
        float totalWeight = 0f;
        foreach (var def in defs) totalWeight += def.spawnWeight;

        float rand = Random.value * totalWeight;
        float cumulative = 0f;

        foreach (var def in defs)
        {
            cumulative += def.spawnWeight;
            if (rand <= cumulative)
                return def;
        }

        return defs[defs.Count - 1]; // fallback
    }
}
