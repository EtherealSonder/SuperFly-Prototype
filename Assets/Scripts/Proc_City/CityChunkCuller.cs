using UnityEngine;

public class CityChunkCuller : MonoBehaviour
{
    public float updateInterval = 0.2f;
    public float padding = 50f; // Extra view margin
    private float timer;
    private Camera mainCam;
    private Bounds chunkBounds;

    void Start()
    {
        mainCam = Camera.main;

        // Calculate bounds from all children
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            chunkBounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
                chunkBounds.Encapsulate(r.bounds);
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            UpdateVisibility();
        }
    }

    void UpdateVisibility()
    {
        if (mainCam == null) return;

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCam);

        Bounds paddedBounds = chunkBounds;
        paddedBounds.Expand(padding);

        bool isVisible = GeometryUtility.TestPlanesAABB(frustumPlanes, paddedBounds);
        if (gameObject.activeSelf != isVisible)
            gameObject.SetActive(isVisible);
    }
}
