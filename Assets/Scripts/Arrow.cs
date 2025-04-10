using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(ArrowSticking))]
public class Arrow : MonoBehaviour
{
    private bool isLaunched = false;
    private bool hasHit = false;
    private Rigidbody2D rb;
    private Vector3 originalPosition;
    private Transform bowParent;

    public float retrieveSpeed = 15f;
    private bool isReturning = false;
    private bool isJumping = false;
    private Collider2D stuckInCollider = null;
    private ArrowSticking sticking;
    private PathingAlgorithm pathing;

    private bool isInsideWall = false;

    public Vector3 OriginPosition()
    {
        return originalPosition;
    }

    public bool IsLaunched()
    {
        return isLaunched;
    }

    public void Launch(float force, ForceMode2D forceMode = ForceMode2D.Impulse)
    {
        if (isLaunched || isReturning)
        {
            return;
        }
        rb.simulated = true;
        transform.SetParent(null);
        isLaunched = true;
        isReturning = false;
        rb.AddForce(transform.right * force, forceMode);
    }
    public void Return()
    {
        if (isReturning || !isLaunched) 
        { 
            return;
        }
        isReturning = true;
        transform.SetParent(null);
        sticking.Unstick();
        StartCoroutine(ReturnSequence());
    }

    private IEnumerator ReturnSequence()
    {
        if (hasHit)
        {
            isInsideWall = !pathing.HasPath(transform.position, bowParent.position);
            hasHit = false;
            isJumping = true;
            Vector2 jumpDirection = ComputeJumpDirection(isInsideWall);
            rb.simulated = true;
            rb.linearVelocity = jumpDirection * retrieveSpeed;
            yield return new WaitForSeconds(0.1f); // Jumping from the wall takes 0.1 seconds
            hasHit = false;
            isJumping = false;
            isInsideWall = false;
        }

        Vector3 currentTargetPosition = bowParent.transform.TransformPoint(originalPosition);
        Vector2 returnDirection = (currentTargetPosition - transform.position).normalized;
        rb.linearVelocity = returnDirection * retrieveSpeed;

        while (isReturning)
        {
            currentTargetPosition = bowParent.transform.TransformPoint(originalPosition);
            returnDirection = (currentTargetPosition - transform.position).normalized;
            rb.linearVelocity = returnDirection * retrieveSpeed;
            yield return null;
        }
    }

    private Vector2 ComputeJumpDirection(bool isInsideWall)
    {
        if (!isInsideWall)
        {
            return -transform.right;
        }
        Debug.Log("Was inside wall!");

        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, 5f, LayerMask.GetMask("Level"));
        Collider2D closestCollider = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider2D collider in nearbyColliders)
        {
            float distance = Vector2.Distance(transform.position, collider.ClosestPoint(transform.position));
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCollider = collider;
            }
        }

        if (closestCollider != null)
        {
            Vector2 closestPoint = closestCollider.ClosestPoint(transform.position);
            return -((Vector2)transform.position - closestPoint).normalized;
        }
        return -transform.right;
    }

    public void RearmingBow()
    {
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        transform.SetParent(bowParent);
        transform.localPosition = originalPosition;
        transform.localEulerAngles = Vector3.zero;
        isReturning = false;
        isLaunched = false;
        rb.gravityScale = 1f;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.simulated = false;
        originalPosition = transform.localPosition;
        bowParent = transform.parent;
        sticking = GetComponent<ArrowSticking>();
        pathing = FindFirstObjectByType<PathingAlgorithm>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Arrow Hit " + collision.gameObject.name + " at " + Time.frameCount);
        if ((isJumping && collision.collider == stuckInCollider) || !isLaunched || hasHit)
        {
            return;
        }
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        hasHit = true;
        isReturning = false;
        //transform.SetParent(collision.transform);
        //rb.simulated = false;
        stuckInCollider = collision.collider;
        sticking.StickTo(collision.rigidbody, collision);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isReturning)
        {
            RearmingBow();
        }
    }

    void Update()
    {
        if (!isLaunched || hasHit)
        {
            return;
        }
        var velocity = rb.linearVelocity;
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
