using BTAI;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
    private Vector2 direction;
    public float keepAliveTime = 20f;
    private float aliveTime = 0f;

    public void Initialize(Vector2 shootDirection)
    {
        direction = shootDirection.normalized;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        transform.position += (Vector3)(speed * Time.deltaTime * direction);
        aliveTime += Time.deltaTime;
        if (aliveTime >= keepAliveTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Level"))
        {
            Destroy(gameObject);
        }
        else if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<Player>(out var player))
            {
                player.TakeDamage(damage, direction);
            }
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Collision");
    }
}