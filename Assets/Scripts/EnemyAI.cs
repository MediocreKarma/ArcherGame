using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    protected Transform player;
    protected Player playerObject;
    protected Rigidbody2D rb;

    public float attackDamage = 1f;
    public float speed = 5f;
    public int hitpoints = 1;
    public bool isAlive = true;
    protected bool hasArrowStuck = false;
    protected bool enableMovement = true;
    protected bool hasImmunity = true;

    private ArrowSticking arrowSticking;
    private float timeSinceLastHit = 0f;

    public PathingAlgorithm pathingAlgorithm;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {
        player = GameObject.Find("Player").transform;
        playerObject = player.GetComponent<Player>();
        rb = GetComponent<Rigidbody2D>();
        arrowSticking = FindFirstObjectByType<ArrowSticking>();
    }

    protected virtual void Update()
    {
        timeSinceLastHit += Time.deltaTime;
    }

    protected void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Arrow"))
        {
            Debug.Log("EnemyAI Hit by arrow");

            ArrowSticking arrow = collision.gameObject.GetComponent<ArrowSticking>();
            arrow.StickTo(GetComponent<Rigidbody2D>(), collision);
            hasArrowStuck = true;
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
        if (collision.gameObject.CompareTag("Player"))
        {
            bool hitFromLeft = transform.position.x < collision.transform.position.x;
            Vector2 hitDirection = hitFromLeft ? Vector2.right : Vector2.left;
            playerObject.TakeDamage(attackDamage, hitDirection);
        }
    }

    private void Die(Collision2D collision)
    {
        hitpoints = 0;
        isAlive = false;
        rb.freezeRotation = false;
        rb.AddForce(collision.transform.right * 2f, ForceMode2D.Impulse);
        rb.AddTorque(2f, ForceMode2D.Impulse);
        StartCoroutine(DestroySelf(15f));
    }

    private void Stagger(Collision2D collision)
    {
        rb.AddForce(collision.transform.right * 0.8f, ForceMode2D.Impulse);
        enableMovement = false;
        StartCoroutine(EnableMovement(0.5f));
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        Debug.Log("EnemyAI exited collision with " + collision.gameObject.name);
    }

    private IEnumerator EnableMovement(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        enableMovement = true;
    }

    private IEnumerator DestroySelf(float seconds, float keepAliveSeconds = 3)
    {
        yield return new WaitForSeconds(seconds);
        while (timeSinceLastHit < keepAliveSeconds)
        {
            yield return new WaitForSeconds(keepAliveSeconds - timeSinceLastHit);
        }
        if (arrowSticking.IsStuckTo() == gameObject)
        {
            arrowSticking.Unstick();
        }
        Destroy(gameObject);
    }

    protected Vector2 GetTargetPosition()
    {
        return player.position;
    }
}
