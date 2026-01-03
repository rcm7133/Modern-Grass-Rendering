using UnityEngine;

public class Footsteps : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] footstepSounds;

    private float stepTimer;

    public float walkStepInterval = 0.33f;
    public float crouchStepInterval = 0.15f;

    public PlayerMovement playerMovement;
    public PlayerCrouch playerCrouch;

    void Update()
    {
        // Check movement speed and grounded
        if (playerMovement.isGrounded && playerMovement.playerRigidbody.linearVelocity.magnitude > 0.2f)
        {
            float interval = playerCrouch.isCrouching ? crouchStepInterval : walkStepInterval;

            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                PlayFootstep();
                stepTimer = interval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    void PlayFootstep()
    {
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(footstepSounds[Random.Range(0, footstepSounds.Length)]);
    }
}
