using Gamekit2D;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(IslandTopDetector))]
public class PlatformPathGraph : PathingAlgorithm
{
    public List<IslandTopDetector.LineSegment> topSurfaces;
    public List<IslandTopDetector.LineSegment> platformSurfaces = new();
    public float nodeSpacing = 1f;
    public float maxSidewaysFallDistance = 1.1f;
    public float maxJumpDistance = 6f;
    public LayerMask levelMask;

    public List<Node> nodes = new();
    public List<Edge> edges = new();
    private readonly List<Node> middlePlatformNodes = new();
    private readonly List<Node> cornerNodes = new();

    private Dictionary<Node, List<Edge>> edgeMap;

    private void Start()
    {
        topSurfaces = GetComponent<IslandTopDetector>().topSurfaces;
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
        GenerateAllJumpArcs();
    }

    private void BuildEdgeMap()
    {
        edgeMap = new Dictionary<Node, List<Edge>>();
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
        Debug.Log($"Removed nodes: {nodes.Count - mergedNodes.Count}");
        nodes = mergedNodes;
    }

    private bool HasWalkPath(Node start, Node end)
    {
        Queue<Node> queue = new();
        HashSet<Node> visited = new();
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
                nodes.Add(new Node(pos));
                if (nodeType == NodeSpecialType.MiddlePlatform && i != 0 && i < nodeCount - 1)
                {
                    middlePlatformNodes.Add(nodes.Last());
                }
                if (nodeType == NodeSpecialType.Corner && (i == 0 || i == nodeCount - 1))
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

        // walk edges
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            var a = nodes[i];
            for (int j = i + 1; j < nodes.Count; j++)
            {
                var b = nodes[j];
                if (Mathf.Abs(a.position.y - b.position.y) < 0.1f && Vector2.Distance(a.position, b.position) < nodeSpacing * 1.1f)
                {
                    edges.Add(new Edge(a, b, EdgeType.Walk));
                    edges.Add(new Edge(b, a, EdgeType.Walk));
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

                if (dy <= 0.02f) continue; // Not below
                if (dx > maxSidewaysFallDistance) continue; // Too far horizontally

                Vector2 origin = fallNode.position + Vector2.up * 0.1f;
                Vector2 target = candidate.position + Vector2.up * 0.1f;

                if (!Physics2D.Linecast(origin, target, levelMask))
                {
                    edges.Add(new Edge(fallNode, candidate, EdgeType.Fall));
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
                    if (!hit && !HasWalkPath(to, from) && !edges.Any(x => x.from == from && x.to == to && x.type == EdgeType.Fall))
                    {
                        edges.Add(new Edge(from, to, EdgeType.Jump));
                    }
                }
            }
        }

        Debug.Log($"Generated {nodes.Count} nodes and {edges.Count} edges.");
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
                JumpArc arc = pair.Value;

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
        WalkerProperties walkerProperties;
        if (properties is not WalkerProperties)
            walkerProperties = new WalkerProperties(1, 1, 100, 100);
        else
            walkerProperties = new WalkerProperties(properties.width, properties.height, 100, 100);
        return InternalShortestPath(start, goal, walkerProperties);
    }

    public List<Vector2> ShortestPath(Vector2 start, Vector2 goal, WalkerProperties properties = null)
    {
        properties ??= new WalkerProperties(1, 1, 100, 100);
        return InternalShortestPath(start, goal, properties);
    }

