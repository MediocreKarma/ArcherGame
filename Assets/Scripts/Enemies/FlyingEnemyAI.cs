using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FlyingEnemyAI : EnemyAI
{
    private new CircleCollider2D collider;

    private new void Start()
    {
        base.Start();
        rb.excludeLayers |= LayerMask.GetMask("Platform");
        collider = GetComponentInChildren<CircleCollider2D>();
    }

    protected override void PerformPathUpdate()
    {
        if (!pathingAlgorithm)
        {
            return;
        }
        List<Vector2> newPath;
        Vector2 start = rb.position;
        if (isAggressive)
        {
            Vector2 goal = GetTargetPosition();
            newPath = pathingAlgorithm.ShortestPath(start, goal);
        }
        else
        {
            Vector2 goal = StartPosition;
            newPath = pathingAlgorithm.ShortestPath(start, goal);
        }

        if (newPath != null)
        {
            currentPath = OffsetPathForCollider(newPath, collider.radius * 1.1f);
            pathIndex = 0;
        }
        else
        {
            Debug.LogWarning("No path found");
        }
    }

    private new void Update()
    {
        base.Update();
    }

    void FixedUpdate()
    {
        if (!isAlive)
        {
            return;
        }
        if (!enableMovement)
        {
            return;
        }
        if (currentPath != null && pathIndex < currentPath.Count)
        {
            Vector2 target = currentPath[pathIndex];
            Vector2 currentPosition = rb.position;
            Vector2 direction = (target - currentPosition).normalized;

            float distanceThisFrame = speed * Time.fixedDeltaTime;
            float distanceToTarget = Vector2.Distance(currentPosition, target);

            if (distanceThisFrame >= distanceToTarget)
            {
                rb.MovePosition(target);
                pathIndex++;
            }
            else
            {
                rb.MovePosition(currentPosition + direction * distanceThisFrame);
            }
        }
#if UNITY_EDITOR
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Vector2 from = currentPath[i];
            Vector2 to = currentPath[i + 1];

            Debug.DrawLine(from, to, Color.cyan, Time.fixedDeltaTime);
        }
#endif
    }

    private new void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Platform"))
        {
            Physics2D.IgnoreCollision(collision.collider, collider);
        }
        else
        {
            base.OnCollisionEnter2D(collision);
        }
    }

    private List<Vector2> OffsetPathForCollider(List<Vector2> path, float radius)
    {
        Vector2[] directions = {
            Vector2.up + Vector2.left,
            Vector2.up + Vector2.right,
            Vector2.down + Vector2.left,
            Vector2.down + Vector2.right,
        };
        var adjustedPath = new List<Vector2>();

        for (int i = 0; i < path.Count; i++)
        {
            Vector2 point = path[i];
            if (i == 0 || i == path.Count - 1)
            {
                adjustedPath.Add(point);
                continue;
            }

            Vector2 push = Vector2.zero;

            foreach (var dir in directions)
            {
                RaycastHit2D hit = Physics2D.Raycast(point, dir, radius, LayerMask.GetMask("Level"));
                if (hit.collider != null)
                {
                    push += -dir;
                    break;
                }
            }
            point += push.normalized * radius;
            adjustedPath.Add(point);
        }
        return adjustedPath;
    }
}
 