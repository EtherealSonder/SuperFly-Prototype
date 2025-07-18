using System.Collections.Generic;
using UnityEngine;

public class CityGenerator : MonoBehaviour
{

    [Header("Runtime")]
    public bool enableRuntimeGen = false;

    [Header("City Settings")]
    public int cityWidth = 10;
    public int cityHeight = 10;
    public float blockSize = 10f;
    public float roadWidth = 2f;

    [Header("Block Merge Size Range")]
    public int maxMergeSizeX = 3;
    public int maxMergeSizeZ = 3;

    [Header("Prefabs")]
    public GameObject blockPrefab;
    public GameObject roadPrefab;

    [Header("Noise Settings")]
    public bool randomizeSeedOnGenerate = false;
    public string noiseSeed = "defaultSeed";
    public float noiseFrequency = 0.1f;
    public float noiseThreshold = 0.3f;

    [Header("Voronoi Settings")]
    public int numVoronoiSeeds = 8;
    public float voronoiGapThreshold = 15f;

    [Header("Block Merging")]
    public bool enableMergedBlocks = true;
    public float mergeChance = 0.4f;


    [Header("Zoning Settings")]
    public float zoneFrequency = 0.02f;
    public float zoneThreshold = 0.5f;
    [Range(0, 1)]
    public float zoneNoiseInfluence = 0.25f;

    [Header("Zone Materials")]
    public Material residentialMaterial;
    public Material urbanMaterial;

    [Header("Building Pool")]
    public List<BuildingDefinition> buildingDefs = new List<BuildingDefinition>();
    public List<HouseDefinition> houseDefs = new List<HouseDefinition>();

    [Header("Culling")]
    public int cullingChunkSize;


    void Start()
    {
        if (enableRuntimeGen)
        {
            Generate(true);
        }
    }
    public void Generate(bool isRuntime = false)
    {
        // List to safely hold targets to destroy
        List<Transform> toDestroy = new List<Transform>();

        string[] generatedGroups = { "Chunks", "Roads", "RoadIntersections" ,"VoronoiFilled"};

        // Collect targets first
        foreach (Transform child in transform)
        {
            foreach (string group in generatedGroups)
            {
                if (child != null && child.name == group)
                {
                    toDestroy.Add(child);
                    break; // No need to check other group names once matched
                }
            }
        }

        // Destroy them safely after iteration
        foreach (Transform target in toDestroy)
        {
#if UNITY_EDITOR
            if (!isRuntime) DestroyImmediate(target.gameObject);
            else Destroy(target.gameObject);
#else
    Destroy(target.gameObject);
#endif
        }

        var generator = new GridRoadGenerator();

        if (randomizeSeedOnGenerate)
        {
            noiseSeed = System.Guid.NewGuid().ToString();
            Debug.Log("Generated new random seed: " + noiseSeed);
        }
        int seedOffset = Mathf.Abs(noiseSeed.GetHashCode()) % 10000;


        bool[,] occupiedGrid = generator.GenerateBlocksPreciseScaling(
    cityWidth,
    cityHeight,
    blockSize,
    roadWidth,
    blockPrefab,
    transform,
    noiseFrequency,
    noiseThreshold,
    seedOffset,
    enableMergedBlocks,
    mergeChance,
    numVoronoiSeeds,
    voronoiGapThreshold,
    maxMergeSizeX,
    maxMergeSizeZ,
    zoneFrequency,
    zoneThreshold,
    zoneNoiseInfluence,
    residentialMaterial,
    urbanMaterial,
    buildingDefs,houseDefs,
    cullingChunkSize,
    out int[,] voronoiMask,
    out List<Vector2Int> voronoiSeeds);


        generator.GenerateRoadsBetweenBlocks(
        occupiedGrid,
        cityWidth,
        cityHeight,
        blockSize,
        roadWidth,
        roadPrefab,
        transform
    );

        generator.GenerateRoadIntersections(
    occupiedGrid,
    cityWidth,
    cityHeight,
    blockSize,
    roadWidth,
    roadPrefab,
    transform
);

        var filler = GetComponent<VoronoiGapFiller>();
        if (filler != null)
        {
            filler.cellSize = blockSize + roadWidth;
            filler.FillGaps(occupiedGrid, voronoiMask, voronoiSeeds, isRuntime);
        }


    }
}
