using UnityEngine;

public class PlayerCrawl : MonoBehaviour
{
    public PlayerLook playerLook;
    public GameObject playerBody;
    public GameObject playerCamera;
    public float height;
    public float speed;

    public Transform camTransform;

    public float groundCheckDistance = 1.0f;
    public LayerMask groundMask;
    
    public Animator animator;
    private bool isCrawling = false;
    private bool wantToCrawl = false;
    private bool nextIsRightArm = true;

    private bool canDragMove = false; // Controlled by animation events

    private void Start()
    {
        playerLook.restrictPitch = true;

        playerCamera.transform.localPosition = new Vector3(0, height, 0);
    }

    void Update()
    {
        // Player wants to crawl
        if (Input.GetKeyDown(KeyCode.W))
        {
            wantToCrawl = true;

            if (!isCrawling)
            {
                StartCrawlCycle();
            }
        }

        // Player released W
        if (Input.GetKeyUp(KeyCode.W))
        {
            wantToCrawl = false;
            // Do NOT stop now — wait for animation to finish
        }

        if (canDragMove)
        {
            MoveAlongGround();
        }
    }

    private void StartCrawlCycle()
    {
        isCrawling = true;
        animator.SetBool("isCrawling", true);

        if (nextIsRightArm)
            animator.Play("RightArmCrawl");
        else
            animator.Play("LeftArmCrawl");
    }

    // -----------------------
    // Animation Event
    // -----------------------
    public void ArmCrawlCompleted()
    {
        // Flip the arm for next time
        nextIsRightArm = !nextIsRightArm;

        if (wantToCrawl)
        {
            // Continue crawling
            StartCrawlCycle();
        }
        else
        {
            // Player let go of W, so return to idle
            isCrawling = false;
            animator.SetBool("isCrawling", false);
        }
    }

    private void MoveAlongGround()
    {
        // Raycast down to get terrain normal
        if (Physics.Raycast(playerBody.transform.position + Vector3.up * 0.1f,
                Vector3.down,
                out RaycastHit hit,
                groundCheckDistance,
                groundMask))
        {
            Vector3 groundNormal = hit.normal;

            // Project forward onto the ground plane
            Vector3 forward = camTransform.forward;
            Vector3 projectedForward = Vector3.ProjectOnPlane(forward, groundNormal).normalized;

            // Move along the terrain
            playerBody.transform.position += projectedForward * speed * Time.deltaTime;
        }
        else
        {
            // Fallback if no ground
            playerBody.transform.Translate(camTransform.forward * speed * Time.deltaTime);
        }
    }

    // ---------------------------
    // Animation Event Functions
    // ---------------------------

    public void StartDrag()
    {
        canDragMove = true;
    }

    public void EndDrag()
    {
        canDragMove = false;
    }
}
