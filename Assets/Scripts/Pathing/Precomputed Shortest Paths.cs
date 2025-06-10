using NUnit.Framework;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PrecomputedShortestPathAlgorithm : PathingAlgorithm
{
    public CompositeCollider2D targetCollider;
    public float raycastOffset = 0.01f;

    private List<Vector2> insideCorners = new();
    private List<Vector2> outsideCorners = new();

    private readonly Dictionary<Vector2, Dictionary<Vector2, float>> insideGraph = new();
    private readonly Dictionary<Vector2, Dictionary<Vector2, float>> outsideGraph = new();

    private readonly Dictionary<(Vector2, Vector2), List<Vector2>> insideShortestPaths = new();
    private readonly Dictionary<(Vector2, Vector2), List<Vector2>> outsideShortestPaths = new();

    private readonly Dictionary<(Vector2, Vector2), float> insidePathLengths = new();
    private readonly Dictionary<(Vector2, Vector2), float> outsidePathLengths = new();

    void Start()
    {
        FindCorners();
        Debug.Assert(insideCorners.Count == outsideCorners.Count, "Inside and outside corners should be equal in count.");

        BuildVisibilityGraph(insideCorners, insideGraph);
        BuildVisibilityGraph(outsideCorners, outsideGraph);

        PrecomputeShortestPaths(insideCorners, insideGraph, insideShortestPaths, insidePathLengths);
        PrecomputeShortestPaths(outsideCorners, outsideGraph, outsideShortestPaths, outsidePathLengths);
    }

    void FindCorners()
    {
        float offsetAmount = 0.25f;
        List<Vector2> corners = new();
        int pathCount = targetCollider.pathCount;
        for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            Vector2[] path = new Vector2[targetCollider.GetPathPointCount(pathIndex)];
            targetCollider.GetPath(pathIndex, path);

            for (int i = 0; i < path.Length; i++)
            {
                Vector2 prev = path[(i - 1 + path.Length) % path.Length];
                Vector2 current = path[i];
                Vector2 next = path[(i + 1) % path.Length];

                Vector2 worldPrev = targetCollider.transform.TransformPoint(prev);
                Vector2 worldCurrent = targetCollider.transform.TransformPoint(current);
                Vector2 worldNext = targetCollider.transform.TransformPoint(next);

                Vector2 toPrev = (worldPrev - worldCurrent).normalized;
                Vector2 toNext = (worldNext - worldCurrent).normalized;
                Vector2 cornerNormal = (toPrev + toNext).normalized;

                Vector2 pushed1 = worldCurrent - cornerNormal * offsetAmount;
                Vector2 pushed2 = worldCurrent + cornerNormal * offsetAmount;

                if (!corners.Contains(pushed1)) corners.Add(pushed1);
                if (!corners.Contains(pushed2)) corners.Add(pushed2);
            }
        }
        HashSet<Vector2> visited = new();
        Queue<Vector2> queue = new();

        queue.Enqueue(corners[0]);
        visited.Add(corners[0]);

        while (queue.Count > 0)
        {
            Vector2 current = queue.Dequeue();

            foreach (var other in corners)
            {
                if (visited.Contains(other)) continue;
                if (IsVisible(current, other))
                {
                    visited.Add(other);
                    queue.Enqueue(other);
                }
            }
        }

        foreach (var pt in corners)
        {
            if (visited.Contains(pt))
                outsideCorners.Add(pt); 
            else
                insideCorners.Add(pt);
        }
        float closestPointInside = 0f;
        float closestPointOutside = 0f;

        var player = GameObject.Find("Player");
        foreach (var pt in insideCorners)
        {
            float dist = Vector2.Distance(pt, player.transform.position);
            if (dist < closestPointInside || closestPointInside == 0f)
                closestPointInside = dist;
        }
        foreach (var pt in outsideCorners)
        {
            float dist = Vector2.Distance(pt, player.transform.position);
            if (dist < closestPointOutside || closestPointOutside == 0f)
                closestPointOutside = dist;
        }
        if (closestPointInside < closestPointOutside)
        {
            (outsideCorners, insideCorners) = (insideCorners, outsideCorners);
        }

        startVisible = new Vector2[outsideCorners.Count];
        goalVisible = new Vector2[outsideCorners.Count];

#if UNITY_EDITOR
        foreach (var pt in insideCorners)
        {
            DebugDrawCircle(pt, 0.02f, Color.red, 1000f);
        }
        foreach (var pt in outsideCorners)
        {
            DebugDrawCircle(pt, 0.02f, Color.blue, 1000f);
        }
#endif
    }

