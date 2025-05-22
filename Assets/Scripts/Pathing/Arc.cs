using System.Collections.Generic;
using UnityEngine;

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

    public List<Vector2> GenerateJumpArcPoints(int numPoints)
    {
        List<Vector2> points = new();
        for (int i = 0; i <= numPoints; i++)
        {
            float t = (float)i / numPoints;
            points.Add(GetPointAt(t));
        }
        return points;
    }
}