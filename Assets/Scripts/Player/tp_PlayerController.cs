using UnityEngine;
using UnityEngine.InputSystem;

public class tp_PlayerController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private PlayerInput input;
    [SerializeField] private Transform cam;
    [SerializeField] private Animator anim;

    //Movement
    [Header("Movement Parameters")]
    [SerializeField] float jumpHeight = 3f;
    [SerializeField] private float turnSmoothTime = 0.1f;
    [SerializeField] float slideSpeed = 8;
    [SerializeField] private float movementSpeed;
     private float turnSmoothvelocity;

    [Header("Gravity & Ground Parameters")]
    [SerializeField] private float gravity = 2f;
    [SerializeField] float groundCheckRadius = 1f;
    [SerializeField] float groundCheckOffset = 0f;
    [SerializeField] LayerMask groundLayer;
    private bool isGrounded;
    Vector3 velocity;
    private float halfHeight;
    private Vector3 hitNormal;

    enum MovementState
    {
        Walking,
        Swimming
    }

    [SerializeField]
    MovementState _movementState;

    private void Start()
    {
        cam = Camera.main.transform;

        //Gets the center of the lower circle of the player controller capsule
        halfHeight = controller.height * 0.5f - controller.radius;
    }

    // Called 50 times a second
    private void FixedUpdate()
    {
        isGrounded = GroundCheck();
    }

    // Update is called once per frame
    void Update()
    {
        HandleAnim();

        switch (_movementState)
        {
            case MovementState.Walking:
                Move();
                break;
        }
    }

    public void Move()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -0.5f;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        Vector2 _input = input.actions["Move"].ReadValue<Vector2>();
        Vector3 movemenetVector = new Vector3(_input.x, 0, _input.y);

        Vector3 moveDir = Vector3.zero;

        //Interprects the players movement direction based on the direction of the players camera
        if (movemenetVector.normalized.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(movemenetVector.x, movemenetVector.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothvelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }

        if (IsSliding())
        {
            moveDir += new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z) * slideSpeed;
        }

        controller.Move((moveDir + velocity) * Time.deltaTime * movementSpeed);
    }

    //Makes the player jump with the desired force
    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            anim.SetTrigger("Jump");
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }


    public void HandleAnim()
    {
        if (anim == null) return;

        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("Y Velocity", velocity.y);
        anim.SetFloat("MoveSpeed", input.actions["Move"].ReadValue<Vector2>().magnitude);
    }

    public void Teleport(Vector3 location)
    {
        controller.enabled = false;
        transform.position = location;
        controller.enabled = true;
    }

    //Handles checking for ground beneath the player and retrieving and processing the angle of the ground the player is standing on
    #region Handle Ground & Slope Checking

    //Creates a sphere at the bottom of the character controller collider to check if they are in contact with the ground
    bool GroundCheck()
    {
        Vector3 bottomPoint = transform.TransformPoint(controller.center - Vector3.up * (halfHeight + groundCheckOffset));
        return Physics.CheckSphere(bottomPoint, groundCheckRadius, groundLayer);
    }

    private bool IsSliding()
    {
        float angle = Vector3.Angle(hitNormal, Vector3.up);
        return angle > controller.slopeLimit && isGrounded;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Vector3 bottomPoint = transform.TransformPoint(controller.center - Vector3.up * (halfHeight + groundCheckOffset));

        if (hit.point.y < bottomPoint.y)
        {
            hitNormal = hit.normal;

            Debug.DrawRay(hit.point, hit.normal * 5f);
        }
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (GroundCheck())
            Gizmos.color = Color.green;
        else
            Gizmos.color = Color.red;

        var bottomPoint = transform.TransformPoint(controller.center - Vector3.up * (halfHeight + groundCheckOffset));

        Gizmos.DrawWireSphere(bottomPoint, groundCheckRadius);
    }


}

