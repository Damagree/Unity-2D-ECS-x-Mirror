using Mirror;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

public partial class NavMeshQuerySystem : SystemBase
{
    public int maxRequestsPerFrame = 128;

    protected override void OnUpdate()
    {
        if (!NetworkServer.active)
            return;

        var em = EntityManager;
        int processed = 0;

        //-------------------------------------------------------------------
        // 1) Query semua entity yang punya NavPathRequest
        //-------------------------------------------------------------------
        foreach (var (req, entity) in
                 SystemAPI.Query<NavPathRequest>().WithEntityAccess())
        {
            if (processed++ >= maxRequestsPerFrame)
                break;

            ComputePath(entity, req, em);
        }
    }

    //-----------------------------------------------------------------------
    // 2) Full ComputePath yang valid untuk Unity 6 FINAL
    //-----------------------------------------------------------------------
    void ComputePath(Entity entity, NavPathRequest req, EntityManager em)
    {
        NavMeshQuery query = new NavMeshQuery();

        int agentType = 0;
        int areaMask = req.AreaMask;
        Vector3 extents = new Vector3(1, 1, 1);

        // Map start/end
        var startLoc = query.MapLocation(
            new Vector3(req.Start.x, req.Start.y, 0),
            extents, agentType, areaMask
        );

        var endLoc = query.MapLocation(
            new Vector3(req.End.x, req.End.y, 0),
            extents, agentType, areaMask
        );

        // Invalid location → clear & exit
        if (!IsValidLocation(startLoc) || IsValidLocation(endLoc))
        {
            if (em.HasBuffer<PathCorner>(entity))
                em.GetBuffer<PathCorner>(entity).Clear();

            em.RemoveComponent<NavPathRequest>(entity);
            return;
        }

        //-------------------------------------------------------------------
        // Begin path
        //-------------------------------------------------------------------
        query.BeginFindPath(startLoc, endLoc, areaMask);

        PathQueryStatus status = PathQueryStatus.InProgress;
        int iterations = 0;
        int guard = 0;

        while (status == PathQueryStatus.InProgress && guard++ < 32)
        {
            status = query.UpdateFindPath(256, out iterations);
        }

        if (status != PathQueryStatus.Success)
        {
            if (em.HasBuffer<PathCorner>(entity))
                em.GetBuffer<PathCorner>(entity).Clear();

            query.Dispose();
            em.RemoveComponent<NavPathRequest>(entity);
            return;
        }

        //-------------------------------------------------------------------
        // EndFindPath → dapatkan jumlah polygon
        //-------------------------------------------------------------------
        int pathSize;
        var endStatus = query.EndFindPath(out pathSize);

        if (endStatus != PathQueryStatus.Success || pathSize <= 0)
        {
            if (em.HasBuffer<PathCorner>(entity))
                em.GetBuffer<PathCorner>(entity).Clear();

            query.Dispose();
            em.RemoveComponent<NavPathRequest>(entity);
            return;
        }

        //-------------------------------------------------------------------
        // Ambil PolygonId[]
        //-------------------------------------------------------------------
        var polys = new NativeArray<PolygonId>(pathSize, Allocator.Temp);
        var slice = new NativeSlice<PolygonId>(polys);

        int copied = query.GetPathResult(slice);

        if (copied <= 0)
        {
            polys.Dispose();
            query.Dispose();
            em.RemoveComponent<NavPathRequest>(entity);
            return;
        }

        //-------------------------------------------------------------------
        // Ambil portal antara polygon i → i+1
        //-------------------------------------------------------------------
        var portals = new List<(Vector3 left, Vector3 right)>();
        portals.Capacity = copied - 1;

        for (int i = 0; i < copied - 1; i++)
        {
            PolygonId a = polys[i];
            PolygonId b = polys[i + 1];

            if (query.GetPortalPoints(a, b, out Vector3 left, out Vector3 right))
            {
                portals.Add((left, right));
            }
            else
            {
                portals.Add((startLoc.position, startLoc.position));
            }
        }

        //-------------------------------------------------------------------
        // Jalankan funnel → hasil corner list
        //-------------------------------------------------------------------
        var corners = RunFunnel(startLoc.position, endLoc.position, portals);

        //-------------------------------------------------------------------
        // Tulis ke ECS buffer
        //-------------------------------------------------------------------
        if (!em.HasBuffer<PathCorner>(entity))
            em.AddBuffer<PathCorner>(entity);

        var buf = em.GetBuffer<PathCorner>(entity);
        buf.Clear();

        foreach (var c in corners)
            buf.Add(new PathCorner { pos = new float2(c.x, c.y) });

        //-------------------------------------------------------------------
        // Cleanup
        //-------------------------------------------------------------------
        polys.Dispose();
        query.Dispose();
        em.RemoveComponent<NavPathRequest>(entity);
    }

    //-----------------------------------------------------------------------
    // Funnel algorithm (String Pulling)
    //-----------------------------------------------------------------------
    List<Vector3> RunFunnel(Vector3 start, Vector3 end, List<(Vector3 L, Vector3 R)> portals)
    {
        var result = new List<Vector3>();
        result.Add(start);

        if (portals.Count == 0)
        {
            result.Add(end);
            return result;
        }

        Vector3 apex = start;
        Vector3 left = portals[0].L;
        Vector3 right = portals[0].R;
        int apexIndex = 0;
        int leftIndex = 0;
        int rightIndex = 0;

        for (int i = 1; i < portals.Count; i++)
        {
            var newLeft = portals[i].L;
            var newRight = portals[i].R;

            // RIGHT
            if (TriArea2D(apex, right, newRight) >= 0)
            {
                if (TriArea2D(apex, left, newRight) < 0)
                {
                    right = newRight;
                    rightIndex = i;
                }
                else
                {
                    apex = left;
                    apexIndex = leftIndex;
                    result.Add(apex);

                    left = apex;
                    right = apex;
                    leftIndex = rightIndex = apexIndex;

                    i = apexIndex;
                    continue;
                }
            }

            // LEFT
            if (TriArea2D(apex, left, newLeft) <= 0)
            {
                if (TriArea2D(apex, right, newLeft) > 0)
                {
                    left = newLeft;
                    leftIndex = i;
                }
                else
                {
                    apex = right;
                    apexIndex = rightIndex;
                    result.Add(apex);

                    left = apex;
                    right = apex;
                    leftIndex = rightIndex = apexIndex;

                    i = apexIndex;
                    continue;
                }
            }
        }

        result.Add(end);
        return result;
    }

    float TriArea2D(Vector3 a, Vector3 b, Vector3 c)
    {
        return (b.x - a.x) * (c.y - a.y) -
               (b.y - a.y) * (c.x - a.x);
    }
    bool IsValidLocation(NavMeshLocation loc)
    {
        return !loc.polygon.IsNull();
    }

}
