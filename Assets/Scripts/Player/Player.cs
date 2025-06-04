using Gamekit2D;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.UI.Image;

[System.Serializable]
public class Player : MonoBehaviour
{
    public float health = 100f;
    public float MaxHealth { get; private set; }
    private float horizontalMovement;
    public float horizontalSpeed = 20f;

    public float jumpingPower = 20f;

    public float dashCooldown = 2f;
    private float timeSinceLastDash = 2f;
    public float TimeSinceLastDash => timeSinceLastDash;
    private bool canDash = true;
    public bool CanDash => canDash;

    public float groundedForgiveness = 0.075f;
    private float timeSinceLastGrounded = 0.075f;

    private bool isFacingRight = true;

    private bool isDropping = false;
    private bool enableMovement = true;

    private bool isInvulnerable = false;
    private readonly float knockbackForce = 20f;

    public float slowMoSeconds = 1.7f;
    public float damageIFramesSeconds = 2f;

    public bool IsDead { get; set; } = false;

    private LayerMask platformLayer;
    [SerializeField] private LayerMask groundLayer = -1;
    [SerializeField] private Bow bow;

    private Rigidbody2D rb;
    private Collider2D triggerCollider;
    private Collider2D groundCollider;

    public Interactable CurrentInteractable { get; set; }