#if UNITY_EDITOR
    void DebugDrawCircle(Vector2 center, float radius, Color color, float duration)
    {
        int segments = 16;
        float angle = 0f;
        Vector2 prevPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        for (int i = 1; i <= segments; i++)
        {
            angle = i * Mathf.PI * 2f / segments;
            Vector2 newPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Debug.DrawLine(prevPoint, newPoint, color, duration);
            prevPoint = newPoint;
        }
    }
#endif

    void BuildVisibilityGraph(List<Vector2> corners, Dictionary<Vector2, Dictionary<Vector2, float>> graph)
    {
        foreach (var from in corners)
        {
            graph[from] = new Dictionary<Vector2, float>();
            foreach (var to in corners)
            {
                if (from == to) continue;
                if (IsVisible(from, to))
                {
                    float dist = Vector2.Distance(from, to);
                    graph[from][to] = dist;
                }
            }
        }
    }

    public bool IsVisible(Vector2 a, Vector2 b)
    {
        var hits = Physics2D.Linecast(a + Vector2.up * raycastOffset, b + Vector2.up * raycastOffset, 1 << targetCollider.gameObject.layer);
        if (hits.collider == targetCollider)
        {
            return false;
        }
        return true;
    }


    void PrecomputeShortestPaths(
        List<Vector2> corners,
        Dictionary<Vector2, Dictionary<Vector2, float>> graph,
        Dictionary<(Vector2, Vector2), List<Vector2>> shortestPaths,
        Dictionary<(Vector2, Vector2), float> pathLengths
    )
    {
        var dist = new Dictionary<(Vector2, Vector2), float>();
        var next = new Dictionary<(Vector2, Vector2), Vector2?>();

        foreach (var i in corners)
        {
            foreach (var j in corners)
            {
                if (i == j) continue;

                var key = (i, j);
                if (graph.ContainsKey(i) && graph[i].ContainsKey(j))
                {
                    dist[key] = graph[i][j];
                    next[key] = j;
                }
                else
                {
                    dist[key] = Mathf.Infinity;
                    next[key] = null;
                }
            }
        }

        foreach (var k in corners)
        {
            foreach (var i in corners)
            {
                foreach (var j in corners)
                {
                    if (i == j || i == k || j == k) continue;

                    var ik = (i, k);
                    var kj = (k, j);
                    var ij = (i, j);

                    float alt = dist[ik] + dist[kj];

                    if (alt < dist[ij])
                    {
                        dist[ij] = alt;
                        next[ij] = next[ik];
                    }
                }
            }
        }

        foreach (var i in corners)
        {
            foreach (var j in corners)
            {
                if (i == j || next[(i, j)] == null) continue;

                var path = new List<Vector2> { i };
                var current = i;
                while (current != j)
                {
                    current = next[(current, j)].Value;
                    path.Add(current);
                }
                shortestPaths[(i, j)] = path;
                float totalLength = 0f;
                for (int p = 0; p < path.Count - 1; p++)
                {
                    totalLength += Vector2.Distance(path[p], path[p + 1]);
                }
                pathLengths[(i, j)] = totalLength;
            }
        }
    }

    private Vector2[] startVisible, goalVisible;
    private int startVisibleCount = 0, goalVisibleCount = 0;

    private bool IsOutside(Vector2 point)
    {
        foreach (var corner in outsideCorners)
        {
            if (IsVisible(point, corner))
            {
                return true;
            }
        }
        return false;
    }

    private void LoadStartAndGoalLists(Vector2 start, Vector2 goal, List<Vector2> corners)
    {
        goalVisibleCount = 0;
        startVisibleCount = 0;
        foreach (var corner in corners)
        {
            if (IsVisible(start, corner))
                startVisible[startVisibleCount++] = corner;
            if (IsVisible(goal, corner))
                goalVisible[goalVisibleCount++] = corner;
        }
    }

    private (Vector2, Vector2) BestFromAndTo(Vector2 start, Vector2 goal, Dictionary<(Vector2, Vector2), float> pathLengths)
    {
        float bestCost = Mathf.Infinity;
        Vector2 bestFrom = Vector2.zero;
        Vector2 bestTo = Vector2.zero;

        for (int i = 0; i < startVisibleCount; i++)
        {
            var from = startVisible[i];
            for (int j = 0; j < goalVisibleCount; j++)
            {
                var to = goalVisible[j];
                float cost = Vector2.Distance(start, from) +
                            pathLengths.GetValueOrDefault((from, to), 0) +
                            Vector2.Distance(to, goal);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestFrom = from;
                    bestTo = to;
                }
            }
        }

        return (bestFrom, bestTo);
    }

    public override List<Vector2> ShortestPath(Vector2 start, Vector2 goal, PatherProperties _properties = null)
    {
        if (IsVisible(start, goal))
        {
            return new List<Vector2> { start, goal };
        }

        bool isOutsideStart = IsOutside(start);
        bool isOutsideGoal = IsOutside(goal);
        if (isOutsideStart != isOutsideGoal)
        {
            return new List<Vector2> { start, goal };
        }

        var corners = insideCorners;
        var shortestPaths = insideShortestPaths;
        var pathLengths = insidePathLengths;
        if (isOutsideStart)
        {
            corners = outsideCorners;
            shortestPaths = outsideShortestPaths;
            pathLengths = outsidePathLengths;
        }

        LoadStartAndGoalLists(start, goal, corners);
        (Vector2 bestFrom, Vector2 bestTo) = BestFromAndTo(start, goal, pathLengths);

        List<Vector2> bestPath = new() { start };
        if (bestFrom == bestTo)
        {
            bestPath.Add(bestFrom);
        }
        else
        {
            bestPath.AddRange(shortestPaths[(bestFrom, bestTo)]);
        }
        bestPath.Add(goal);
        return bestPath;
    }

    public List<Vector2> ShortestPath(Vector2 start, Vector2 goal, PatherProperties _, List<Vector2> buffer)
    {
        buffer.Clear();
        if (IsVisible(start, goal))
        {
            buffer.Add(start);
            buffer.Add(goal);
            return buffer;
        }
        bool isOutsideStart = IsOutside(start);
        bool isOutsideGoal = IsOutside(goal);
        if (isOutsideStart != isOutsideGoal)
        {
            buffer.Add(start);
            buffer.Add(goal);
            return buffer;
        }

        var corners = insideCorners;
        var shortestPaths = insideShortestPaths;
        var pathLengths = insidePathLengths;
        if (isOutsideStart)
        {
            corners = outsideCorners;
            shortestPaths = outsideShortestPaths;
            pathLengths = outsidePathLengths;
        }

        LoadStartAndGoalLists(start, goal, corners);
        (Vector2 bestFrom, Vector2 bestTo) = BestFromAndTo(start, goal, pathLengths);

        buffer.Add(start);
        if (bestFrom == bestTo)
        {
            buffer.Add(bestFrom);
        }
        else
        {
            buffer.AddRange(shortestPaths[(bestFrom, bestTo)]);
        }
        buffer.Add(goal);
        return buffer;
    }

    public override bool HasPath(Vector2 start, Vector2 goal, PatherProperties _properties)
    {
        if (IsVisible(start, goal))
        {
            return true;
        }
        bool visibleByStart = false;
        bool visibleByGoal = false;
        foreach (var corner in outsideCorners)
        {
            if (IsVisible(start, corner))
                visibleByStart = true;
            if (IsVisible(goal, corner))
                visibleByGoal = true;
        }
        return visibleByGoal == visibleByStart;
    }
}
