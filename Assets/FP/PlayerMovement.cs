using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] public bool movementEnabled = true;
    //** Player body and input info **\\
    [Header("Input Info")]
    public Rigidbody playerRigidbody;
    // Direction player is looking
    [SerializeField] private Transform playerOrientation;
    //public PlayerCrouch playerCrouch;

    public float walkSpeed;
    public float crouchSpeed;
    // Inputs
    private float horizontalInput;
    private float verticalInput;
    // Direction to move
    private Vector3 moveDirection;
    // States the player is in
    public enum State {Crouch, Walk, Crawl};
    // The movement speed of the current mode
    [SerializeField] private float movementSpeed;
    // Walk state by default
    public State currState;
    // Map of states to speeds
    private Dictionary < State, float > speedMap = new Dictionary < State, float > ();

    
    private float raycastBufferSize = 1f;
    [Header("Head Check")]
    public float headCheckRadius = 0.45f;

    //** Ground Check **\\

    [Header("Ground Check")]
    [SerializeField] private float playerHeight;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundDrag = 3f;
    // If player is touching the ground
    [SerializeField] public bool isGrounded;
    

    [Header("Slope Handling")]
    // Max angle allowed to move up
    public float maxSlopeAngle;
    // Info of slope hit with raycast
    private RaycastHit slopeHit;
    // Currently leaving a slop object
    private bool exitingSlope;
    // Debug to see if currently on slope
    [SerializeField] bool onSlope;

    //** Collide and slide **\\

    [Header("Collide and Slide")]
    // Max iterations of the collide and slide algorithm
    private int maxBounces = 5;
    // Inner thickness of collider to check from
    private float skinWidth = 0.015f;
    // Player collider
    [SerializeField] private Collider _collider;
    // Bounds of the skin of our hitbox
    Bounds bounds;
    // Layermask for collide and slide to apply to
    [SerializeField] private LayerMask collideMask;

    // Start is called before the first frame update
    private void Start()
    {
        // Initialize values of speedMap
        speedMap[State.Walk] = walkSpeed;

        playerRigidbody = GetComponent<Rigidbody>();
        playerRigidbody.freezeRotation = true;
        
        bounds = _collider.bounds;
        bounds.Expand(-2 * skinWidth);
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        StateHandler();
    }


    // Update is called once per frame
    private void Update()
    {
        if (!movementEnabled)
        {
            return;
        }
        
        // Fall Check for fall damage
        bool prevGrounded = isGrounded;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.05f + raycastBufferSize, groundMask);
        
        MyInput();  
        SpeedControl();
        onSlope = OnSlope();

        if (isGrounded)
        {
            playerRigidbody.linearDamping = groundDrag;
        } 
        else 
        {
            playerRigidbody.linearDamping = 0.5f;
        }
        
        
        MovePlayer();
    }

    private void MovePlayer()
    {
        // Set speed of movement based off player state

       
        movementSpeed = walkSpeed;
            
        moveDirection = playerOrientation.forward * verticalInput + playerOrientation.right * horizontalInput;

        // On a slope apply a down force to keep player on it
        if (OnSlope() && !exitingSlope)
        {
            playerRigidbody.AddForce(GetSlopeMoveDirection() * movementSpeed * Time.deltaTime * 5f, ForceMode.Force);
        }
        // If grounded then apply friction of ground
        if (isGrounded)
        {
            if (moveDirection != Vector3.zero)
            {
                //footstep.PlayFootstep(currState);
            }

            playerRigidbody.AddForce(moveDirection.normalized * movementSpeed * Time.deltaTime * 10f, ForceMode.Force);
            
        }
        
    }

    private void SpeedControl()
    {
        if (OnSlope() && !exitingSlope)
        {
            if (playerRigidbody.linearVelocity.magnitude > movementSpeed)
            {
                playerRigidbody.linearVelocity = playerRigidbody.linearVelocity.normalized * movementSpeed;
            }
        }
        else
        {
            Vector3 flatVel = new Vector3(playerRigidbody.linearVelocity.x, 0f, playerRigidbody.linearVelocity.z);

            if (flatVel.magnitude > movementSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * movementSpeed;
                playerRigidbody.linearVelocity = new Vector3(limitedVel.x, playerRigidbody.linearVelocity.y, limitedVel.z);
            }
        }
    }
    
    private void StateHandler()
    {
        
    }

    /// Adjust the velocity vector of the player running into a wall at an angle to slide along it
    // pos : Starting position of player
    // vel : Starting velocity of player
    // depth : current bounce recursion number
    // velInit : the direction the player is initally trying to go
    private Vector3 CollideAndSlide(Vector3 pos, Vector3 vel, int depth, Vector3 velInit)
    {
        if (depth >= maxBounces)
        {
            return Vector3.zero;
        }

        float dist = vel.magnitude + skinWidth;
        // Check for hit on wall plane
        RaycastHit hit;
        if (Physics.SphereCast(pos, bounds.extents.x, vel.normalized, out hit, dist, collideMask))
        {
            Vector3 snapToSurface = vel.normalized * (hit.distance - skinWidth);
            Vector3 leftOver = vel - snapToSurface;

            if (snapToSurface.magnitude <= skinWidth)
            {
                snapToSurface = Vector3.zero;
            }

            float scale = 1 - Vector3.Dot(
                new Vector3(hit.normal.x, 0, hit.normal.z).normalized,
                -new Vector3(velInit.x, 0, velInit.z).normalized
            );

            leftOver = ProjectAndScale(leftOver, hit.normal) * scale;

            return snapToSurface + CollideAndSlide(leftOver, pos + snapToSurface, depth + 1, velInit);
        }

        return vel;
    }

    private Vector3 ProjectAndScale(Vector3 vec, Vector3 normal)
    {
        float mag = vec.magnitude;
        vec = Vector3.ProjectOnPlane(vec, normal).normalized;
        vec *= mag;

        return vec;
    }

    private void Jump()
    {
        //exitingSlope = true;

        //playerRigidbody.linearVelocity = new Vector3(playerRigidbody.linearVelocity.x, 0f, playerRigidbody.linearVelocity.z);
        //playerRigidbody.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        //readyToJump = true;
        //exitingSlope = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + raycastBufferSize))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle > 0f;
        }
       
        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
}
