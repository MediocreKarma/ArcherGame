using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.Image;

public class Arc
{
    public Vector2 start;
    public Vector2 end;
    public Vector2 peak;

    public Arc(Vector2 start, Vector2 end, float peakHeight = 1f)
    {
        this.start = start;
        this.end = end;

        float peakY = Mathf.Max(start.y, end.y) + peakHeight;
        peak = new Vector2((start.x + end.x) / 2f, peakY);
    }

    public Vector2 GetPointAt(float t)
    {
        Vector2 a = Vector2.Lerp(start, peak, t);
        Vector2 b = Vector2.Lerp(peak, end, t);
        return Vector2.Lerp(a, b, t);
    }

    public List<Vector2> GenerateJumpArcPoints(int numPoints, bool reversed = false, List<Vector2> buffer = null)
    {
        buffer ??= new(numPoints);
        buffer.Clear();
        if (!reversed)
        {
            for (int i = 0; i <= numPoints; ++i)
            {
                float t = (float)i / numPoints;
                buffer.Add(GetPointAt(t));
            }
        }
        else
        {
            for (int i = numPoints; i >= 0; --i)
            {
                float t = (float)i / numPoints;
                buffer.Add(GetPointAt(t));
            }
        }
        return buffer;
    }

    public float ComputeLength(int segments = 10, List<Vector2> buffer = null)
    {
        if (segments < 1)
        {
            segments = 1; // Ensure at least one segment
        }
        float distance = 0f;
        List<Vector2> points = GenerateJumpArcPoints(segments, false, buffer);
        for (int i = 0; i < points.Count - 1; i++)
        {
            distance += Vector2.Distance(points[i], points[i + 1]);
        }
        return distance;
    }
}