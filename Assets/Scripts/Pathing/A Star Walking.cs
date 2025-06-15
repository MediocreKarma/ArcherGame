using Gamekit2D;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(IslandTopDetector))]
public class PlatformPathGraph : PathingAlgorithm
{
    public List<IslandTopDetector.LineSegment> topSurfaces;
    public List<IslandTopDetector.LineSegment> platformSurfaces = new();
    public float nodeSpacing = 0.5f;
    private float maxSidewaysSpeed = 0f;
    private float maxJumpDistance = 0f;
    public LayerMask levelMask;

    public List<Node> nodes = new();
    public List<Edge> edges = new();
    private readonly List<Node> middlePlatformNodes = new();
    private readonly List<Node> cornerNodes = new();

    private readonly Dictionary<Node, List<Edge>> edgeMap = new();
    private readonly Dictionary<Edge, Arc> jumpArcs = new();
    private readonly Dictionary<Edge, Arc> fallArcs = new();

    private void Start()
    {
        WalkingEnemyAI[] walkers = FindObjectsByType<WalkingEnemyAI>(FindObjectsSortMode.None);
        foreach (var walker in walkers)
        {
            if (walker != null)
            {
                maxSidewaysSpeed = Mathf.Max(maxSidewaysSpeed, walker.Properties.maxSidewaysSpeed);
                maxJumpDistance = Mathf.Max(maxJumpDistance, walker.Properties.maxJumpDistance);
            }
        }
        topSurfaces = GetComponent<IslandTopDetector>().TopSurfaces;
        var platformParent = GameObject.Find("Platforms");
        foreach (var platformCollider in platformParent.GetComponentsInChildren<BoxCollider2D>())
        {
            Bounds bounds = platformCollider.bounds;

            Vector2 topLeft = new(bounds.min.x, bounds.max.y);
            Vector2 topRight = bounds.max;
            platformSurfaces.Add(new IslandTopDetector.LineSegment(topLeft, topRight));
        }
        GenerateGraph();
        BuildEdgeMap();
    }

    private void BuildEdgeMap()
    {
        foreach (var edge in edges)
        {
            if (!edgeMap.ContainsKey(edge.from))
                edgeMap[edge.from] = new List<Edge>();

            edgeMap[edge.from].Add(edge);
        }
    }

    private void MergeSimilarNodes()
    {
        List<Node> mergedNodes = new();

        for (int i = 0; i < nodes.Count; i++)
        {
            Node current = nodes[i];
            bool found = false;

            foreach (var existing in mergedNodes)
            {
                if (Vector2.Distance(current.position, existing.position) < 0.25f)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                mergedNodes.Add(current);
            }
        }
        nodes = mergedNodes;
        int id = 0;
        foreach (var node in nodes)
        {
            node.id = id++;
        }
    }

    private readonly Queue<Node> queue = new();
    private readonly HashSet<Node> visited = new();

    private bool HasWalkPath(Node start, Node end)
    {
        queue.Clear();
        visited.Clear();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Node current = queue.Dequeue();
            if (current == end)
                return true;

            foreach (var edge in edges)
            {
                if (edge.type == EdgeType.Walk && edge.from == current && !visited.Contains(edge.to))
                {
                    visited.Add(edge.to);
                    queue.Enqueue(edge.to);
                }
            }
        }

