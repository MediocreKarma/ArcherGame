using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Unity.VisualScripting.FullSerializer.Internal;
using UnityEngine;
using UnityEngine.Rendering.UI;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyAI : MonoBehaviour
{
    protected Transform playerTransform;
    protected Player player;
    protected Rigidbody2D rb;

    public float attackDamage = 1f;
    public float speed = 5f;
    public float hitpoints = 1f;
    public bool isAlive = true;
    public bool isAggressive = false;
    protected bool hasArrowStuck = false;
    protected bool enableMovement = true;
    protected bool hasImmunity = true;
    protected bool isFacingRight = true;

    public float aggroDistance = 10f;
    protected int pathIndex = 0;
    protected float pathTimer = 0f;
    protected const float pathInterval = 0.333f;
    public List<Vector2> currentPath = new();

    public event System.Action OnDeath;

    public float StartHitpoints { get; private set; }
    public Vector3 StartPosition { get; private set; }

    private ArrowSticking arrowSticking;
    private float timeSinceLastHit = 0f;

    public PathingAlgorithm pathingAlgorithm;
    protected string id;

    public bool IsTarget = false;
    private AudioSource hitAudio;
    [SerializeField] private AudioClip hitSound;

    private float playerHeightOffset;

    protected virtual void Start()
    {
        id = System.Guid.NewGuid().ToString();
        playerTransform = GameObject.Find("Player").transform;
        player = playerTransform.GetComponent<Player>();
        playerHeightOffset = playerTransform.GetComponent<Collider2D>().bounds.min.y - playerTransform.position.y;
        rb = GetComponent<Rigidbody2D>();
        arrowSticking = FindFirstObjectByType<ArrowSticking>();
        StartHitpoints = hitpoints;
        StartPosition = transform.position;
        var sources = GetComponents<AudioSource>();
        if (sources.Length > 0)
        {
            hitAudio = GetComponents<AudioSource>()?[0];
        }
    }

    public void Reset()
    {
        isAggressive = false;
        transform.SetPositionAndRotation(StartPosition, Quaternion.identity);
        isAlive = true;
        hitpoints = StartHitpoints;
        hasArrowStuck = false;
        enableMovement = true;
        pathIndex = 0;
        currentPath.Clear();
        rb.freezeRotation = true;
        rb.angularVelocity = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    protected virtual void Update()
    {
        timeSinceLastHit += Time.deltaTime;
        TryRotateSprite();
        if (!isAlive)
        {
            rb.gravityScale = 4f;
            return;
        }
        if (!enableMovement)
        {
            return;
        }
        UpdatePath();
        UpdateAggro();
    }

    protected abstract void PerformPathUpdate();

    protected virtual void UpdatePath()
    {
        pathTimer -= Time.deltaTime;
        if (pathTimer > 0f)
        {
            return;
        }
        pathTimer = pathInterval;
        PerformPathUpdate();
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
            float distanceToPlayer = Vector2.Distance(rb.position, playerTransform.position);
            if (distanceToPlayer > aggroDistance * 3 || player.IsDead)
            {
                isAggressive = false;
            }
        }
    }

    protected virtual void TryRotateSprite()
    {
        if (!isAlive) return;
        float horizontalMovement = rb.linearVelocityX;
        if ((isFacingRight && horizontalMovement < 0f) || (!isFacingRight && horizontalMovement > 0f))
        {
            RotateSprite();
        }
    }

    protected void RotateSprite()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1f;
        transform.localScale = localScale;
    }

    protected void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Arrow"))
        {
            Arrow arrow = collision.gameObject.GetComponent<Arrow>();
            ArrowSticking sticking = arrow.Sticking;
            if (sticking.StuckTo() == gameObject)
            {
                return;
            }
            sticking.StickTo(rb, collision);
            hasArrowStuck = true;
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint2D contact = collision.GetContact(i);
                if (contact.otherCollider.CompareTag("Shield"))
                {
                    return;
                }
            }
            TakeDamage(arrow.AttackDamage, collision);
        }
    }

    protected void TakeDamage(float damage, Collision2D collision)
    {
        hitpoints -= Mathf.RoundToInt(damage);
        const float epsilon = 0.01f;
        if (hitAudio != null && hitSound != null)
        {
            hitAudio.PlayOneShot(hitSound);
        }
        if (hitpoints <= epsilon)
        {
            Die(collision);
        }
        else
        {
            isAggressive = true;
            Stagger(collision);
        }
    }

    protected void OnTriggerEnter2D(Collider2D collision)
    {
        if (isAlive && collision.gameObject.CompareTag("Player"))
        {
            bool hitFromLeft = transform.position.x < collision.transform.position.x;
            Vector2 hitDirection = hitFromLeft ? Vector2.right : Vector2.left;
            player.TakeDamage(attackDamage, hitDirection);
            if (player.IsDead)
            {
                isAggressive = false;
            }
        }
    }

    private void Die(Collision2D collision)
    {
        if (!isAlive) return;
        hitpoints = 0;
        OnDeath?.Invoke();
        isAlive = false;
        rb.freezeRotation = IsTarget;
        rb.AddForce(collision.transform.right * 2f, ForceMode2D.Impulse);
        rb.AddTorque(2f, ForceMode2D.Impulse);
        Physics2D.IgnoreCollision(GetComponentInChildren<Collider2D>(), player.GetComponent<Collider2D>(), true);
        if (!IsTarget)
        {
            StartCoroutine(DestroySelf(15f));
        }
    }

    private void Stagger(Collision2D collision)
    {
        if (IsTarget)
        {
            return; 
        }
        rb.AddForce(collision.transform.right * 0.8f, ForceMode2D.Impulse);
        enableMovement = false;
        StartCoroutine(EnableMovement(0.5f));
    }

    protected IEnumerator EnableMovement(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        enableMovement = true;
    }

    private IEnumerator DestroySelf(float seconds, float keepAliveSeconds = 3)
    {
        yield return new WaitForSeconds(seconds);
        while (timeSinceLastHit < keepAliveSeconds || arrowSticking.StuckTo() == gameObject)
        {
            if (isAlive)
                yield break;
            if (timeSinceLastHit >= keepAliveSeconds)
            {
                timeSinceLastHit = 0f;
            }
            yield return new WaitForSeconds(keepAliveSeconds - timeSinceLastHit);
        }
        if (!isAlive)
        {
            gameObject.SetActive(false);
        }
    }

    protected virtual Vector2 GetTargetPosition()
    {
        return new Vector2(playerTransform.position.x, playerTransform.position.y + playerHeightOffset);
    }
}
