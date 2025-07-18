using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkingEnemyAI : EnemyAI
{
    private PlatformPathGraph.WalkerProperties walkerProperties;
    public PlatformPathGraph.WalkerProperties Properties => walkerProperties;
    private PlatformPathGraph platformPathingAlgorithm;
    [SerializeField] private LayerMask groundLayerMask;
    public float maxJumpHeight = 10f;

    private float movedDistance = 0f;
    [SerializeField] private float MoveDistanceWalkSound = 1f;
    private Vector2 previousPosition;
    private AudioSource walkingAudio;
    [SerializeField] private AudioClip moveSound;

    private void Awake()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        if (colliders.Length == 0)
        {
            walkerProperties = new PlatformPathGraph.WalkerProperties(0.1f, 0.1f, maxJumpHeight, speed);
        }
        else
        {
            Bounds combinedBounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                combinedBounds.Encapsulate(colliders[i].bounds);
            }
            float width = combinedBounds.size.x;
            float height = combinedBounds.size.y;
            walkerProperties = new PlatformPathGraph.WalkerProperties(width, height, maxJumpHeight, speed);
        }
    }

    private new void Start()
    {
        base.Start();
        rb.excludeLayers |= LayerMask.GetMask("Platform");
        if (base.pathingAlgorithm is PlatformPathGraph graph)
        {
            platformPathingAlgorithm = graph;
        }
        else
        {
            return;
        }
        pathIndex = 0;
        previousPosition = rb.position;
        walkingAudio = GetComponents<AudioSource>()[1];
        walkingAudio.volume = 0.5f;
    }

    private new void Update()
    {
        base.Update();
    }

    protected override void UpdatePath()
    {
        pathTimer -= Time.deltaTime;
        if (pathTimer > 0f)
        {
            return;
        }
        if (!IsGrounded())
        {
            return;
        }
        pathTimer = pathInterval;
        PerformPathUpdate();
    }

    bool AreListsAlmostEqual(List<Vector2> list1, List<Vector2> list2, float tolerance = 0.01f)
    {
        if (list1.Count != list2.Count)
        {
            return false;
        }
        for (int i = 0; i < list1.Count; i++)
        {
            if (Vector2.Distance(list1[i], list2[i]) > tolerance)
            {
                return false;
            }
        }
        return true;
    }

    protected override void PerformPathUpdate()
    {
        if (!IsGrounded())
        {
            return;
        }
        List<Vector2> newPath;
        Vector2 start = rb.position;
        Vector2 goal = StartPosition; 
        if (isAggressive)
        {
            goal = GetTargetPosition();
        }
        newPath = platformPathingAlgorithm.ShortestPath(start, goal, walkerProperties, currentPath);
        if (newPath != null)
        {
            currentPath = newPath;
            pathIndex = 2;
        }
    }

    public static List<Vector2> MergeListsWithOverlap(List<Vector2> list1, List<Vector2> list2, float tolerance = 0.01f)
    {
        int bestLen = 0;
        int bestI = -1;
        int bestJ = -1;

        for (int i = 0; i < list1.Count; i++)
        {
            for (int j = 0; j < list2.Count; j++)
            {
                int length = 0;
                while (i + length < list1.Count && j + length < list2.Count &&
                       AreApproximatelySame(list1[i + length], list2[j + length], tolerance))
                {
                    length++;
                }

                if (length > bestLen)
                {
                    bestLen = length;
                    bestI = i;
                    bestJ = j;
                }
            }
        }

        if (bestLen == 0)
        {
            // No overlap found
            var fallback = new List<Vector2>(list1);
            fallback.AddRange(list2);
            return fallback;
        }

        var result = new List<Vector2>();
        result.AddRange(list1.GetRange(0, bestI));                                  // Before overlap from list1
        result.AddRange(list1.GetRange(bestI, bestLen));                            // Overlap once
        result.AddRange(list2.GetRange(bestJ + bestLen, list2.Count - bestJ - bestLen)); // Remainder of list2
        return result;
    }

    private static bool AreApproximatelySame(Vector2 a, Vector2 b, float tolerance)
    {
        return Mathf.Abs(a.x - b.x) <= tolerance && Mathf.Abs(a.y - b.y) <= tolerance;
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
                movedDistance += Vector2.Distance(rb.position, previousPosition);
            }
            else
            {
                rb.MovePosition(currentPosition + direction * distanceThisFrame);
                var currentDistance = Vector2.Distance(rb.position, previousPosition);
                if (currentDistance < 0.0001f)
                {
                    enableMovement = false;
                    StartCoroutine(EnableMovementWhenGrounded());
                }
                movedDistance += currentDistance;
            }
            previousPosition = rb.position;
            if (movedDistance >= MoveDistanceWalkSound && IsGrounded() && moveSound != null)
            {
                walkingAudio.PlayOneShot(moveSound);
                movedDistance = 0f;
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

    protected IEnumerator EnableMovementWhenGrounded()
    {
        yield return new WaitUntil(IsGrounded);
        enableMovement = true;
    }

    protected override void TryRotateSprite() {}

    bool IsGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.05f, groundLayerMask);
        return hit.collider != null;
    }

}
