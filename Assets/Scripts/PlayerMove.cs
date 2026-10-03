using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    private Rigidbody2D body;
    private Animator anim;
    private bool grounded;
    private PlayerInput playerInput;
    public float speed = 5f;
    public float jumpForce = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // grab references for the components
        playerInput = GetComponent<PlayerInput>();
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        Jump();
        Move();
    }

    void Jump()
    {
        if (playerInput.actions["Jump"].triggered && grounded)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
            anim.SetTrigger("jump");
            grounded = false;
        }
    }

    void Move()
    {
        float horizontalInput = playerInput.actions["Move"].ReadValue<Vector2>().x;


        // flip player when moving left or right
        if (horizontalInput > 0.01f)
        {
            transform.localScale = new Vector3(6, 6, 6);
        }
        else if (horizontalInput < -0.01f)
        {
            transform.localScale = new Vector3(-6, 6, 6);
        }


            Vector2 moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
        body.linearVelocity = new Vector2(moveInput.x * speed, body.linearVelocity.y);

        // set animator parameters
        anim.SetBool("run", horizontalInput != 0);
        anim.SetBool("grounded", grounded);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            grounded = true;
        }
    }
}
