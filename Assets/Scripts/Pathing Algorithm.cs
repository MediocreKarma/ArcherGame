using System.Collections.Generic;
using UnityEngine;

abstract public class PathingAlgorithm : MonoBehaviour
{
    abstract public List<Vector2> ShortestPath(Vector2 start, Vector2 goal);
    abstract public bool HasPath(Vector2 start, Vector2 goal);
}
