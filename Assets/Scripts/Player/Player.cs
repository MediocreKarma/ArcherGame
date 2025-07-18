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
    private float timeSinceLastGrounded = 5f;

    public float jumpBufferingForgiveness = 0.075f;
    private float timeSinceLastJumpInput = 5f;

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
    public Collider2D TriggerCollider { get { return triggerCollider; } }
    private Collider2D groundCollider;

    public Interactable CurrentInteractable { get; set; }

    public float ElapsedTime { get; set; } = 0f;
    public bool playerFirstInput = false;

    [SerializeField] private GameObject head;
    [SerializeField] private GameObject body;

    private bool isHeadFacingRight = true;
    public float groundedCheckHeight = 5f;

    private bool isGroundedLastUpdate;

    private AudioSource walkAudio;
    private AudioSource effectSource;
    [SerializeField] private AudioClip dashEffect;
    [SerializeField] private AudioClip jumpEffect;
    [SerializeField] private AudioClip landEffect;
    [SerializeField] private AudioClip hurtEffect;

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
        groundedCheckSize = new Vector2(groundCollider.bounds.size.x, groundedCheckHeight);
        bottomCenter = new();
        var audios = GetComponents<AudioSource>();
        walkAudio = audios[0];
        effectSource = audios[1];
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
        if (timeSinceLastJumpInput < jumpBufferingForgiveness)
        {
            timeSinceLastJumpInput += Time.deltaTime;
        }
        if (walkAudio.isPlaying)
        {
            if (horizontalMovement == 0 || !isGrounded)
            {
                walkAudio.Stop();
            }
        }
        else
        {
            if (horizontalMovement != 0 && isGrounded)
            {
                walkAudio.Play();
            }
        }
        if (!isGroundedLastUpdate && isGrounded)
        {
            effectSource.PlayOneShot(landEffect);
        }
        isGroundedLastUpdate = isGrounded;
    }

    private void FixedUpdate()
    {
        if (IsDead) return;
        if (!enableMovement)
        {
            return;
        }
        rb.linearVelocityX = horizontalMovement * horizontalSpeed;
        if (timeSinceLastJumpInput < jumpBufferingForgiveness)
        {
            PerformJump();
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
        TryRotateHead(mousePosition);
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

    private Vector2 leftOrigin = new();
    private Vector2 rightOrigin = new();

    public void DropThroughPlatform(InputAction.CallbackContext context)
    {
        if (IsDead) return;
        if (context.performed && !isDropping && IsGrounded())
        {
            Bounds bounds = groundCollider.bounds;
            float rayLength = 0.2f;

            leftOrigin.x = bounds.min.x;
            leftOrigin.y = bounds.min.y;
            rightOrigin.x = bounds.max.x;
            rightOrigin.y = bounds.min.y;

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
            var dir = isFacingRight ? Vector2.right : Vector2.left;
            timeSinceLastDash = 0;
            rb.AddForce(dir * 40f, ForceMode2D.Impulse);
            effectSource.PlayOneShot(dashEffect);
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
        if ((isFacingRight && horizontalMovement < 0f) || (!isFacingRight && horizontalMovement > 0f))
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = body.transform.localScale;
            localScale.x *= -1f;
            body.transform.localScale = localScale;
            //bow.RotateSprite();
            //Look();
        }
    }

    private void TryRotateHead(Vector2 mousePosition)
    {
        if (IsDead) return;
        float dx = mousePosition.x - head.transform.position.x;
        if ((isHeadFacingRight && dx < 0) || (!isHeadFacingRight && dx > 0))
        {
            isHeadFacingRight = !isHeadFacingRight;
            Vector3 localScale = head.transform.localScale;
            localScale.x *= -1f;
            head.transform.localScale = localScale;
        }

    }

    private Vector2 bottomCenter;
    private Vector2 groundedCheckSize;

    private bool IsGrounded()
    {
        bottomCenter.x = groundCollider.bounds.center.x;
        bottomCenter.y = groundCollider.bounds.min.y;
        RaycastHit2D hit = Physics2D.BoxCast(
            bottomCenter,
            groundedCheckSize,
            0f, 
            Vector2.down, 
            0f, 
            groundLayer
        );
        return hit.collider != null;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (IsDead) return;
        timeSinceLastJumpInput = 0f;
    }

    public void PerformJump()
    {
        timeSinceLastJumpInput = jumpBufferingForgiveness;
        if (!playerFirstInput)
        {
            playerFirstInput = true;
        }
        if (timeSinceLastGrounded < groundedForgiveness)
        {
            timeSinceLastGrounded = groundedForgiveness;
            rb.linearVelocityY = jumpingPower;
        }
        effectSource.PlayOneShot(jumpEffect);
    }

    public void TakeDamage(float damage, Vector2 direction)
    {
        if (isInvulnerable) return;

        health -= damage;
        if (health <= 0)
        {
            Die();
        }
        effectSource.PlayOneShot(hurtEffect);
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
