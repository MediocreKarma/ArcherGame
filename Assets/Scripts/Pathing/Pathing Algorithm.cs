using System.Collections.Generic;
using UnityEngine;

abstract public class PathingAlgorithm : MonoBehaviour
{
    abstract public List<Vector2> ShortestPath(Vector2 start, Vector2 goal, PatherProperties properties = null);
    abstract public bool HasPath(Vector2 start, Vector2 goal, PatherProperties properties = null);

    public class PatherProperties
    {
        public float width;
        public float height;

        public PatherProperties(float width, float height)
        {
            this.width = width;
            this.height = height;
        }
    }
}
