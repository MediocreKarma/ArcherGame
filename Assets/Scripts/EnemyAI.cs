using System.Collections;
using Unity.VisualScripting.FullSerializer.Internal;
using UnityEngine;
using UnityEngine.Rendering.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    protected Transform playerTransform;
    protected Player player;
    protected Rigidbody2D rb;

    public float attackDamage = 1f;
    public float speed = 5f;
    public int hitpoints = 1;
    public bool isAlive = true;
    public bool isAggressive = false;
    protected bool hasArrowStuck = false;
    protected bool enableMovement = true;
    protected bool hasImmunity = true;
    protected bool isFacingRight = true;

    public int StartHitpoints { get; private set; }
    public Vector2 StartPosition { get; private set; }

    private ArrowSticking arrowSticking;
    private float timeSinceLastHit = 0f;

    public PathingAlgorithm pathingAlgorithm;
    protected string id;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        id = System.Guid.NewGuid().ToString();
        playerTransform = GameObject.Find("Player").transform;
        player = playerTransform.GetComponent<Player>();
        rb = GetComponent<Rigidbody2D>();
        arrowSticking = FindFirstObjectByType<ArrowSticking>();
        StartHitpoints = hitpoints;
        StartPosition = transform.position;
    }

    protected virtual void Update()
    {
        timeSinceLastHit += Time.deltaTime;
        TryRotateSprite();
    }

    private void TryRotateSprite()
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
            Debug.Log("Arrow hit enemy at " + Time.frameCount);
            ArrowSticking arrow = collision.gameObject.GetComponent<ArrowSticking>();
            if (arrow.StuckTo() == gameObject)
            {
                Debug.Log("Arrow already stuck to enemy at " + Time.frameCount);
                return;
            }
            arrow.StickTo(GetComponent<Rigidbody2D>(), collision);
            hasArrowStuck = true;
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint2D contact = collision.GetContact(i);
                if (contact.otherCollider.CompareTag("Shield"))
                {
                    Debug.Log("Arrow hit shield " + Time.frameCount);
                    return;
                }
            }
            Debug.Log("Arrow dealt damage " + Time.frameCount);
            hitpoints--;
            if (hitpoints <= 0)
            {
                Die(collision);
            }
            else
            {
                Stagger(collision);
            }
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
        hitpoints = 0;
        isAlive = false;
        rb.freezeRotation = false;
        rb.AddForce(collision.transform.right * 2f, ForceMode2D.Impulse);
        rb.AddTorque(2f, ForceMode2D.Impulse);
        Physics2D.IgnoreCollision(GetComponentInChildren<Collider2D>(), player.GetComponent<Collider2D>(), true);
        StartCoroutine(DestroySelf(15f));
    }

    private void Stagger(Collision2D collision)
    {
        rb.AddForce(collision.transform.right * 0.8f, ForceMode2D.Impulse);
        enableMovement = false;
        StartCoroutine(EnableMovement(0.5f));
    }

    private IEnumerator EnableMovement(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        enableMovement = true;
    }

    private IEnumerator DestroySelf(float seconds, float keepAliveSeconds = 3)
    {
        yield return new WaitForSeconds(seconds);
        while (timeSinceLastHit < keepAliveSeconds || arrowSticking.StuckTo() == gameObject)
        {
            if (timeSinceLastHit >= keepAliveSeconds)
            {
                timeSinceLastHit = 0f;
            }
            yield return new WaitForSeconds(keepAliveSeconds - timeSinceLastHit);
        }
        gameObject.SetActive(false);
    }

    protected Vector2 GetTargetPosition()
    {
        return playerTransform.position;
    }
}
