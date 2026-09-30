using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    private Rigidbody2D body;
    private PlayerInput playerInput;
    public float speed = 5f;
    public float jumpForce = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        body = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        Jump();
        Move();
    }

    void Jump()
    {
        if (playerInput.actions["Jump"].triggered)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
        }
    }

    void Move()
    {
        Vector2 moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
        body.linearVelocity = new Vector2(moveInput.x * speed, body.linearVelocity.y);
    }
}
