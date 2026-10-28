using Game;
using UnityEngine;

public class FieldFollower : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform target;
    [SerializeField] private Animator animator = default;
    [SerializeField] private SpriteRenderer spriteRenderer = default;

    [Header("Animator Params")]
    [SerializeField] private string isMovingParam = "isMoving";
    [SerializeField] private string moveXParam = "moveX";
    [SerializeField] private string moveYParam = "moveY";

    [Header("最小位移阈值")]
    [SerializeField] private float movementThreshold = 0.001f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundSnapForce = -2f;
    [SerializeField] private float maxVelocity = -40f;

     private float verticalVelocity;

     private void Awake()
     {
        // if (animator == null)
             //animator = GetComponentInChildren<Animator>();

         if (spriteRenderer == null)
             spriteRenderer = GetComponentInChildren<SpriteRenderer>();

         if (characterController == null)
             characterController = GetComponent<CharacterController>();
     }

     public void SetupFollower(CharacterDefinitionSO definition)
    {
        // if (definition == null)
        //     return;

        // // 没有 Animator/动画控制器时，至少显示一张静态图
        // if (spriteRenderer != null && definition.Portrait != null)
        //     spriteRenderer.sprite = definition.Portrait;

       // if (animator != null)
        //{
            // 允许 definition.fieldAnimator 为空（例如临时占位角色）
            animator.runtimeAnimatorController = definition.fieldAnimator;
    //}
}

public void MoveTo(Vector3 targetPosition, float speed)
{
    Vector3 toTarget = targetPosition - transform.position;
    toTarget.y = 0; // 只考虑水平移动

    Vector3 horizontalStep = Vector3.ClampMagnitude(toTarget, Mathf.Max(0f ,speed) * Time.deltaTime);

    // 检查角色是否在地面上
    bool isGrounded = characterController.isGrounded;

    // 如果角色在地面上且垂直速度小于0
    if (isGrounded && verticalVelocity < 0)
    {
        // 设置垂直速度为地面吸附力，使角色稳定在地面上
        verticalVelocity = groundSnapForce;
    }
    else
    {
        // 应用重力效果，乘以Time.deltaTime确保与帧率无关
        verticalVelocity += gravity * Time.deltaTime;
        // 限制最大垂直速度
        if (verticalVelocity < maxVelocity)
            verticalVelocity = maxVelocity;
    }

    Vector3 movement = horizontalStep;
    movement.y = verticalVelocity * Time.deltaTime;

    characterController.Move(movement);
    UpdateAnimation(horizontalStep);
}

public void SnapTo(Vector3 position)
{
    bool enabled = characterController != null && characterController.enabled;

    if (characterController != null)
        characterController.enabled = false;

    transform.position = position;

    if (characterController != null)
        characterController.enabled = enabled;

    verticalVelocity = 0f;

    if (animator != null)
        animator.SetBool(isMovingParam, false);
}
    private void UpdateAnimation(Vector3 step)
    {
        bool isMoving = step.sqrMagnitude > movementThreshold * movementThreshold;
        animator.SetBool(isMovingParam, isMoving);

        if(isMoving)
        {
            animator.SetFloat(moveXParam, step.x);
            animator.SetFloat(moveYParam, step.z);
        }
    }
}