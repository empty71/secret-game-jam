using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    private float movespeed;
    public float walkspeed;
    public float sprintspeed;
    public float slideSpeed;
    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;


    public float groundDrag;


    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;
    

    [Header("keybinds")]
    public KeyCode jumpkey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.Z;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;


    public Transform orientation;


    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    public MovementState state;
    public enum MovementState 
    {
        walking,
        sprinting,
        crouching,
        sliding,
        air
    }

    public bool sliding;


    Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;

        startYScale = transform.localScale.y;
    }
    void Update()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
        MyInput();
        SpeedControl();
        StateHandler();

        if (grounded)
        {
            rb.drag = groundDrag;
        }
        else
        {
            rb.drag = 0;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        
        if(Input.GetKeyDown(jumpkey) && readyToJump && grounded)
        {
            Debug.Log("jump");
            readyToJump = true;
            Jump();

            Invoke(nameof(resetJump), jumpCooldown);
        }

        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale , transform.localScale.z);
        }
    }

    private void StateHandler()
    {
       if (sliding)
       {
           state = MovementState.sliding;

            if (OnSlope()&& rb.velocity.y < 0.1f)
            {
                desiredMoveSpeed = slideSpeed;
            }
            else
            {
                desiredMoveSpeed = sprintspeed;
            }

       }
       else if (Input.GetKey(crouchKey))
       {
            state = MovementState.crouching;
            desiredMoveSpeed = crouchSpeed;
       }

       else if(grounded && Input.GetKey(sprintKey))
       {
            state = MovementState.sprinting;
            desiredMoveSpeed = sprintspeed;
       }
       else if (grounded)
       {
           state = MovementState.walking;
           desiredMoveSpeed = walkspeed;
       }
       else
       {
           state = MovementState.air;
       }
        // check if desiredmovespeed has changed drastically
        if (Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 4f && movespeed!= 0)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMoveSpeed());
        }
        else
        {
            movespeed = desiredMoveSpeed;
        }


        lastDesiredMoveSpeed = desiredMoveSpeed;
    }

    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        //smoothly lerp movespeed to desired value
        float time = 0;
        float diffrence = Mathf.Abs(desiredMoveSpeed - movespeed);
        float startValue = movespeed;

        while(time< diffrence)
        {
            movespeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / diffrence);
            time += Time.deltaTime;
            yield return null;
        }
        movespeed = desiredMoveSpeed;
    }

    private void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * movespeed * 20f, ForceMode.Force);
            if (rb.velocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }

        }

        if (grounded)
        {
        rb.AddForce(moveDirection.normalized * movespeed * 10f, ForceMode.Force);
        }else if (!grounded)
        {
            rb.AddForce(moveDirection.normalized * movespeed * 10f * airMultiplier, ForceMode.Force);
        }

    }

    private void SpeedControl()
    {
        if (OnSlope() && !exitingSlope)
        {
            if(rb.velocity.magnitude > movespeed)
            {
                rb.velocity = rb.velocity.normalized * movespeed;
            }
        }
        else
        {
            Vector3 flatvel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

             if(flatvel.magnitude > movespeed)
             {
               Vector3 limitedvel = flatvel.normalized * movespeed;
               rb.velocity = new Vector3(limitedvel.x, rb.velocity.y, limitedvel.z);
             }
        }

    }

    private void Jump()
    {
        exitingSlope = true;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);


        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    public void resetJump()
    {
        readyToJump = true;

        exitingSlope = false;
    }

    public bool OnSlope()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }
    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }
}
