using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;
public class IslandTopDetector : MonoBehaviour
{
    public CompositeCollider2D targetCollider;

    private readonly List<LineSegment> horizontalEdges = new();
    private readonly List<LineSegment> verticalEdges = new();
    public List<LineSegment> TopSurfaces { get; private set; } = new();
    private readonly List<EdgeCornerInfo> topSurfaceCorners = new();

    private void Awake()
    {
        FindHorizontalEdges();
        FindVerticalEdges();
        FindTopSurfaces();
        AnalyzeTopSurfaceCorners();
    }

    private void FindHorizontalEdges()
    {
        horizontalEdges.Clear();
        int pathCount = targetCollider.pathCount;
        for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            Vector2[] points = new Vector2[targetCollider.GetPathPointCount(pathIndex)];
            targetCollider.GetPath(pathIndex, points);

            for (int i = 0; i < points.Length; i++)
            {
                Vector2 p1 = points[i];
                Vector2 p2 = points[(i + 1) % points.Length];

                if (Mathf.Abs(p1.y - p2.y) < 0.01f && (p2.x - p1.x) != 0)
                {
                    horizontalEdges.Add(new LineSegment(p1, p2));
                }
            }
        }
    }

    private void FindVerticalEdges()
    {
        verticalEdges.Clear();
        int pathCount = targetCollider.pathCount;
        for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            Vector2[] points = new Vector2[targetCollider.GetPathPointCount(pathIndex)];
            targetCollider.GetPath(pathIndex, points);

            for (int i = 0; i < points.Length; i++)
            {
                Vector2 p1 = targetCollider.transform.TransformPoint(points[i]);
                Vector2 p2 = targetCollider.transform.TransformPoint(points[(i + 1) % points.Length]);

                if (Mathf.Abs(p1.x - p2.x) < 0.01f && (p1.y != p2.y))
                {
                    verticalEdges.Add(new LineSegment(p1, p2));
                }
            }
        }
    }

    private EdgeCornerInfo AnalyzeCorner(Vector2 corner)
    {
        const float positionTolerance = 0.1f;

        foreach (var vEdge in verticalEdges)
        {
            if (ApproximatelySame(corner.x, vEdge.start.x, positionTolerance) &&
                ApproximatelySame(corner.y, vEdge.start.y, positionTolerance))
            {
                float height = Mathf.Abs(vEdge.end.y - vEdge.start.y);
                bool goesUp = vEdge.end.y > vEdge.start.y;

                return new EdgeCornerInfo(corner, true, goesUp, height);
            }
            else if (ApproximatelySame(corner.x, vEdge.end.x, positionTolerance) &&
                     ApproximatelySame(corner.y, vEdge.end.y, positionTolerance))
            {
                float height = Mathf.Abs(vEdge.end.y - vEdge.start.y);
                bool goesUp = vEdge.start.y > vEdge.end.y;
                return new EdgeCornerInfo(corner, true, goesUp, height);
            }
        }

        return new EdgeCornerInfo(corner, false, false, 0f);
    }

    private void AnalyzeTopSurfaceCorners()
    {
        topSurfaceCorners.Clear();
        var cnt = 0;
        var cnt2 = 0;
        var cnt3 = 0;

        foreach (var edge in TopSurfaces)
        {
            Vector2[] corners = { edge.start, edge.end };

            foreach (var corner in corners)
            {
                EdgeCornerInfo info = AnalyzeCorner(corner);
                topSurfaceCorners.Add(info);
                if (info.wallGoesUp) cnt++;
                if (!info.wallGoesUp && info.hasWall) cnt2++;
                if (!info.hasWall) cnt3++;
            }
        }

        Debug.Log($"Found {cnt} upwards walls and {cnt2} downwards walls and {cnt3} corners with no walls.");
    }

    private bool ApproximatelySame(float a, float b, float tolerance)
    {
        return Mathf.Abs(a - b) < tolerance;
    }

    private bool RayIntersectsSegment(float rayX, Vector2 a, Vector2 b, out Vector2 intersection)
    {
        intersection = Vector2.zero;
        if (Mathf.Approximately(a.x, b.x))
        {
            return false;
        }
        if ((a.x <= rayX && b.x >= rayX) || (b.x <= rayX && a.x >= rayX))
        {
            float t = (rayX - a.x) / (b.x - a.x);

            if (t >= 0f && t <= 1f)
            {
                intersection = Vector2.Lerp(a, b, t);
                return true;
            }
        }

        return false;
    }

    private void FindTopSurfaces()
    {
        TopSurfaces.Clear();

        foreach (var edge in horizontalEdges)
        {
            Vector2 midPoint = (edge.start + edge.end) * 0.5f;
            float rayX = midPoint.x;
            rayX = Mathf.Floor(rayX) + 0.5f;

            List<Vector2> intersections = new();

            foreach (var testEdge in horizontalEdges)
            {
                if (RayIntersectsSegment(rayX, testEdge.start, testEdge.end, out Vector2 hitPoint))
                {
                    intersections.Add(hitPoint);
                }
            }

            if (intersections.Count == 0)
            {
                Debug.LogWarning($"No intersections found at x={midPoint.x}.");
                continue;
            }

            if (intersections.Count % 2 != 0)
            {
                Debug.LogWarning($"Odd number of intersections found at x={midPoint.x}.");
                continue;
            }

            intersections.Sort((a, b) => b.y.CompareTo(a.y));

            for (int i = 0; i < intersections.Count; i += 2)
            {
                Vector2 topHit = intersections[i];

                if (Mathf.Abs(topHit.y - midPoint.y) < 0.5f)
                {
                    Vector2 startWorld = targetCollider.transform.TransformPoint(edge.start);
                    Vector2 endWorld = targetCollider.transform.TransformPoint(edge.end);
                    TopSurfaces.Add(new(startWorld, endWorld));
                    break;
                }
            }
        }

        Debug.Log($"Identified {TopSurfaces.Count} top surfaces.");
    }

    public struct LineSegment
    {
        public Vector2 start;
        public Vector2 end;

        public LineSegment(Vector2 start, Vector2 end)
        {
            this.start = start;
            this.end = end;
        }
    }

    private struct EdgeCornerInfo
    {
        public Vector2 position;
        public bool hasWall;
        public bool wallGoesUp; // true = up, false = down
        public float wallHeight;

        public EdgeCornerInfo(Vector2 pos, bool hasWall, bool wallGoesUp, float wallHeight)
        {
            position = pos;
            this.hasWall = hasWall;
            this.wallGoesUp = wallGoesUp;
            this.wallHeight = wallHeight;
        }
    }
}