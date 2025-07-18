using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RegionMeshGenerator : MonoBehaviour
{
    [Header("Input")]
    public List<Vector2Int> regionCells = new List<Vector2Int>();
    public float cellSize = 150f;
    public string regionType = "Lake"; // or "Park"

    [Header("Materials")]
    public Material lakeMaterial;
    public Material parkMaterial;
    public Material wallMaterial;
    public GameObject wallPrefab;
    public float wallHeight = 5f;

    [ContextMenu("Generate Filled Mesh (Per Cell)")]
    public void GenerateFilledMeshPerCell()
    {
#if UNITY_EDITOR
        if (regionCells.Count == 0)
        {
            Debug.LogWarning("No region cells to fill.");
            return;
        }

        List<CombineInstance> combine = new List<CombineInstance>();

        foreach (var cell in regionCells)
        {
            Mesh quad = CreateQuadMesh(cellSize);

            Vector3 worldPos = new Vector3(cell.x * cellSize, 0, cell.y * cellSize);

            CombineInstance ci = new CombineInstance();
            ci.mesh = quad;
            ci.transform = Matrix4x4.TRS(worldPos, Quaternion.identity, Vector3.one);
            combine.Add(ci);
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // for large meshes
        combinedMesh.CombineMeshes(combine.ToArray());

        GetComponent<MeshFilter>().sharedMesh = combinedMesh;

        var rend = GetComponent<MeshRenderer>();
        if (regionType == "Lake" && lakeMaterial != null)
            rend.sharedMaterial = lakeMaterial;
        else if (regionType == "Park" && parkMaterial != null)
            rend.sharedMaterial = parkMaterial;

        GeneratePerimeterWalls();
#endif
    }

    private Mesh CreateQuadMesh(float size)
    {
        float half = size / 2f;

        Vector3[] vertices = new Vector3[]
        {
        new Vector3(-half, 0, -half),
        new Vector3(half, 0, -half),
        new Vector3(half, 0, half),
        new Vector3(-half, 0, half)
        };

        int[] triangles = new int[]
        {
        0, 2, 1,
        0, 3, 2
        };

        Vector2[] uvs = new Vector2[]
        {
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(1, 1),
        new Vector2(0, 1)
        };

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        return mesh;
    }

    public void GeneratePerimeterWalls()
    {
#if UNITY_EDITOR
        if (wallPrefab == null) return;

        HashSet<Vector2Int> regionSet = new HashSet<Vector2Int>(regionCells);
        Vector2Int[] directions = new Vector2Int[]
        {
        Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down
        };

        GameObject wallRoot = new GameObject("Walls");
        wallRoot.transform.parent = this.transform;

        foreach (var cell in regionCells)
        {
            foreach (var dir in directions)
            {
                Vector2Int neighbor = cell + dir;
                if (!regionSet.Contains(neighbor))
                {
                    Vector3 basePos = new Vector3(cell.x * cellSize, 0, cell.y * cellSize);
                    Vector3 offset = new Vector3(dir.x, 0, dir.y) * (cellSize / 2f);
                    Vector3 wallPos = basePos + offset;

                    Quaternion rot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y));
                    GameObject wall = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(wallPrefab);
                    wall.transform.position = wallPos;
                    wall.transform.rotation = rot;
                    wall.transform.localScale = new Vector3(cellSize, wallHeight, 10f);
                    wall.transform.parent = wallRoot.transform;

                    // Apply material with tiling
                    if (wallMaterial != null)
                    {
                        Renderer rend = wall.GetComponentInChildren<Renderer>();
                        if (rend != null)
                        {
                            rend.sharedMaterial = wallMaterial;

                            float tileUnitX = 20f;
                            float tileUnitZ = 20f;

                            rend.sharedMaterial.mainTextureScale = new Vector2(
                                wall.transform.localScale.x / tileUnitX,
                                wall.transform.localScale.z / tileUnitZ
                            );
                        }
                    }
                }
            }
        }
#endif
    }


    #region MarchSquares not used
    [ContextMenu("Generate Region Mesh")]
    public void Generate()
    {
#if UNITY_EDITOR
        if (regionCells.Count == 0)
        {
            Debug.LogWarning("No cells assigned for region mesh.");
            return;
        }

        // Step 1: Convert cells to 2D grid
        var bounds = GetBounds(regionCells);
        int gridW = bounds.size.x + 2;
        int gridH = bounds.size.y + 2;

        bool[,] grid = new bool[gridW, gridH];

        foreach (var cell in regionCells)
        {
            int x = cell.x - bounds.min.x + 1;
            int y = cell.y - bounds.min.y + 1;
            grid[x, y] = true;
        }

        List<Vector2> outline = MarchingSquares2D.GetOutline(grid);
        if (outline.Count < 3) return;

        outline = SmoothOutline(outline, 0.3f); // ✅ smooth first

        // Create vertex positions
        Vector3[] vertices = new Vector3[outline.Count];
        for (int i = 0; i < outline.Count; i++)
        {
            Vector2 p = outline[i];
            float worldX = (p.x + bounds.min.x - 1) * cellSize;
            float worldZ = (p.y + bounds.min.y - 1) * cellSize;
            vertices[i] = new Vector3(worldX, 0, worldZ);
        }

        // Use same smoothed outline for triangulation
        int[] triangles = TriangulatePolygon(outline);

        // Build mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;

        var rend = GetComponent<MeshRenderer>();
        if (regionType == "Lake" && lakeMaterial != null)
            rend.sharedMaterial = lakeMaterial;
        else if (regionType == "Park" && parkMaterial != null)
            rend.sharedMaterial = parkMaterial;
#endif
    }

    List<Vector2> SmoothOutline(List<Vector2> points, float factor = 0.25f)
    {
        List<Vector2> smooth = new List<Vector2>();
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[(i + 1) % points.Count];
            Vector2 mid = Vector2.Lerp(a, b, factor);
            smooth.Add(a);
            smooth.Add(mid);
        }
        return smooth;
    }
    BoundsInt GetBounds(List<Vector2Int> cells)
    {
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var c in cells)
        {
            if (c.x < minX) minX = c.x;
            if (c.x > maxX) maxX = c.x;
            if (c.y < minY) minY = c.y;
            if (c.y > maxY) maxY = c.y;
        }

        return new BoundsInt(minX, minY, 0, maxX - minX + 1, maxY - minY + 1, 1);
    }

    List<Vector2> TraceOutline(bool[,] grid)
    {
        // Simple marching squares edge tracing
        int[,] dirs = new int[,] { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } }; // NESW
        int w = grid.GetLength(0);
        int h = grid.GetLength(1);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (grid[x, y])
                {
                    return FollowEdge(x, y, grid, w, h);
                }
            }
        }

        return new List<Vector2>();
    }

    List<Vector2> FollowEdge(int startX, int startY, bool[,] grid, int w, int h)
    {
        List<Vector2> points = new List<Vector2>();
        Vector2Int[] offsets = new Vector2Int[]
        {
            new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(1, 1), new Vector2Int(0, 1)
        };

        int[,] nextDir = new int[,]
        {
            {0, -1}, {1, 0}, {0, 1}, {-1, 0}
        };

        int x = startX;
        int y = startY;
        int dir = 0;

        Vector2 startPoint = new Vector2(x, y);
        Vector2 lastPoint = new Vector2(-999, -999);
        int safety = 0;

        do
        {
            int index = 0;
            if (grid[x, y]) index |= 1;
            if (x + 1 < w && grid[x + 1, y]) index |= 2;
            if (x + 1 < w && y + 1 < h && grid[x + 1, y + 1]) index |= 4;
            if (y + 1 < h && grid[x, y + 1]) index |= 8;

            Vector2 edgePoint = new Vector2(x, y);
            switch (index)
            {
                case 1: edgePoint += new Vector2(0, 0.5f); x += 0; y += -1; break;
                case 2: edgePoint += new Vector2(0.5f, 0); x += 1; y += 0; break;
                case 4: edgePoint += new Vector2(1f, 0.5f); x += 0; y += 1; break;
                case 8: edgePoint += new Vector2(0.5f, 1f); x += -1; y += 0; break;
                case 3: edgePoint += new Vector2(0.5f, 0); x += 1; y += 0; break;
                case 6: edgePoint += new Vector2(1f, 0.5f); x += 0; y += 1; break;
                case 9: edgePoint += new Vector2(0, 0.5f); x += 0; y += -1; break;
                case 12: edgePoint += new Vector2(0.5f, 1f); x += -1; y += 0; break;
                default: break;
            }

            if ((edgePoint - lastPoint).sqrMagnitude > 0.001f)
                points.Add(edgePoint);

            lastPoint = edgePoint;

            if (++safety > 1000)
                break;

        } while ((x != startX || y != startY));

        return points;
    }

    int[] TriangulatePolygon(List<Vector2> points)
    {
        List<int> indices = new List<int>();
        for (int i = 1; i < points.Count - 1; i++)
        {
            indices.Add(0);
            indices.Add(i);
            indices.Add(i + 1);
        }
        return indices.ToArray();
    }

    #endregion
}