        return false;
    }
    
    private enum NodeSpecialType
    {
        None,
        Corner,
        MiddlePlatform
    }

    private void AddNodesFromLines(List<IslandTopDetector.LineSegment> lines, NodeSpecialType nodeType = NodeSpecialType.None)
    {
        foreach (var surface in lines)
        {
            float length = Vector2.Distance(surface.start, surface.end);
            int nodeCount = Mathf.RoundToInt(Mathf.Max(2, Mathf.CeilToInt(length / nodeSpacing) + 1));

            for (int i = 0; i < nodeCount; i++)
            {
                float t = (float)i / (nodeCount - 1);
                Vector2 pos = Vector2.Lerp(surface.start, surface.end, t);
                pos.y += 0.05f;
                if (i == 0 || i == nodeCount - 1)
                {
                    Vector2 checkCenter = pos + Vector2.up * 0.25f;
                    Vector2 boxSize = new(0.1f, 0.1f);

                    int levelMask = LayerMask.GetMask("Level");
                    bool blocked = Physics2D.OverlapBox(checkCenter, boxSize, 0f, levelMask);
                    if (blocked)
                    {
                        continue;
                    }
                }
                nodes.Add(new Node(pos, nodes.Count));
                if (nodeType == NodeSpecialType.MiddlePlatform && i != 0 && i < nodeCount - 1)
                {
                    middlePlatformNodes.Add(nodes.Last());
                }
                if ((i == 0 || i == nodeCount - 1) && nodeType == NodeSpecialType.Corner)
                {
                    cornerNodes.Add(nodes.Last());
                }
            }
        }
    }

    private void GenerateGraph()
    {
        nodes.Clear();
        edges.Clear();
        AddNodesFromLines(topSurfaces, NodeSpecialType.Corner);
        AddNodesFromLines(platformSurfaces, NodeSpecialType.MiddlePlatform);
        MergeSimilarNodes();

        openArray = new PathNode?[nodes.Count];
        closedSet = new bool[nodes.Count];
        pathNodeIndexFromNodeId = new int[nodes.Count];

        // walk edges
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            var a = nodes[i];
            for (int j = i + 1; j < nodes.Count; j++)
            {
                var b = nodes[j];
                if (Mathf.Abs(a.position.y - b.position.y) < 0.1f && Vector2.Distance(a.position, b.position) < nodeSpacing * 1.1f)
                {
                    float distance = Vector2.Distance(a.position, b.position);
                    edges.Add(new Edge(a, b, EdgeType.Walk, distance));
                    edges.Add(new Edge(b, a, EdgeType.Walk, distance));
                }
            }
        }

        // fall edges
        foreach (var fallNode in middlePlatformNodes.Concat(cornerNodes))
        {
            foreach (var candidate in nodes)
            {
                if (candidate == fallNode) continue;

                float dx = Mathf.Abs(candidate.position.x - fallNode.position.x);
                float dy = fallNode.position.y - candidate.position.y;

                if (dy <= -0.02f) continue; // Not below

                float gravity = 9.81f;
                float fallTime = Mathf.Sqrt(2f * dy / gravity);
                float maxReachX = maxSidewaysSpeed * fallTime;

                if (dx > maxReachX) continue; // Too far horizontally to reach by falling

                Vector2 origin = fallNode.position + Vector2.up * 0.1f;
                Vector2 target = candidate.position + Vector2.up * 0.1f;

                if (!Physics2D.Linecast(origin, target, levelMask))
                {
                    var arc = new Arc(fallNode.position, candidate.position, 0f);
                    var edge = new Edge(fallNode, candidate, EdgeType.Fall, arc.ComputeLength(10, arcBuffer));
                    edges.Add(edge);
                    fallArcs.Add(edges.Last(), arc);
                }
            }
        }

        // jump edges
        for (int i = 0; i < nodes.Count; ++i)
        {
            var from = nodes[i];
            for (int j = 0; j < nodes.Count; ++j)
            {
                var to = nodes[j];
                float distance = Vector2.Distance(from.position, to.position);

                if (distance <= maxJumpDistance)
                {
                    var hit = Physics2D.Linecast(from.position + new Vector2(0, 0.1f), to.position + new Vector2(0, 0.1f), levelMask);
                    if (!hit && !HasWalkPath(to, from))
                    {
                        var arc = new Arc(from.position, to.position, 3f);
                        var edge = new Edge(from, to, EdgeType.Jump, arc.ComputeLength(10, arcBuffer));
                        edges.Add(edge);
                        jumpArcs.Add(edges.Last(), arc);
                    }
                }
            }
        }

    }

    private void OnDrawGizmosSelected()
    {
        if (nodes == null || edges == null)
            return;

        Gizmos.color = Color.cyan;
        foreach (var node in nodes)
        {
            Gizmos.DrawSphere(node.position, 0.1f);
        }
        foreach (var edge in edges)
        {
            Gizmos.color = edge.type switch
            {
                EdgeType.Walk => Color.green,
                EdgeType.Jump => Color.red,
                EdgeType.Fall => Color.blue,
                _ => Color.gray,
            };
            if (edge.type != EdgeType.Jump)
            {
                Gizmos.DrawLine(edge.from.position, edge.to.position);
            }
        }
        Gizmos.color = Color.yellow;
        if (jumpArcs != null)
        {
            foreach (var pair in jumpArcs)
            {
                Arc arc = pair.Value;

                const int segments = 20;
                Vector2 prev = arc.GetPointAt(0);
                for (int i = 1; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    Vector2 next = arc.GetPointAt(t);
                    Gizmos.DrawLine(prev, next);
                    prev = next;
                }
            }
        }
    }

    public override List<Vector2> ShortestPath(Vector2 start, Vector2 goal, PathingAlgorithm.PatherProperties properties = null)
    {
        return ShortestPath(start, goal, properties, new());
    }

    public List<Vector2> ShortestPath(Vector2 start, Vector2 goal, PathingAlgorithm.PatherProperties properties = null, List<Vector2> buffer = null)
    {
        WalkerProperties walkerProperties;
        if (properties is not WalkerProperties)
            walkerProperties = new WalkerProperties(1, 1, 100, 100);
        else
            walkerProperties = new WalkerProperties(properties.width, properties.height, 100, 100);
        return InternalShortestPath(start, goal, walkerProperties, new());
    }

    public List<Vector2> ShortestPath(Vector2 start, Vector2 goal, WalkerProperties properties = null, List<Vector2> buffer = null)
    {
        properties ??= new WalkerProperties(1, 1, 100, 100);
        buffer ??= new();
        return InternalShortestPath(start, goal, properties, buffer);
    }

    //public List<PathNode> ShortestPathWithNodes(Vector2 start, Vector2 goal, WalkerProperties properties)
    //{
    //    properties ??= new WalkerProperties(1, 1, 100, 100);
    //    //return InternalShortestPath(start, goal, properties);
    //    return null;
    //}

    private readonly PriorityQueue<PathNode> openSet = new(Comparer<PathNode>.Create((x, y) => {
        int cmp = x.fCost.CompareTo(y.fCost);
        return cmp != 0 ? cmp : x.node.id.CompareTo(y.node.id);
    }));
    private bool[] closedSet;
    private PathNode?[] openArray;
    private readonly List<PathNode> pathNodesList = new();
    private int[] pathNodeIndexFromNodeId;

    private List<Vector2> InternalShortestPath(Vector2 start, Vector2 goal, WalkerProperties properties, List<Vector2> buffer)
    {
        buffer.Clear();
        Bounds walkerBounds = new(
            new Vector2(0, properties.height / 2f),
            new Vector2(properties.width, properties.height - 0.1f)
        );

        Node startNode = FindClosestNodeBelow(start);
        Node goalNode = FindClosestNodeBelow(goal);

        openSet.Clear();
        Array.Fill(closedSet, false);
        Array.Fill(openArray, null);
        pathNodesList.Clear();

        var startPathNode = new PathNode
        {
            node = startNode,
            parentIndex = -1,
            gCost = 0f,
            fCost = Vector2.Distance(startNode.position, goalNode.position)
        };

        pathNodesList.Add(startPathNode);
        pathNodeIndexFromNodeId[startPathNode.node.id] = 0;

        openSet.Enqueue(startPathNode);
        openArray[startNode.id] = startPathNode;

        while (openSet.Count > 0)
        {
            PathNode current = openSet.Dequeue();

            if (closedSet[current.node.id])
                continue;

            closedSet[current.node.id] = true;
            openArray[current.node.id] = null;

            if (current.node.id == goalNode.id)
                return ReconstructPath(current, start, goal, buffer);

            if (!edgeMap.TryGetValue(current.node, out var neighbors))
                continue;

            foreach (var edge in neighbors)
            {
                Node neighbor = edge.to;

                if (closedSet[neighbor.id])
                    continue;
                if (!IsPositionValid(neighbor.position, walkerBounds))
                    continue;

                float dy = neighbor.position.y - current.node.position.y;
                float dx = Mathf.Abs(neighbor.position.x - current.node.position.x);

                if (edge.type == EdgeType.Jump && (Mathf.Sqrt(dx * dx + dy * dy) > properties.maxJumpDistance || !IsArcValid(jumpArcs.GetValueOrDefault(edge), walkerBounds)))
                    continue;

                float gravity = Mathf.Abs(Physics2D.gravity.y);
                float fallTime = Mathf.Sqrt(2f * dy / gravity);
                float maxFallDistance = properties.maxSidewaysSpeed * fallTime;

                if (edge.type == EdgeType.Fall && (dx > maxFallDistance || !IsArcValid(fallArcs.GetValueOrDefault(edge), walkerBounds)))
                    continue;

                float tentativeG = current.gCost + Vector2.Distance(current.node.position, neighbor.position);

                if (!openArray[neighbor.id].HasValue || tentativeG < openArray[neighbor.id].Value.gCost)
                {
                    PathNode newNode = new()
                    {
                        node = neighbor,
                        parentIndex = pathNodeIndexFromNodeId[current.node.id],
                        gCost = tentativeG,
                        fCost = tentativeG + Vector2.Distance(neighbor.position, goalNode.position),
                        connectingEdge = edge
                    };
                    openArray[neighbor.id] = newNode;
                    openSet.Enqueue(newNode);
                    pathNodesList.Add(newNode);
                    pathNodeIndexFromNodeId[neighbor.id] = pathNodesList.Count - 1;
                }
            }
        }

        return buffer; // No path found
    }

    private readonly List<Vector2> arcBuffer = new(10);

    private List<Vector2> ReconstructPath(PathNode endNode, Vector2 start, Vector2 goal, List<Vector2> buffer)
    {
        buffer.Add(goal);
        PathNode current = endNode;
        while (current.parentIndex != -1)
        {
            switch (current.connectingEdge.type)
            {
                case EdgeType.Walk:
                    Vector2 to = current.node.position;
                    buffer.Add(to);
                    break;
                case EdgeType.Fall:
                {
                    List<Vector2> arc = fallArcs[current.connectingEdge].GenerateJumpArcPoints(10, true, arcBuffer);
                    buffer.AddRange(arc);
                    break;
                }
                case EdgeType.Jump:
                {
                    List<Vector2> arc = jumpArcs[current.connectingEdge].GenerateJumpArcPoints(10, true, arcBuffer);
                    buffer.AddRange(arc);
                    break;
                }
            }
            current = pathNodesList[current.parentIndex];
        }
        buffer.Add(start);
        buffer.Reverse();
        return buffer;
    }

    private Node FindClosestNodeBelow(Vector2 position, float horizontalTolerance = 3f)
    {
        Node closest = null;
        float minDist = float.MaxValue;

        foreach (var node in nodes)
        {
            bool isBelow = position.y - node.position.y >= -0.5f;

            if (isBelow)
            {
                float dist = Vector2.Distance(position, node.position);
                if (dist < minDist)
                {
                    closest = node;
                    minDist = dist;
                }
            }
        }

        return closest;
    }


    public override bool HasPath(Vector2 start, Vector2 goal, PatherProperties properties = null)
    {
        throw new System.NotImplementedException();
    }

    public bool HasPath(Vector2 start, Vector2 goal, WalkerProperties properties = null)
    {
        throw new System.NotImplementedException();
    }

    private bool IsPositionValid(Vector2 position, Bounds walkerBounds)
    {
        Vector3 center = walkerBounds.center + (Vector3)position;
        Vector3 size = walkerBounds.size;

        // Draw rectangle (2D box) in the XY plane
        Vector3 halfSize = size * 0.5f;
        Vector3 topLeft = center + new Vector3(-halfSize.x, halfSize.y);
        Vector3 topRight = center + new Vector3(halfSize.x, halfSize.y);
        Vector3 bottomLeft = center + new Vector3(-halfSize.x, -halfSize.y);
        Vector3 bottomRight = center + new Vector3(halfSize.x, -halfSize.y);

        Debug.DrawLine(topLeft, topRight, Color.yellow, 0.1f);
        Debug.DrawLine(topRight, bottomRight, Color.yellow, 0.1f);
        Debug.DrawLine(bottomRight, bottomLeft, Color.yellow, 0.1f);
        Debug.DrawLine(bottomLeft, topLeft, Color.yellow, 0.1f);
        return !Physics2D.OverlapBox(walkerBounds.center + (Vector3)position, walkerBounds.size, 0f, levelMask);
    }

    private bool IsArcValid(Arc arc, Bounds walkerBounds, int segments = 10)
    {
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector2 pos = arc.GetPointAt(t);
            if (!IsPositionValid(pos, walkerBounds))
                return false;
        }
        return true;
    }

    private bool IsEdgeValid(Vector2 a, Vector2 b, Bounds walkerBounds, int segments = 4)
    {
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector2 samplePos = Vector2.Lerp(a, b, t);
            Vector2 center = samplePos + (Vector2)walkerBounds.center;
            Vector2 size = walkerBounds.size;
            if (Physics2D.OverlapBox(center, size, 0f, levelMask))
            {
                return false;
            }
        }
        return true;
    }

    public class Node
    {
        public int id;
        public Vector2 position;
        public Node(Vector2 pos, int id) { position = pos; this.id = id; }
    }

    public enum EdgeType { Walk, Jump, Fall }

    public class Edge
    {
        public Node from, to;
        public float distance;
        public EdgeType type;
        public Edge(Node from, Node to, EdgeType type, float distance)
        {
            this.from = from;
            this.to = to;
            this.type = type;
            this.distance = distance;
        }
    }

    public class WalkerProperties : PathingAlgorithm.PatherProperties
    {
        public float maxJumpDistance;
        public float maxSidewaysSpeed;

        public WalkerProperties(float width, float height, float maxJumpDistance, float maxSidewaysSpeed) : base(width, height)
        {
            this.maxJumpDistance = maxJumpDistance;
            this.maxSidewaysSpeed = maxSidewaysSpeed;
        }
    }

    public struct PathNode
    {
        public Node node;
        public int parentIndex;
        public float gCost;
        public float fCost;
        public Edge connectingEdge;
    }
}