using UnityEngine;

public class OutOfBounds : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Player has fallen out of bounds!");
            Player player = collision.GetComponent<Player>();
            player.Die();
        }
    }
}
