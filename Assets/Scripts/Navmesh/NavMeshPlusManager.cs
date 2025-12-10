using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using NavMeshPlus.Components;

public enum SurfaceMode { Single, Multi }

public class NavMeshPlusManager : MonoBehaviour
{
    public static NavMeshPlusManager Instance;

    [Header("Surface Mode")]
    public SurfaceMode mode = SurfaceMode.Multi;

    [Header("Auto Detect Surfaces")]
    public bool autoFindSurfaces = true;

    [Header("Surfaces (optional manual override)")]
    public List<NavMeshSurface> surfaces = new();

    private List<NavMeshDataInstance> activeInstances = new();

    void Awake()
    {
        Instance = this;

        if (autoFindSurfaces)
            FindSurfaces();

        BuildAndRegister();
    }

    public void FindSurfaces()
    {
        surfaces.Clear();
        surfaces.AddRange(FindObjectsByType<NavMeshSurface>(sortMode: FindObjectsSortMode.None));
    }

    public void BuildAndRegister()
    {
        // Remove old data
        foreach (var inst in activeInstances)
            inst.Remove();
        activeInstances.Clear();

        if (mode == SurfaceMode.Single && surfaces.Count > 0)
        {
            var s = surfaces[0];
            s.BuildNavMesh();
            activeInstances.Add(NavMesh.AddNavMeshData(s.navMeshData));
            return;
        }

        // Multi mode
        foreach (var s in surfaces)
        {
            s.BuildNavMesh();
            activeInstances.Add(NavMesh.AddNavMeshData(s.navMeshData));
        }
    }

    /// Multi-surface SamplePosition
    public bool SamplePosition(Vector2 position, out Vector3 hit, float maxDistance, int areaMask)
    {
        hit = Vector3.zero;

        if (mode == SurfaceMode.Single)
            return NavMesh.SamplePosition(position, out NavMeshHit h, maxDistance, areaMask) && (hit == h.position);

        // Multi mode: choose closest hit
        float bestDist = Mathf.Infinity;
        bool found = false;
        Vector3 bestPos = Vector3.zero;

        foreach (var s in surfaces)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit h, maxDistance, areaMask))
            {
                float d = Vector2.Distance(position, h.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestPos = h.position;
                    found = true;
                }
            }
        }

        hit = bestPos;
        return found;
    }
}
