using UnityEngine;

public class RangedAttacker : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float shootCooldown = 2f;
    public float projectileSpeed = 10f;
    public float projectileDamage = 10f;
    public float detectionRange = 10f;

    private float lastShootTime;
    private Transform player;

    private EnemyAI enemy;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        enemy = GetComponent<EnemyAI>();
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= detectionRange && Time.time >= lastShootTime + shootCooldown)
        {
            ShootAtPlayer();
            lastShootTime = Time.time;
        }
    }

    private void ShootAtPlayer()
    {
        if (enemy != null && !enemy.isAlive)
        {
            return;
        }
        Vector2 direction = (player.position - shootPoint.position).normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        GameObject projectileObj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.Euler(0, 0, angle));
        projectileObj.layer = LayerMask.NameToLayer("Bullet");

        Projectile projectile = projectileObj.GetComponent<Projectile>();
        projectile.speed = projectileSpeed;
        projectile.damage = projectileDamage;
        projectile.Initialize(direction);
    }

    
}