    public float ElapsedTime { get; set; } = 0f;
    public bool playerFirstInput = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        triggerCollider = GetComponent<CompositeCollider2D>();
        groundCollider = transform.GetChild(0).GetComponent<Collider2D>();
        if (groundLayer == -1)
        {
            groundLayer = LayerMask.GetMask("Ground");
        }
        platformLayer = LayerMask.GetMask("Platform");
        MaxHealth = health;
    }

    private void Update()
    {
        bool isGrounded = IsGrounded();
        if (playerFirstInput)
        {
            ElapsedTime += Time.deltaTime;
        }
        TryRotateSprite();
        if (!canDash && isGrounded)
        {
            canDash = true;
        }
        if (timeSinceLastDash < dashCooldown)
        {
            timeSinceLastDash += Time.deltaTime;
        }
        if (isGrounded)
        {
            timeSinceLastGrounded = 0;
        }
        else if (timeSinceLastGrounded < groundedForgiveness)
        {
            timeSinceLastGrounded += Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (IsDead) return;
        if (enableMovement)
        {
            rb.linearVelocityX = horizontalMovement * horizontalSpeed;
        }
    }

    public bool IsFacingRight()
    {
        return isFacingRight;
    }

    public void HorizontalMovement(InputAction.CallbackContext context)
    {
        if (IsDead) return;
        horizontalMovement = context.ReadValue<Vector2>()[0];
        if (horizontalMovement != 0)
        {
            playerFirstInput = true;
        }
    }

    private void Look()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        bow.AimTowards(mousePosition);
    }

    public void LookCallback(InputAction.CallbackContext _)
    {
        if (IsDead) return;
        Look();
    }

    public void ChargeBow(InputAction.CallbackContext context)
    {
        if (IsDead) return;
        if (context.performed)
        {
            if (!playerFirstInput)
            {
                playerFirstInput = true;
            }
            bow.StartCharging();
        }
        else if (context.canceled)
        {
            bow.ReleaseCharge();
        }
    }

    public void RetrieveArrow(InputAction.CallbackContext context)
    {
        if (IsDead) return;
        if (context.performed)
        {
            bow.RetrieveArrow();
        }
    }

    public void DropThroughPlatform(InputAction.CallbackContext context)
    {
        if (IsDead) return;
        if (context.performed && !isDropping && IsGrounded())
        {
            Bounds bounds = GetComponent<Collider2D>().bounds;
            float rayLength = 0.2f;

            Vector2 leftOrigin = new(bounds.min.x, bounds.min.y);
            Vector2 rightOrigin = new(bounds.max.x, bounds.min.y);

            RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, Vector2.down, rayLength, platformLayer);
            RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, Vector2.down, rayLength, platformLayer);

            if (leftHit.collider != null &&
                rightHit.collider != null &&
                leftHit.collider == rightHit.collider)
            {
                StartCoroutine(DisablePlatformCollider(leftHit.collider, 0.3f));
            }
        }
    }

    private IEnumerator DisablePlatformCollider(Collider2D collider, float seconds)
    {
        isDropping = true;
        collider.enabled = false;

        rb.AddForce(Vector2.down * 1f, ForceMode2D.Impulse);
        yield return new WaitForSeconds(seconds);
        collider.enabled = true;
        isDropping = false;
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (IsDead) return;
        if (!context.performed) return;
        if (!playerFirstInput)
        {
            playerFirstInput = true;
        }
        if (timeSinceLastDash >= dashCooldown && canDash)
        {
            canDash = false;
            enableMovement = false;
            var dir = isFacingRight ? Vector2.left : Vector2.right;
            timeSinceLastDash = 0;
            rb.AddForce(dir * 40f, ForceMode2D.Impulse);
            StartCoroutine(DashIFrames(0.1f));
        }
    }

    private IEnumerator DashIFrames(float seconds)
    {
        isInvulnerable = true;
        triggerCollider.excludeLayers = LayerMask.GetMask("Everything");
        yield return new WaitForSeconds(seconds);
        triggerCollider.excludeLayers = LayerMask.GetMask("Player");
        isInvulnerable = false;
        enableMovement = true;
    }

    private void TryRotateSprite()
    {
        if (IsDead) return;
        if ((isFacingRight && horizontalMovement > 0f) || (!isFacingRight && horizontalMovement < 0f))
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
            bow.RotateSprite();
            Look();
        }
    }

    private bool IsGrounded()
    {
        RaycastHit2D hit = Physics2D.BoxCast(
            new Vector2(groundCollider.bounds.center.x, groundCollider.bounds.min.y), 
            groundCollider.bounds.size, 
            0f, 
            Vector2.down, 
            0.05f, 
            groundLayer
        );
        return hit.collider != null;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (IsDead) return;
        if (!context.performed)
        {
            return;
        }
        if (!playerFirstInput)
        {
            playerFirstInput = true;
        }
        if (timeSinceLastGrounded < groundedForgiveness)
        {
            timeSinceLastGrounded = groundedForgiveness;
            rb.linearVelocityY = jumpingPower;
        }
    }

    public void TakeDamage(float damage, Vector2 direction)
    {
        if (isInvulnerable) return;

        health -= damage;
        if (health <= 0)
        {
            Die();
        }
        //Debug.Log($"Player took {damage} damage! Health: {health}");
        //Debug.Log("Direction " + direction);
        Vector2 knockbackDirection = (direction.normalized + Vector2.up * 0.2f).normalized;

        StartCoroutine(SlowMoPushBack(knockbackDirection.normalized * knockbackForce, slowMoSeconds));
        StartCoroutine(DamageIFrames(damageIFramesSeconds));
    }

    public void Die()
    {
        StartCoroutine(DeathCoroutine(5f));
    }

    private IEnumerator DeathCoroutine(float seconds)
    {
        rb.freezeRotation = false;
        enableMovement = false;
        IsDead = true;
        Physics2D.IgnoreCollision(GetComponentInChildren<Collider2D>(), GetComponent<Collider2D>(), true);
        yield return new WaitForSeconds(seconds);
    }

    private IEnumerator DamageIFrames(float seconds)
    {
        isInvulnerable = true; 
        triggerCollider.excludeLayers = LayerMask.GetMask("Everything");
        yield return new WaitForSecondsRealtime(seconds);
        triggerCollider.excludeLayers = LayerMask.GetMask("Player");
        isInvulnerable = false;
    }

    private IEnumerator SlowMoPushBack(Vector2 force, float seconds)
    {
        enableMovement = false;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(force, ForceMode2D.Impulse);
        Time.timeScale = 0.2f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(seconds);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        enableMovement = true;
    }

    public bool IsInvulnerable()
    {
        return isInvulnerable;
    }

    public void Interact(InputAction.CallbackContext context)
    {
        if (IsDead) return;
        if (context.performed && CurrentInteractable)
        {
            CurrentInteractable.Trigger(this);
        }
    }

    public float restartHoldTime = 2f;
    private Coroutine restartCoroutine;
    public void RestartAction(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            // Start measuring hold
            restartCoroutine = StartCoroutine(RestartCoroutine());
        }
        else if (context.canceled)
        {
            // Cancel hold
            if (restartCoroutine != null)
            {
                StopCoroutine(restartCoroutine);
                restartCoroutine = null;
            }
        }
    }

    private IEnumerator RestartCoroutine()
    {
        float timer = 0f;

        while (timer < restartHoldTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        Restart();
    }

    private void Restart()
    {
        health = MaxHealth;
        rb.freezeRotation = true;
        enableMovement = true;
        rb.linearVelocity = Vector2.zero;
        horizontalMovement = 0;
        IsDead = false;
        FindFirstObjectByType<SaveManager>().LoadGame();
    }

    public void Quit(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    //private bool SlopeCheck()
    //{
    //}

    //private void SlopeCheckHorizontal(Vector2 origin)
    //{

    //}

    //private void SlopeCheckVertical(Vector2 origin)
    //{
    //}
}
