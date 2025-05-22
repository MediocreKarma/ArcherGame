using UnityEngine;

public class Spawner : MonoBehaviour
{
    public EnemyAI enemyPrefab;
    private EnemyAI current;


    private void Start()
    {
        current = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
    }

    void Update()
    {
        if (current == null)
        {
            Spawn();
        }
    }

    void Spawn()
    {
        current = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
    }
}
