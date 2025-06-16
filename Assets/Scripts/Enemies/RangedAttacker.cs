using UnityEngine;

public class RangedAttacker : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float shootCooldown = 2f;
    public float projectileSpeed = 10f;
    public float projectileDamage = 10f;
    public float detectionRange = 10f;

    private float lastShootTime = 0f;
    private Player player;
    private Transform playerTransform;
    private Collider2D playerCollider;

    private EnemyAI enemy;
    private AudioSource effectAudio;
    [SerializeField] private AudioClip shootSound;
    private LayerMask level;

    private void Start()
    {
        player = FindFirstObjectByType<Player>();
        playerTransform = player.transform;
        playerCollider = player.TriggerCollider;
        enemy = GetComponent<EnemyAI>();
        effectAudio = GetComponents<AudioSource>()[2];
        level = LayerMask.GetMask("Level", "Door");
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        if (distance <= detectionRange && Time.time >= lastShootTime + shootCooldown)
        {
            ShootAtPlayer();
            lastShootTime = Time.time;
        }
    }

    private Vector2? LineOfSight()
    {
        if (playerCollider == null) 
        { 
            playerCollider = player.TriggerCollider;
            return null;
        }
        var bounds = playerCollider.bounds;
        Vector2[] points = new Vector2[]
        {
            bounds.center,
            bounds.center - Vector3.up * bounds.extents.y,
            bounds.center + Vector3.up * bounds.extents.y,
        };
        foreach (var point in points)
        {
            RaycastHit2D hit = Physics2D.Linecast(shootPoint.position, point, level);
            Debug.DrawLine(shootPoint.position, point, Color.red, 1f);
            if (hit.collider == null)
            {
                return point;
            }
        }

        return null;
    }

    private void ShootAtPlayer()
    {
        if (enemy != null && !enemy.isAlive)
        {
            return;
        }
        Vector2? attackPoint = LineOfSight();
        if (!attackPoint.HasValue)
        {
            return;
        }
        Vector2 direction = (attackPoint.Value - (Vector2)shootPoint.position).normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        GameObject projectileObj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.Euler(0, 0, angle));
        projectileObj.layer = LayerMask.NameToLayer("Bullet");

        Projectile projectile = projectileObj.GetComponent<Projectile>();
        projectile.speed = projectileSpeed;
        projectile.damage = projectileDamage;
        if (effectAudio != null && shootSound != null)
        {
            effectAudio.PlayOneShot(shootSound);
        }
        projectile.Initialize(direction);
    }

}