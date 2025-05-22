using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(ArrowSticking))]
public class Arrow : MonoBehaviour
{
    private bool isLaunched = false;
    private bool hasHit = false;
    private Rigidbody2D rb;
    private Vector3 originalPosition;
    private Transform bowParent;
    private Collider2D arrowCollider;

    public float retrieveSpeed = 15f;
    private bool isReturning = false;
    private bool isJumping = false;
    private ArrowSticking sticking;
    private PathingAlgorithm pathing;

    private bool isInsideWall = false;
    private Coroutine returnCoroutine;

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
        if (!isLaunched) 
        { 
            return;
        }
        isReturning = true;
        //transform.SetParent(null);

        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
        
        returnCoroutine = StartCoroutine(ReturnSequence());
    }

    public float jumpTime = 0.5f;

    private IEnumerator ReturnSequence()
    {
        GameObject stuckTo = sticking.StuckTo();
        sticking.Unstick();
        if (hasHit)
        {
            isInsideWall = !pathing.HasPath(transform.position, bowParent.position);
            hasHit = false;
            isJumping = true;
            Vector2 jumpDirection = ComputeJumpDirection(
                isInsideWall &&
                (stuckTo == null || stuckTo != null && stuckTo.layer == LayerMask.NameToLayer("Level"))
            );
            rb.simulated = true;
            rb.linearVelocity = 3f * retrieveSpeed * jumpDirection;
            yield return new WaitForSeconds(jumpTime);
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
            return -transform.TransformDirection(Vector3.right);
        }

        Collider2D[] levelColliders = Physics2D.OverlapCircleAll(transform.position, 1f, LayerMask.GetMask("Level"));
        Collider2D closestCollider = levelColliders.Length > 0 ? levelColliders[0] : null;

        if (closestCollider != null)
        {
            Vector2 closestPoint = closestCollider.ClosestPoint(transform.position);
            Debug.Log(-((Vector2)transform.position - closestPoint).normalized);
            StartCoroutine(DisableCollider(closestCollider, jumpTime));
            return -((Vector2)transform.position - closestPoint).normalized;
        }
        return -transform.TransformDirection(Vector3.right);
    }

    private IEnumerator DisableCollider(Collider2D collider, float seconds)
    {
        Physics2D.IgnoreCollision(collider, arrowCollider, true);
        yield return new WaitForSeconds(seconds);
        Physics2D.IgnoreCollision(collider, arrowCollider, false);
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
        rb.transform.localScale = new Vector3(1, 1.5f, 1);
        rb.gravityScale = 1f;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.simulated = false;
        originalPosition = transform.localPosition;
        bowParent = transform.parent;
        sticking = GetComponent<ArrowSticking>();
        pathing = FindFirstObjectByType<AStar>();
        arrowCollider = GetComponentInChildren<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Debug.Log("Arrow Hit " + collision.gameObject.name + " at " + Time.frameCount);
        if ((isJumping && collision.gameObject == sticking.StuckTo()) || !isLaunched || hasHit)
        {
            return;
        }
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        hasHit = true;
        isReturning = false;
        //transform.SetParent(collision.transform);
        //rb.simulated = false;
        //sticking.StickTo(collision.rigidbody, collision);
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
