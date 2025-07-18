using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class VoronoiGapFiller : MonoBehaviour
{
    [Header("Settings")]
    public GameObject cellPrefab;
    public Material lakeMaterial;
    public Material parkMaterial;

    public float cellSize = 10f;

    public void FillGaps(bool[,] visited, int[,] voronoiMask, List<Vector2Int> seeds)
    {
#if UNITY_EDITOR
        ClearExisting();

        int width = visited.GetLength(0);
        int height = visited.GetLength(1);

        // Group Voronoi regions
        Dictionary<int, List<Vector2Int>> regionMap = new();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                int region = voronoiMask[x, z];

                if (!regionMap.ContainsKey(region))
                    regionMap[region] = new List<Vector2Int>();

                regionMap[region].Add(new Vector2Int(x, z));
            }
        }

        GameObject root = new GameObject("VoronoiFilled");
        root.transform.parent = this.transform;

        foreach (var kvp in regionMap)
        {
            int regionID = kvp.Key;
            var cells = kvp.Value;
            var unvisited = cells.Where(c => !visited[c.x, c.y]).ToList();

            if (unvisited.Count < 4) continue;

            string type = ClassifyRegion(cells);
            Color col = GetColorByType(type);

            if (type == "Discard") continue;

            GameObject regionGO = new GameObject($"Region_{type}_{regionID}");
            regionGO.transform.parent = root.transform;

            var regionMesh = regionGO.AddComponent<RegionMeshGenerator>();
            regionMesh.regionCells = unvisited;
            regionMesh.regionType = type;
            regionMesh.cellSize = cellSize;
            regionMesh.lakeMaterial = lakeMaterial; // you can later assign separate ones
            regionMesh.parkMaterial = parkMaterial;

#if UNITY_EDITOR
            regionMesh.GenerateFilledMeshPerCell();
#endif
        }
#endif
    }

    void ClearExisting()
    {
#if UNITY_EDITOR
        Transform existing = transform.Find("VoronoiFilled");
        if (existing != null)
            DestroyImmediate(existing.gameObject);
#endif
    }

    string ClassifyRegion(List<Vector2Int> cells)
    {
        int area = cells.Count;

        int minX = cells.Min(c => c.x);
        int maxX = cells.Max(c => c.x);
        int minZ = cells.Min(c => c.y);
        int maxZ = cells.Max(c => c.y);

        int w = maxX - minX + 1;
        int h = maxZ - minZ + 1;

        float compactness = (float)area / (w * h);
        float aspect = (float)w / h;

        if (area > 40 && compactness > 0.6f)
            return "Lake";
        if (area > 16)
            return "Park";
        if (aspect > 2f || aspect < 0.5f)
            return "Strip";

        return "Discard";
    }

    Color GetColorByType(string type)
    {
        switch (type)
        {
            case "Lake": return Color.cyan * 0.6f;
            case "Park": return Color.green * 0.8f;
            case "Strip": return new Color(0.4f, 0.8f, 0.4f);
            default: return new Color(0, 0, 0, 0);
        }
    }
}
