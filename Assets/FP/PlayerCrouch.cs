using System.Collections;
using UnityEngine;

public class PlayerCrouch : MonoBehaviour
{
    private Coroutine crouchRoutine;
    
    public bool isCrouching = false;

    public GameObject camera;
    public CapsuleCollider collider;

    public float camCrouchHeight;
    public float colliderCrouchHeight;
    public float camStandHeight;
    public float colliderStandHeight;

    public float crouchSpeed = 8f;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            isCrouching = !isCrouching;
            ToggleCrouch(isCrouching);
        }
    }

    public void ToggleCrouch(bool crouching)
    {
        if (crouchRoutine != null)
            StopCoroutine(crouchRoutine);

        crouchRoutine = StartCoroutine(CrouchRoutine(crouching));
    }

    public void StandUp()
    {
        collider.height = colliderStandHeight;
        Vector3 camLocalPos = camera.transform.localPosition;
        camLocalPos.y = camStandHeight;
        camera.transform.localPosition = camLocalPos;
    }

    private IEnumerator CrouchRoutine(bool crouching)
    {
        // Pick target values
        float targetCamY = crouching ? camCrouchHeight : camStandHeight;
        float targetColliderHeight = crouching ? colliderCrouchHeight : colliderStandHeight;

        float startCamY = camera.transform.localPosition.y;
        float startColliderHeight = collider.height;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * crouchSpeed;

            // Lerp camera height
            float newCamY = Mathf.Lerp(startCamY, targetCamY, t);
            Vector3 camLocalPos = camera.transform.localPosition;
            camLocalPos.y = newCamY;
            camera.transform.localPosition = camLocalPos;

            // Lerp collider height
            collider.height = Mathf.Lerp(startColliderHeight, targetColliderHeight, t);
            
            yield return null;
        }
    }
}
