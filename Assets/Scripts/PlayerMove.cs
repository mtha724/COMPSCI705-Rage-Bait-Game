using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private float horizontal;
    private float jumpBufferedUntil = -1f;
    private float nextStep;
    private Vector3 originalScale;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        var input = GetComponent<PlayerInput>();
        moveAction = input.actions.FindAction("Move", true);
        jumpAction = input.actions.FindAction("Jump", true);
        originalScale = transform.localScale;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Update()
    {
        horizontal = MovementEnabled ? moveAction.ReadValue<Vector2>().x : 0f;
        if (ControlsReversed) horizontal *= -1f;
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
        Vector2 centre = new Vector2(bounds.center.x, bounds.min.y - .025f);
        return body.linearVelocity.y <= .1f && Physics2D.OverlapBox(centre,
            new Vector2(bounds.size.x * .8f, .07f), 0f, groundLayer) != null;
    }

    private void FixedUpdate()
    {
        bool wasGrounded = Grounded;
        Grounded = CheckGrounded();
        if (Grounded && !wasGrounded) Landed?.Invoke(transform.position);
        if (!MovementEnabled) return;
        body.gravityScale = gravityScale * (body.linearVelocity.y < 0f ? fallGravityMultiplier : 1f);
        body.linearVelocity = new Vector2(horizontal * speed, Mathf.Max(body.linearVelocity.y, -maxFallSpeed));
        if (Grounded && jumpBufferedUntil >= Time.time)
        {
            jumpBufferedUntil = -1f;
            Grounded = false;
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
            if (anim != null) anim.SetTrigger("jump");
            Jumped?.Invoke(transform.position);
        }
        if (Grounded && Mathf.Abs(horizontal) > .01f && Time.time >= nextStep)
        {
            nextStep = Time.time + .24f;
            Stepped?.Invoke(transform.position - Vector3.up * .3f);
        }
    }

    public void ReverseControls() => ControlsReversed = true;
    public void StopMovement()
    {
        MovementEnabled = false;
        horizontal = 0f;
        jumpBufferedUntil = -1f;
        body.linearVelocity = Vector2.zero;
        body.simulated = false;
    }

    private void OnDrawGizmosSelected()
    {
        var collider = GetComponent<BoxCollider2D>();
        if (collider == null) return;
        Bounds bounds = collider.bounds;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(bounds.center.x, bounds.min.y - .025f, 0f), new Vector3(bounds.size.x * .8f, .07f, 0f));
    }
}
