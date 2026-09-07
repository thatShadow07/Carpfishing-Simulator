using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 6f;

    [Header("Gravity")]
    public float gravity = -20f;

    private CharacterController characterController;
    private float verticalVelocity;

    private float jumpHeight;


    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleMovement();

        Jump();

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if(isGrounded && velocity.y < 0){

            velocity.y = -2f;

        }
    }

    private void HandleMovement()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            input = new Vector2(
                (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f),
                (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f)
            );
        }

        input = Vector2.ClampMagnitude(input, 1f);

        float speed = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed
            ? sprintSpeed
            : walkSpeed;

        Vector3 move = transform.right * input.x + transform.forward * input.y;

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * speed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);



        private void Jump()
        {
            if(Input.GetButtonDown("Jump") && isGrounded){

                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            }            
        }
    }
}