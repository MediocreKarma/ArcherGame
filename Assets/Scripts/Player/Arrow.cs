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
    public ArrowSticking Sticking { get; private set; }
    private PathingAlgorithm pathing;

    private bool isInsideWall = false;
    private Coroutine returnCoroutine;

    private AudioSource effectAudio;
    private AudioSource magicalHumAudio;

    public float AttackDamage { get; private set; } = 1f;

    [SerializeField] private AudioClip hitEffect;

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
        magicalHumAudio.Play();
        returnCoroutine = StartCoroutine(ReturnSequence());
    }

    public float jumpTime = 0.5f;
    private WaitForSeconds arrowJumpWait;
    private static readonly WaitForSeconds arrowReturningWait = new(0.067f);

    private IEnumerator ReturnSequence()
    {
        GameObject stuckTo = Sticking.StuckTo();
        Sticking.Unstick();
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
            yield return arrowJumpWait;
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
            yield return arrowReturningWait;
        }
    }

    private Vector2 ComputeJumpDirection(bool isInsideWall)
    {
        if (!isInsideWall)
        {
            return -transform.TransformDirection(Vector3.right);
        }
        Collider2D closestCollider = Physics2D.OverlapCircle(transform.position, 1f, LayerMask.GetMask("Level"));
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

    private static readonly Vector3 defaultRearmedBowRbTransformLocalScale = new(1, 1.5f, 1);

    public void RearmingBow()
    {
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        transform.SetParent(bowParent);
        transform.localPosition = originalPosition;
        transform.localEulerAngles = Vector3.zero;
        isReturning = false;
        isLaunched = false;
        isJumping = false;
        hasHit = false;
        transform.localScale = defaultRearmedBowRbTransformLocalScale;
        rb.gravityScale = 1f;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.simulated = false;
        originalPosition = transform.localPosition;
        bowParent = transform.parent;
        Sticking = GetComponent<ArrowSticking>();
        pathing = FindFirstObjectByType<PrecomputedShortestPathAlgorithm>();
        arrowCollider = GetComponentInChildren<Collider2D>();
        var audios = GetComponents<AudioSource>();
        effectAudio = audios[0];
        magicalHumAudio = audios[1];
        arrowJumpWait = new WaitForSeconds(jumpTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Debug.Log("Arrow Hit " + collision.gameObject.name + " at " + Time.frameCount);
        if ((isJumping && collision.gameObject == Sticking.StuckTo()) || !isLaunched || hasHit)
        {
            return;
        }
        if (magicalHumAudio.isPlaying)
        {
            magicalHumAudio.Stop();
        }
        effectAudio.PlayOneShot(hitEffect);
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
            if (magicalHumAudio.isPlaying)
            {
                magicalHumAudio.Stop();
            }
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
