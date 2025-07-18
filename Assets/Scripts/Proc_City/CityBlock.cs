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

    #region Urban Buildings Generation
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

    #endregion

    #region Residential Houses Generation
    public void GenerateResidential(List<HouseDefinition> houseDefs, float blockSize)
    {
        if (zone != ZoneType.Residential) return;

        if (currentRoot != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(currentRoot);
#else
        Destroy(currentRoot);
#endif
        }

        if (houseDefs == null || houseDefs.Count == 0)
        {
            Debug.LogWarning("No house definitions provided.");
            return;
        }

        leftoverZones.Clear();
        cellSize = blockSize;

        currentRoot = new GameObject($"ResidentialBlock_{sizeX}x{sizeZ}");
        currentRoot.transform.parent = this.transform;
        currentRoot.transform.localPosition = Vector3.zero;

        Vector3 center = transform.position;

        float totalWidth = sizeX * cellSize;
        float totalDepth = sizeZ * cellSize;

        // Clamp usable size
        float usableWidth = Mathf.Floor(totalWidth / 100f) * 100f;
        float usableDepth = Mathf.Floor(totalDepth / 100f) * 100f;

        float leftoverX = totalWidth - usableWidth;
        float leftoverZ = totalDepth - usableDepth;

        int cols = Mathf.Max(1, Mathf.FloorToInt(usableWidth / 50f));
        int rows = Mathf.Max(1, Mathf.FloorToInt(usableDepth / 50f));

        float cellWidth = usableWidth / cols;
        float cellDepth = usableDepth / rows;

        float startX = -usableWidth / 2f + cellWidth / 2f;
        float startZ = -usableDepth / 2f + cellDepth / 2f;

        int count = 0;
        List<GameObject> allHouses = new List<GameObject>();

        for (int z = 0; z < rows; z++)
        {
            for (int x = 0; x < cols; x++)
            {
                float offsetX = startX + x * cellWidth;
                float offsetZ = startZ + z * cellDepth;
                Vector3 pos = center + new Vector3(offsetX, 0f, offsetZ);

                Quaternion rotation;
                if (z == 0)
                    rotation = Quaternion.Euler(0, 180f, 0); // South
                else if (z == rows - 1)
                    rotation = Quaternion.Euler(0, 0f, 0);   // North
                else if (x == 0)
                    rotation = Quaternion.Euler(0, 270f, 0); // West
                else if (x == cols - 1)
                    rotation = Quaternion.Euler(0, 90f, 0);  // East
                else
                    rotation = Quaternion.identity;

                GameObject house = SpawnHouse(GetRandomHouse(houseDefs), pos, rotation);
                allHouses.Add(house);
                count++;
            }
        }

        // Interior deletion
        if (cols > 1 && rows > 1)
        {
            List<int> deleteIndices = new List<int>();
            for (int z = 1; z < rows - 1; z++)
            {
                for (int x = 1; x < cols - 1; x++)
                {
                    int index = z * cols + x;
                    deleteIndices.Add(index);
                }
            }

            foreach (int i in deleteIndices)
            {
#if UNITY_EDITOR
                DestroyImmediate(allHouses[i]);
#else
            Destroy(allHouses[i]);
#endif
                count--;
            }
        }

        // === LEFTOVER: Interior gap zone (deleted center houses)
        if (cols > 2 && rows > 2)
        {
            float interiorWidth = (cols - 2) * cellWidth;
            float interiorDepth = (rows - 2) * cellDepth;

            leftoverZones.Add(new LeftoverArea
            {
                position = transform.position, // block center
                size = new Vector2(interiorWidth, interiorDepth)
            });
        }

        // === LEFTOVER: Edge margins from merged block mismatch (like urban)
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

        Debug.Log($"[CityBlock] Spawned {count} houses in {sizeX}x{sizeZ} residential block. Leftovers: {leftoverZones.Count}");
    }

    private GameObject SpawnHouse(HouseDefinition def, Vector3 position, Quaternion rotation)
    {
        GameObject houseGO = Instantiate(def.housePrefab, position, rotation, currentRoot.transform);

        if (def.materials != null && def.materials.Count > 0)
        {
            Material chosen = def.materials[Random.Range(0, def.materials.Count)];
            Renderer rend = houseGO.GetComponentInChildren<Renderer>();
            if (rend != null)
                rend.material = chosen;
        }

        return houseGO;
    }



    private Quaternion GetFacingRotation(int x, int z, int maxX, int maxZ)
    {
        if (z == 0) return Quaternion.Euler(0, 0, 0);         // South
        if (x == maxX - 1) return Quaternion.Euler(0, 90, 0); // East
        if (z == maxZ - 1) return Quaternion.Euler(0, 180, 0);// North
        if (x == 0) return Quaternion.Euler(0, 270, 0);       // West
        return Quaternion.identity;
    }

    private HouseDefinition GetRandomHouse(List<HouseDefinition> defs)
    {
        // Simple random for now, can add weighted logic later
        return defs[Random.Range(0, defs.Count)];
    }
    #endregion

}
