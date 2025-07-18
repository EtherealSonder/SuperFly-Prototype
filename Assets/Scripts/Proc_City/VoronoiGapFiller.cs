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

    [Header("Region Thresholds")]
    public int lakeAreaThreshold = 20;
    public int parkAreaThreshold = 8;

    [Header("Wall Prefab")]
    public GameObject wallPrefab;
    public float wallHeight = 5f;
    public Material wallMaterial;
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

        int subRegionID = 0;

        foreach (var kvp in regionMap)
        {
            var regionCells = kvp.Value;
            var unvisited = regionCells.Where(c => !visited[c.x, c.y]).ToList();
            if (unvisited.Count == 0) continue;

            // Break into distinct regions using flood fill
            List<List<Vector2Int>> subRegions = FloodFillRegions(unvisited);

            foreach (var sub in subRegions)
            {
                if (sub.Count == 0) continue;

                string type = ClassifyRegion(sub);
                if (type == "Discard") continue;

                GameObject regionGO = new GameObject($"Region_{type}_{subRegionID++}");
                regionGO.transform.parent = root.transform;

                var regionMesh = regionGO.AddComponent<RegionMeshGenerator>();
                regionMesh.regionCells = sub;
                regionMesh.regionType = type;
                regionMesh.cellSize = cellSize;
                regionMesh.lakeMaterial = lakeMaterial;
                regionMesh.parkMaterial = parkMaterial;
                regionMesh.wallPrefab = wallPrefab;
                regionMesh.wallHeight = wallHeight;
                regionMesh.wallMaterial = wallMaterial;

#if UNITY_EDITOR
                regionMesh.GenerateFilledMeshPerCell();
#endif
            }
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

        if (area >= lakeAreaThreshold && compactness > 0.4f)
            return "Lake";
        if (area >= parkAreaThreshold)
            return "Park";
        if (aspect > 1f || aspect < 0.5f)
            return "Strip";

        return "Discard";
    }

    Color GetColorByType(string type)
    {
        return type switch
        {
            "Lake" => Color.cyan * 0.6f,
            "Park" => Color.green * 0.8f,
            "Strip" => new Color(0.4f, 0.8f, 0.4f),
            _ => new Color(0, 0, 0, 0),
        };
    }

    List<List<Vector2Int>> FloodFillRegions(List<Vector2Int> cells)
    {
        var regions = new List<List<Vector2Int>>();
        var remaining = new HashSet<Vector2Int>(cells);

        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        while (remaining.Count > 0)
        {
            var region = new List<Vector2Int>();
            var queue = new Queue<Vector2Int>();

            Vector2Int start = remaining.First();
            queue.Enqueue(start);
            remaining.Remove(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                region.Add(current);

                foreach (var dir in directions)
                {
                    Vector2Int neighbor = current + dir;
                    if (remaining.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        remaining.Remove(neighbor);
                    }
                }
            }

            regions.Add(region);
        }

        return regions;
    }
}
