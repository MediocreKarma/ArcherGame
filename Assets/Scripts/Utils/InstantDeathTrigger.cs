using UnityEngine;

public class InstantDeathTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(collision.tag + " " + collision.gameObject.name);
        if (collision.CompareTag("Player"))
        {
            Player player = FindFirstObjectByType<Player>();
            player.health = 0;
            player.Die();
        }
    }
}
