/// <summary>
/// 实现人物的基本移动，同步实现动画效果
/// </summary>
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private CharacterController characterController;
    private Animator animator;
    [SerializeField] private float speed = 5f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundSnapForce = -2f;
    [SerializeField] private float maxVelocityY = -40f;
    private float verticalVelocity;

    private Vector2 movementInput;
    private bool isMoving;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        Movement();
        SetAnimation();
    }

    private void Movement()
    {
        var input = InputSystemController.Instance;

        if (input == null) return;

        movementInput = input.GetMovementInput();

        bool isGrounded = characterController.isGrounded;
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = groundSnapForce;
        }
       else
        {
            verticalVelocity += gravity * Time.deltaTime;
            if(verticalVelocity < maxVelocityY)
            {
                verticalVelocity = maxVelocityY;
            }
        }

        Vector3 velocity = new Vector3(movementInput.x,0,movementInput.y)*speed;
        velocity.y = verticalVelocity;


        characterController.Move(velocity * Time.deltaTime);
    }
    private void SetAnimation()
    {
        if(animator == null) return;
        isMoving = movementInput.magnitude > 0.1f;
        animator.SetBool("isMoving", isMoving);

        if (isMoving)
        {
            animator.SetFloat("moveX", movementInput.x);
            animator.SetFloat("moveY", movementInput.y);
        }

    }
}
