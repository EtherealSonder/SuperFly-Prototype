using UnityEngine;
using System.Collections.Generic;

public class CityCullingManager : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    [Tooltip("Optional override if you want to assign a chunk parent manually")]
    public Transform chunksRootOverride;

    [Header("Culling Settings")]
    public float updateInterval = 0.25f;
    public float cullPadding = 50f;
    public int maxActivationsPerFrame = 2;

    private float timer = 0f;
    private bool initialized = false;
    private Transform chunksRoot;
    private List<Transform> chunkList = new List<Transform>();
    private Queue<Transform> activationQueue = new Queue<Transform>();

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        // Initial (one-time) clip plane set in case LateUpdate is skipped
        if (targetCamera != null && targetCamera.farClipPlane < 3000f)
        {
            targetCamera.farClipPlane = 3000f;
            Debug.Log("[CityCullingManager] Far clip plane initially set to 3000.");
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (!initialized)
        {
            TryInitializeChunks();
        }

        if (initialized && timer >= updateInterval)
        {
            timer = 0f;
            PerformFrustumCulling();
        }

        ProcessActivationQueue();
    }

    void LateUpdate()
    {
        // Ensure the farClipPlane remains extended, even with Cinemachine overrides
        if (targetCamera != null && targetCamera.farClipPlane < 3000f)
        {
            targetCamera.farClipPlane = 3000f;
        }
    }

    void TryInitializeChunks()
    {
        chunksRoot = chunksRootOverride;

        if (chunksRoot == null)
        {
            GameObject auto = GameObject.Find("Chunks");
            if (auto != null)
                chunksRoot = auto.transform;
        }

        if (chunksRoot == null) return;

        chunkList.Clear();
        foreach (Transform child in chunksRoot)
        {
            if (child.name.StartsWith("Chunk_"))
                chunkList.Add(child);
        }

        if (chunkList.Count > 0)
        {
            initialized = true;
            Debug.Log($"[CityCullingManager] Auto-discovered {chunkList.Count} chunks.");
        }
    }

    void PerformFrustumCulling()
    {
        if (targetCamera == null || chunkList.Count == 0) return;

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(targetCamera);

        foreach (Transform chunk in chunkList)
        {
            if (chunk == null) continue;

            Renderer[] renderers = chunk.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;

            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);

            bounds.Expand(cullPadding);

            bool isVisible = GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);

            // Throttled activation
            if (isVisible && !chunk.gameObject.activeSelf)
            {
                activationQueue.Enqueue(chunk);
            }
            else if (!isVisible && chunk.gameObject.activeSelf)
            {
                chunk.gameObject.SetActive(false);
            }
        }
    }

    void ProcessActivationQueue()
    {
        int activated = 0;
        while (activationQueue.Count > 0 && activated < maxActivationsPerFrame)
        {
            Transform chunk = activationQueue.Dequeue();
            if (chunk != null && !chunk.gameObject.activeSelf)
            {
                chunk.gameObject.SetActive(true);
                activated++;
            }
        }
    }
}
