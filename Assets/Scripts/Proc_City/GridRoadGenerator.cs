using UnityEngine;
using System.Collections.Generic;

public class GridRoadGenerator
{
    public bool[,] GenerateBlocksPreciseScaling(
    int width,
    int height,
    float blockSize,
    float roadWidth,
    GameObject blockPrefab,
    Transform parent,
    float noiseFrequency,
    float noiseThreshold,
    int seedOffset,
    bool enableMergedBlocks,
    float mergeChance,
    int numVoronoiSeeds,
    float voronoiGapThreshold,
    int maxMergeSizeX,
    int maxMergeSizeZ,
    float zoneFrequency,
    float zoneThreshold,
    float zoneNoiseInfluence,
    Material residentialMat,
    Material urbanMat,
    List<BuildingDefinition> GlobalBuildingDefs,
    List<HouseDefinition> GlobalHouseDefs,
    int chunkSize,
    out int[,] voronoiMask,
    out List<Vector2Int> voronoiSeeds
    )
    {
        bool[,] visited = new bool[width, height];
        float cellSize = blockSize + roadWidth;

        // New chunk system
        Dictionary<Vector2Int, GameObject> chunkMap = new Dictionary<Vector2Int, GameObject>();
        Transform chunkParent = new GameObject("Chunks").transform;
        chunkParent.parent = parent;

        // Voronoi seeds
        voronoiSeeds = new List<Vector2Int>();

       

        Random.InitState(seedOffset);
        for (int i = 0; i < numVoronoiSeeds; i++)
            voronoiSeeds.Add(new Vector2Int(Random.Range(0, width), Random.Range(0, height)));

        voronoiMask = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float minDist = float.MaxValue;
                int closest = -1;

                for (int i = 0; i < voronoiSeeds.Count; i++)
                {
                    float dist = Vector2Int.Distance(new Vector2Int(x, z), voronoiSeeds[i]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = i;
                    }
                }

                voronoiMask[x, z] = closest;
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                if (visited[x, z]) continue;

                // Voronoi gap
                float minDistance = float.MaxValue;
                foreach (var seed in voronoiSeeds)
                {
                    float dist = Vector2Int.Distance(new Vector2Int(x, z), seed);
                    if (dist < minDistance) minDistance = dist;
                }
                if (minDistance > voronoiGapThreshold) continue;

                // Perlin noise skip
                float noise = Mathf.PerlinNoise((x + seedOffset) * noiseFrequency, (z + seedOffset) * noiseFrequency);
                if (noise < noiseThreshold) continue;

                // Merging
                int sizeX = 1, sizeZ = 1;
                if (enableMergedBlocks && Random.value < mergeChance)
                {
                    int trySizeX = Random.Range(1, maxMergeSizeX + 1);
                    int trySizeZ = Random.Range(1, maxMergeSizeZ + 1);

                    bool canMerge = true;
                    for (int dx = 0; dx < trySizeX && canMerge; dx++)
                        for (int dz = 0; dz < trySizeZ && canMerge; dz++)
                            if ((x + dx >= width) || (z + dz >= height) || visited[x + dx, z + dz])
                                canMerge = false;

                    if (canMerge)
                    {
                        sizeX = trySizeX;
                        sizeZ = trySizeZ;
                    }
                }

                // Mark visited
                for (int dx = 0; dx < sizeX; dx++)
                    for (int dz = 0; dz < sizeZ; dz++)
                        if ((x + dx < width) && (z + dz < height))
                            visited[x + dx, z + dz] = true;

                // World placement
                float posX = (x + sizeX / 2f) * cellSize - (cellSize / 2f);
                float posZ = (z + sizeZ / 2f) * cellSize - (cellSize / 2f);
                Vector3 blockPos = new Vector3(posX, 0, posZ);

                float widthWorld = sizeX * cellSize - roadWidth;
                float depthWorld = sizeZ * cellSize - roadWidth;
                Vector3 blockScale = new Vector3(widthWorld, 1f, depthWorld);

                // Chunk logic
                int chunkX = x / chunkSize;
                int chunkZ = z / chunkSize;
                Vector2Int chunkKey = new Vector2Int(chunkX, chunkZ);

                if (!chunkMap.ContainsKey(chunkKey))
                {
                    GameObject chunkGO = new GameObject($"Chunk_{chunkX}_{chunkZ}");
                    chunkGO.transform.parent = chunkParent;
                    chunkGO.transform.position = Vector3.zero;
                    chunkGO.AddComponent<CityChunkCuller>();
                    chunkMap[chunkKey] = chunkGO;
                }

                // Instantiate block under chunk
                GameObject block = GameObject.Instantiate(blockPrefab, blockPos, Quaternion.identity, chunkMap[chunkKey].transform);
                block.transform.localScale = blockScale;

                // Determine zone
                ZoneType zone = GetZoneType(x, z, width, height, seedOffset, zoneFrequency, zoneThreshold, zoneNoiseInfluence);

                // Set material
                Renderer blockRenderer = block.GetComponent<Renderer>();
                if (blockRenderer != null)
                {
                    if (zone == ZoneType.Residential && residentialMat != null)
                        blockRenderer.sharedMaterial = residentialMat;
                    else if (zone == ZoneType.Urban && urbanMat != null)
                        blockRenderer.sharedMaterial = urbanMat;

                    float tileUnitX = 20f;
                    float tileUnitZ = 20f;

                    blockRenderer.sharedMaterial.mainTextureScale = new Vector2(
                        block.transform.localScale.x / tileUnitX,
                        block.transform.localScale.z / tileUnitZ
                    );
                }
                // CityBlock setup
                CityBlock cityBlock = block.GetComponent<CityBlock>();
                if (cityBlock != null)
                {
                    cityBlock.zone = zone;
                    cityBlock.sizeX = sizeX;
                    cityBlock.sizeZ = sizeZ;

                    if (zone == ZoneType.Urban)
                    {
                        cityBlock.GenerateUrban(GlobalBuildingDefs, blockSize);
                    }
                    else if (zone == ZoneType.Residential)
                    {
                        // Add your houseDefs list to the method signature if needed
                        cityBlock.GenerateResidential(GlobalHouseDefs, blockSize);
                    }

                }
            }
        }

        return visited;
    }


    private ZoneType GetZoneType(
      int x, int z,
      int width, int height,
      int seedOffset,
      float zoneFrequency,
      float zoneThreshold,
      float noiseInfluence)
    {
        // Get city center in grid space
        Vector2 center = new Vector2(width / 2f, height / 2f);
        Vector2 current = new Vector2(x, z);

        // Normalized distance from center (0 = center, 1 = corner)
        float distNorm = Vector2.Distance(current, center) / Vector2.Distance(Vector2.zero, center);

        // Add Perlin noise (blended in)
        float noise = Mathf.PerlinNoise(
            (x + seedOffset + 1000) * zoneFrequency,
            (z + seedOffset + 1000) * zoneFrequency
        );

        // Blend noise into distance-based value
        float blendedValue = distNorm - (noise * noiseInfluence);

        return (blendedValue < zoneThreshold) ? ZoneType.Urban : ZoneType.Residential;
    }


    public void GenerateRoadsBetweenBlocks(
    bool[,] occupied,
    int width,
    int height,
    float blockSize,
    float roadWidth,
    GameObject roadPrefab,
    Transform parent)
    {
        GameObject roadsParent = new GameObject("Roads");
        roadsParent.transform.parent = parent;

        float cellSize = blockSize + roadWidth;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                if (!occupied[x, z])
                    continue;

                Vector3 basePos = new Vector3(x * cellSize, -0.05f, z * cellSize);

                // Check right neighbor (horizontal road)
                if (x + 1 < width && occupied[x + 1, z])
                {
                    float centerX = basePos.x + cellSize / 2f;
                    float centerZ = basePos.z;
                    Vector3 roadPos = new Vector3(centerX, -0.05f, centerZ);
                    GameObject road = GameObject.Instantiate(roadPrefab, roadPos, Quaternion.identity, roadsParent.transform);
                    road.transform.localScale = new Vector3(roadWidth, 0.1f, blockSize);
                }

                // Check top neighbor (vertical road)
                if (z + 1 < height && occupied[x, z + 1])
                {
                    float centerX = basePos.x;
                    float centerZ = basePos.z + cellSize / 2f;
                    Vector3 roadPos = new Vector3(centerX, -0.05f, centerZ);
                    GameObject road = GameObject.Instantiate(roadPrefab, roadPos, Quaternion.identity, roadsParent.transform);
                    road.transform.localScale = new Vector3(blockSize, 0.1f, roadWidth);
                }
            }
        }
    }

    public void GenerateRoadIntersections(
    bool[,] occupied,
    int width,
    int height,
    float blockSize,
    float roadWidth,
    GameObject roadPrefab,
    Transform parent)
    {
        GameObject intersectionsParent = new GameObject("RoadIntersections");
        intersectionsParent.transform.parent = parent;

        float cellSize = blockSize + roadWidth;

        for (int x = 0; x < width - 1; x++)
        {
            for (int z = 0; z < height - 1; z++)
            {
                // Check for intersection: all 4 adjacent cells must be occupied
                if (occupied[x, z] &&
                    occupied[x + 1, z] &&
                    occupied[x, z + 1] &&
                    occupied[x + 1, z + 1])
                {
                    float posX = x * cellSize + cellSize / 2f;
                    float posZ = z * cellSize + cellSize / 2f;
                    Vector3 intersectionPos = new Vector3(posX, -0.05f, posZ);

                    GameObject intersection = GameObject.Instantiate(roadPrefab, intersectionPos, Quaternion.identity, intersectionsParent.transform);
                    intersection.transform.localScale = new Vector3(roadWidth, 0.1f, roadWidth);
                }
            }
        }
    }


}