    private List<Vector2> InternalShortestPath(Vector2 start, Vector2 goal, WalkerProperties properties)
    {
        Bounds walkerBounds = new(
            new Vector2(0, properties.height / 2f),
            new Vector2(properties.width, properties.height - 0.1f)
        );
        Node startNode = FindClosestNode(start);
        Node goalNode = FindClosestNode(goal);

        var openSet = new SortedSet<PathNode>(Comparer<PathNode>.Create((x, y) => {
            var costComparison = x.fCost.CompareTo(y.fCost);
            if (costComparison == 0)
            {
                return x.node.GetHashCode().CompareTo(y.node.GetHashCode());
            }
            return costComparison;
        }));
        var closedSet = new HashSet<Node>();
        var openDict = new Dictionary<Node, PathNode>();

        var startPathNode = new PathNode
        {
            node = startNode,
            parent = null,
            gCost = 0f,
            fCost = Vector2.Distance(startNode.position, goalNode.position)
        };
        openSet.Add(startPathNode);
        openDict[startNode] = startPathNode;

        while (openSet.Count > 0)
        {
            PathNode current = openSet.Min;
            openSet.Remove(current);
            openDict.Remove(current.node);

            if (current.node == goalNode)
                return ReconstructPath(current, start, goal);

            closedSet.Add(current.node);

            if (!edgeMap.TryGetValue(current.node, out var neighbors))
                continue;

            foreach (var edge in neighbors)
            {
                Node neighbor = edge.to;
                if (closedSet.Contains(neighbor))
                    continue;
                if (!IsPositionValid(neighbor.position, walkerBounds))
                    continue;
                if (edge.type == EdgeType.Jump && !IsArcValid(jumpArcs.GetValueOrDefault(edge), walkerBounds))
                    continue;
                if (edge.type == EdgeType.Fall && !IsEdgeValid(current.node.position, neighbor.position, walkerBounds))
                    continue;

                float tentativeG = current.gCost + Vector2.Distance(current.node.position, neighbor.position);

                if (!openDict.TryGetValue(neighbor, out var neighborNode))
                {
                    neighborNode = new PathNode
                    {
                        node = neighbor,
                        parent = current,
                        gCost = tentativeG,
                        fCost = tentativeG + Vector2.Distance(neighbor.position, goalNode.position),
                        connectingEdgeType = edge.type
                    };
                    openSet.Add(neighborNode);
                    openDict[neighbor] = neighborNode;
                }
                else if (tentativeG < neighborNode.gCost)
                {
                    openSet.Remove(neighborNode);
                    neighborNode.gCost = tentativeG;
                    neighborNode.parent = current;
                    neighborNode.fCost = tentativeG + Vector2.Distance(neighbor.position, goalNode.position);
                    neighborNode.connectingEdgeType = edge.type;
                    openSet.Add(neighborNode); 
                }
            }
        }

        return new List<Vector2>(); // No path found
    }

    private List<Vector2> ReconstructPath(PathNode endNode, Vector2 start, Vector2 goal)
    {
        var path = new List<Vector2> { goal };
        PathNode current = endNode;
        while (current != null)
        {
            Vector2 from = start;
            if (current.parent != null)
                from = current.parent.node.position;
            Vector2 to = current.node.position;

            switch (current.connectingEdgeType)
            {
                case EdgeType.Walk:
                case EdgeType.Fall:
                    path.Add(to);
                    break;

                case EdgeType.Jump:
                    List<Vector2> arc = new JumpArc(to, from).GenerateJumpArcPoints(10);
                    path.AddRange(arc);
                    break;
            }

            current = current.parent;
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    private Node FindClosestNode(Vector2 position)
    {
        Node closest = null;
        float minDist = float.MaxValue;

        foreach (var node in nodes)
        {
            float dist = Vector2.Distance(position, node.position);
            if (dist < minDist)
            {
                closest = node;
                minDist = dist;
            }
        }
        return closest;
    }


    public override bool HasPath(Vector2 start, Vector2 goal, PatherProperties? properties = null)
    {
        throw new System.NotImplementedException();
    }

    public bool HasPath(Vector2 start, Vector2 goal, WalkerProperties? properties = null)
    {
        throw new System.NotImplementedException();
    }


    private readonly Dictionary<Edge, JumpArc> jumpArcs = new();

    private void GenerateJumpArcFor(Edge edge)
    {
        if (edge.type == EdgeType.Jump)
        {
            var arc = new JumpArc(edge.from.position, edge.to.position);
            jumpArcs[edge] = arc;
        }
    }

    private void GenerateAllJumpArcs()
    {
        jumpArcs.Clear();
        foreach (var edge in edges)
        {
            GenerateJumpArcFor(edge);
        }
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

    private bool IsArcValid(JumpArc arc, Bounds walkerBounds, int segments = 10)
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
        public Vector2 position;
        public Node(Vector2 pos) { position = pos; }
    }

    public enum EdgeType { Walk, Jump, Fall }

    public class Edge
    {
        public Node from, to;
        public EdgeType type;
        public Edge(Node from, Node to, EdgeType type)
        {
            this.from = from;
            this.to = to;
            this.type = type;
        }
    }

    public class WalkerProperties : PathingAlgorithm.PatherProperties
    {
        public float maxJumpHeight;
        public float maxSpeed;

        public WalkerProperties(float width, float height, float maxJumpHeight, float maxSpeed) : base(width, height)
        {
            this.maxJumpHeight = maxJumpHeight;
            this.maxSpeed = maxSpeed;
        }
    }

    private class PathNode
    {
        public Node node;
        public PathNode parent;
        public float gCost;
        public float fCost;
        public EdgeType connectingEdgeType;
    }
}