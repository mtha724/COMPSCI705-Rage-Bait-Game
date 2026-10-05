using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Reads input each frame and applies movement during physics updates; feedback is exposed through events.
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(PlayerInput))]
public class PlayerMove : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 9f;
    public LayerMask groundLayer = 1 << 6;
    public float gravityScale = 3f;
    public float fallGravityMultiplier = 1.4f;
    public float maxFallSpeed = 25f;
    public bool ControlsReversed { get; private set; }
    public bool Grounded { get; private set; }
    public bool MovementEnabled { get; private set; } = true;
    public event Action<Vector3> Jumped;
    public event Action<Vector3> Landed;
    public event Action<Vector3> Stepped;
    private Rigidbody2D body;
    private BoxCollider2D box;
    private Animator anim;
    private InputAction moveAction;
    private InputAction jumpAction;
    private PlayerInput playerInput;
    private float horizontal;
    private float jumpBufferedUntil = -1f;
    private float nextStep;
    private Vector3 originalScale;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        originalScale = transform.localScale;
        // Prevent collision forces from tipping the character; interpolation smooths rendered motion.
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Start()
    {
        // PlayerInput creates/enables its private action copy during OnEnable.
        moveAction = playerInput.actions.FindAction("Move", true);
        jumpAction = playerInput.actions.FindAction("Jump", true);
    }

    private void Update()
    {
        if (moveAction == null || jumpAction == null) return;
        horizontal = MovementEnabled ? moveAction.ReadValue<Vector2>().x : 0f;
        if (ControlsReversed) horizontal *= -1f;
        // Buffer a jump briefly so input just before landing can be consumed by the next physics update.
        if (MovementEnabled && jumpAction.WasPressedThisFrame()) jumpBufferedUntil = Time.time + .12f;
        if (Mathf.Abs(horizontal) > .01f)
            transform.localScale = new Vector3(Mathf.Abs(originalScale.x) * Mathf.Sign(horizontal), originalScale.y, originalScale.z);
        if (anim != null)
        {
            anim.SetBool("run", MovementEnabled && Mathf.Abs(horizontal) > .01f);
            anim.SetBool("grounded", Grounded);
        }
    }

    public bool CheckGrounded()
    {
        Bounds bounds = box.bounds;
        // A narrow probe below the feet avoids treating side-wall contact as standing on the ground.
        Vector2 centre = new Vector2(bounds.center.x, bounds.min.y - .025f);
        // Reject grounding while rising, even if the feet still overlap the floor after take-off.
        return body.linearVelocity.y <= .1f && Physics2D.OverlapBox(centre,
            new Vector2(bounds.size.x * .8f, .07f), 0f, groundLayer) != null;
    }

    private void FixedUpdate()
    {
        if (!MovementEnabled) return;
        bool wasGrounded = Grounded;
        Grounded = CheckGrounded();
        if (Grounded && !wasGrounded) Landed?.Invoke(transform.position);
        // Stronger gravity on descent reduces floatiness; preserve vertical velocity when steering sideways.
        body.gravityScale = gravityScale * (body.linearVelocity.y < 0f ? fallGravityMultiplier : 1f);
        body.linearVelocity = new Vector2(horizontal * speed, Mathf.Max(body.linearVelocity.y, -maxFallSpeed));
        // Consume the buffered jump once and immediately clear grounding to prevent repeated launches.
        if (Grounded && jumpBufferedUntil >= Time.time)
        {
            jumpBufferedUntil = -1f;
            Grounded = false;
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
            if (anim != null) anim.SetTrigger("jump");
            Jumped?.Invoke(transform.position);
        }
        // Emit footsteps only during grounded movement, with an interval independent of rendering FPS.
        if (Grounded && Mathf.Abs(horizontal) > .01f && Time.time >= nextStep)
        {
            nextStep = Time.time + .24f;
            Stepped?.Invoke(transform.position - Vector3.up * .3f);
        }
    }

    // Reversal remains active until a restore zone or a fresh scene player resets it.
    public void ReverseControls() => ControlsReversed = true;
    // A later zone can restore normal controls without replacing the player.
    public void SetControlsReversed(bool reversed) => ControlsReversed = reversed;
    // Freeze input and physics during death, goal feedback and menus. Reloading supplies a fresh enabled player.
    public void StopMovement()
    {
        MovementEnabled = false;
        horizontal = 0f;
        jumpBufferedUntil = -1f;
        body.linearVelocity = Vector2.zero;
        body.simulated = false;
    }

    // Editor-only illustration of the ground probe; it does not affect collision detection.
    private void OnDrawGizmosSelected()
    {
        var collider = GetComponent<BoxCollider2D>();
        if (collider == null) return;
        Bounds bounds = collider.bounds;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(bounds.center.x, bounds.min.y - .025f, 0f), new Vector3(bounds.size.x * .8f, .07f, 0f));
    }
}
