using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WalkingEnemyAI : EnemyAI
{
    private List<Vector2> currentPath = new();
    private int pathIndex = 0;

    private float pathTimer = 0f;
    private const float pathInterval = 0.333f;
    public float aggroDistance = 10f;
    private PlatformPathGraph.WalkerProperties walkerProperties;

    private new void Start()
    {
        base.Start();
        rb.excludeLayers |= LayerMask.GetMask("Platform");
        pathingAlgorithm = FindFirstObjectByType<PlatformPathGraph>();
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        if (colliders.Length == 0)
        {
            walkerProperties = new PlatformPathGraph.WalkerProperties(0.1f, 0.1f, 10f, speed);
        }
        else
        {
            Bounds combinedBounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                combinedBounds.Encapsulate(colliders[i].bounds);
            }
            Vector2 center = transform.position;
            float top = combinedBounds.max.y - center.y;
            float bottom = center.y - combinedBounds.min.y;
            float height = top - bottom;
            float width = combinedBounds.size.x;
            walkerProperties = new PlatformPathGraph.WalkerProperties(width, height, 10f, speed);
            Debug.Log($"Collider bounds: width: {width}, height: {height}");
        }
    }

    private new void Update()
    {
        base.Update();
        if (!isAlive)
        {
            rb.gravityScale = 4f;
            return;
        }
        if (!enableMovement)
        {
            return;
        }
        if (!IsGrounded())
        {
            return;
        }
        UpdatePath();
        UpdateAggro();
    }

    private void UpdatePath()
    {
        pathTimer -= Time.deltaTime;
        if (pathTimer > 0f)
        {
            return;
        }
        pathTimer = pathInterval;
        List<Vector2> newPath;
        Vector2 start = rb.position;
        if (isAggressive)
        {
            Vector2 goal = GetTargetPosition();
            newPath = pathingAlgorithm.ShortestPath(start, goal, walkerProperties);
        }
        else
        {
            Vector2 goal = StartPosition;
            newPath = pathingAlgorithm.ShortestPath(start, goal, walkerProperties);
        }

        if (newPath != null)
        {
            //currentPath = OffsetPathForCollider(newPath, collider.radius * 1.1f);
            currentPath = newPath;
            pathIndex = 2;
        }
        else
        {
            Debug.LogWarning("No path found");
        }
    }

    private void UpdateAggro()
    {
        if (!isAggressive)
        {
            float distanceToPlayer = Vector2.Distance(rb.position, playerTransform.position);
            if (distanceToPlayer < aggroDistance && !player.IsDead)
            {
                isAggressive = true;
            }
        }
        else
        {
            isAggressive = !player.IsDead;
        }
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
            if (direction.x < 0f && isFacingRight)
            {
                RotateSprite();
            }
            else if (direction.x > 0f && !isFacingRight)
            {
                RotateSprite();
            }

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

    bool IsGrounded()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, 0.05f);
    }
}
