using System.Collections;
using UnityEngine;

public class PlayerVaulting : MonoBehaviour
{
    public bool isVaulting = false;
    
    public PlayerMovement playerMovement;
    public PlayerLook playerLook;
    public Camera playerCamera;
    public Transform rayOrigin;
    public float rayDistance;
    public LayerMask layerMask;
    public CapsuleCollider playerCollider;
    public CapsuleCollider outlineCollider;

    public float warpSpeed;
    public float offsetAmount;
    public float pointTolerance;
    public float sweepOffset;
    public float groundCheckDistance;
    
    public float maxVaultableHeight;
    public float maxVaultableWidth;
    
    public Vector3 debugOffsetPoint;
    public Vector3 debugGroundPoint;
    public Vector3 debugEntryPoint;
    public Vector3 debugNearestPoint;
    public Vector3 debugLeftOfNearestPoint;
    public Vector3 debugRightOfNearestPoint;
    
    public Vector3 debugOldNearestPoint;
    public Vector3 debugOldLeftOfNearestPoint;
    public Vector3 debugOldRightOfNearestPoint;
    public Vector3 debugFarOfOppositePoint;
    
    public Vector3 debugSweepBegin;
    public Vector3 debugSweepEnd;
    
    public Vector3 debugAdjustedFarthestPoint;
    public Vector3 debugGroundHitPoint;
    
    public Vector3 lookAtPoint;
    

    // For calculations
    public Vector3 oppositePoint;
    public Vector3 closestPoint;
    public Vector3 surfaceNormal;
    public bool showDebugPoint;

    public IEnumerator VaultSequence()
    {
        isVaulting = true;
        
        bool coverHit = false;
        
        // Debug bullshit
        debugOffsetPoint = Vector3.negativeInfinity;        
        debugGroundPoint = Vector3.positiveInfinity;
        debugEntryPoint = Vector3.negativeInfinity;
        debugLeftOfNearestPoint = Vector3.positiveInfinity;
        debugRightOfNearestPoint = Vector3.positiveInfinity;
        debugNearestPoint = Vector3.negativeInfinity;
        debugOldNearestPoint = Vector3.positiveInfinity;
        debugOldLeftOfNearestPoint = Vector3.positiveInfinity;
        debugOldRightOfNearestPoint = Vector3.positiveInfinity;
        
        debugSweepBegin =  Vector3.negativeInfinity;
        debugSweepEnd =  Vector3.negativeInfinity;
        debugAdjustedFarthestPoint =  Vector3.negativeInfinity;
        debugGroundHitPoint = Vector3.negativeInfinity;
        lookAtPoint = Vector3.negativeInfinity;
        
        RaycastHit hit;
        
        coverHit = Physics.Raycast(rayOrigin.position, rayOrigin.forward, out hit, rayDistance, layerMask);

        if (!coverHit)
        {
            isVaulting = false;
            yield break;
        }

        if (hit.collider.bounds.size.x < playerCollider.radius * 2f)
        {
            Debug.Log("Not wide enough");
            isVaulting = false;
            yield break;
        }

        if (hit.collider.bounds.size.y > maxVaultableHeight)
        {
            Debug.Log("Too high");
            isVaulting = false;
            yield break;
        }
        
        if (hit.collider.bounds.size.z > maxVaultableWidth)
        {
            Debug.Log("Too Far to go over");
            isVaulting = false;
            yield break;
        }
        
        Debug.DrawRay(rayOrigin.position, rayOrigin.forward * rayDistance, Color.red, 3f);
            
        Debug.Log($"Length: {hit.collider.bounds.size.x},  Height: {hit.collider.bounds.size.y}, Width: {hit.collider.bounds.size.z}");
        
        Vector3 entryPoint = GetVaultEntryPosition(hit.collider, hit);

        if (entryPoint == Vector3.negativeInfinity)
        {
            isVaulting = false;
            yield break;
        }
        
        Vector3 sweepStart = closestPoint + Vector3.up * (playerCollider.height / 2f) + sweepOffset * Vector3.up;
        debugSweepBegin = sweepStart;
        Vector3 sweepEnd = oppositePoint + Vector3.up * (playerCollider.height / 2f) + sweepOffset * Vector3.up;
        debugSweepEnd = sweepEnd;

        if (PlayerBlockedBetweenTwoPoints(sweepStart, sweepEnd, hit.collider))
        {
            Debug.Log("Blocked");
            isVaulting = false;
            yield break;
        }
        
        Vector3 groundHitPoint = GetGroundPoint(oppositePoint, hit.collider);
        
        lookAtPoint = groundHitPoint + Vector3.up * (playerCollider.height );
        
        playerMovement.enabled = false;
        playerLook.lookEnabled = false;
        
        yield return StartCoroutine(WarpToPosition(entryPoint));
        yield return StartCoroutine(WarpToPosition(groundHitPoint +  Vector3.up * playerCollider.height / 2f));
        
        playerMovement.enabled = true;
        playerLook.lookEnabled = true;
    }
    
    // Smoothly move player to entry position
    private IEnumerator WarpToPosition(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;

        float elapsed = 0f;
        float duration = Vector3.Distance(startPosition, targetPosition) / warpSpeed;

        // --- Compute a fixed rotation toward lookAtPoint ---
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = startRotation;

        if (lookAtPoint != Vector3.negativeInfinity)
        {
            targetRotation = GetLevelLookRotation(lookAtPoint);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Move
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            // Rotate toward the landing/look point
            if (lookAtPoint != Vector3.negativeInfinity)
            {
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            }

            yield return null;
        }

        transform.position = targetPosition;

        // Snap to final rotation
        if (lookAtPoint != Vector3.negativeInfinity)
        {
            transform.rotation = GetLevelLookRotation(lookAtPoint);
        }
    }
    
    public Quaternion GetLevelLookRotation(Vector3 lookTarget)
    {
        // Direction from player to target
        Vector3 dir = lookTarget - playerCamera.transform.position;

        // Remove vertical influence
        dir.y = 0f;  

        // Handle degenerate case
        if (dir.sqrMagnitude < 0.0001f)
            return playerCamera.transform.rotation;

        return Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    public Vector3 GetExitPosition()
    {
        return Vector3.negativeInfinity;
    }

    public Vector3 GetGroundPoint(Vector3 farthestPoint, Collider cover)
    {
        // Adjust the point based on player collider
        Vector3 adjustedFarthestPoint = farthestPoint - surfaceNormal * playerCollider.radius - surfaceNormal * 0.15f  + Vector3.up * playerCollider.height / 2;
        debugAdjustedFarthestPoint = adjustedFarthestPoint;

        // Calculate capsule top and bottom points for the sweep
        float capsuleHeight = playerCollider.height;
        float capsuleRadius = playerCollider.radius;

        Vector3 capsuleBottom = adjustedFarthestPoint - Vector3.up * (capsuleHeight / 2 - capsuleRadius);
        Vector3 capsuleTop = adjustedFarthestPoint + Vector3.up * (capsuleHeight / 2 - capsuleRadius);

        RaycastHit hit;
        if (Physics.CapsuleCast(capsuleTop, capsuleBottom, capsuleRadius, Vector3.down, out hit, 10f))
        {
            // Return the hit point on the ground
            debugGroundHitPoint = hit.point;
            Debug.Log(hit.collider.name);
            return hit.point;
        }

        // If nothing is hit, fallback to the original adjusted point
        return Vector3.negativeInfinity;
    }

    public Vector3 GetVaultEntryPosition(Collider cover, RaycastHit hit)
    {
        Vector3 nearestPoint = cover.ClosestPoint(playerCollider.transform.position);
        
        Vector3 surfaceRight = Vector3.Cross(hit.normal, Vector3.up).normalized;
        
        surfaceNormal = hit.normal;
        
        Vector3 leftOfNearestPoint = nearestPoint - surfaceRight * playerCollider.radius;
        Vector3 rightOfNearestPoint = nearestPoint + surfaceRight * playerCollider.radius;

        if (Vector3.Distance(leftOfNearestPoint, cover.ClosestPoint(leftOfNearestPoint)) > pointTolerance)
        {
            debugOldRightOfNearestPoint = rightOfNearestPoint;
            debugOldLeftOfNearestPoint = leftOfNearestPoint;
            debugOldNearestPoint = nearestPoint;
            
            Vector3 moveOffset = surfaceRight * Vector3.Distance(leftOfNearestPoint, cover.ClosestPoint(leftOfNearestPoint));
            
            rightOfNearestPoint += moveOffset;
            leftOfNearestPoint += moveOffset;
            nearestPoint +=  moveOffset;
        }
        
        if (Vector3.Distance(rightOfNearestPoint, cover.ClosestPoint(rightOfNearestPoint)) > pointTolerance)
        {
            debugOldRightOfNearestPoint = rightOfNearestPoint;
            debugOldLeftOfNearestPoint = leftOfNearestPoint;
            debugOldNearestPoint = nearestPoint;
            
            Vector3 moveOffset = surfaceRight * Vector3.Distance(rightOfNearestPoint, cover.ClosestPoint(rightOfNearestPoint));
            
            rightOfNearestPoint -= moveOffset;
            leftOfNearestPoint -= moveOffset;
            nearestPoint -=  moveOffset;
        }
        
        debugLeftOfNearestPoint = leftOfNearestPoint;
        debugRightOfNearestPoint = rightOfNearestPoint;
        debugNearestPoint = nearestPoint;
        
        Vector3 direction = (nearestPoint - playerCollider.transform.position).normalized;
        Vector3 offsetPoint = nearestPoint - direction * playerCollider.radius - direction * offsetAmount;

        closestPoint = nearestPoint;
        
        debugOffsetPoint = offsetPoint;
        
        bool groundHit = Physics.Raycast(offsetPoint, Vector3.down, out RaycastHit hit2, rayDistance, layerMask);
       
        if (!groundHit)
            return Vector3.negativeInfinity;
        
        debugGroundPoint = hit2.point;
        
        Vector3 finalPoint = hit2.point + Vector3.up * (playerCollider.height / 2);
        
        debugEntryPoint = finalPoint;
        
        oppositePoint = GetOppositePoint(nearestPoint, cover, surfaceNormal);
        
        return finalPoint;
    }

    public Vector3 GetOppositePoint(Vector3 closestPoint, Collider cover, Vector3 normal)
    {
        float largestBound = float.NegativeInfinity;
        if (cover.bounds.size.x > largestBound)
            largestBound = cover.bounds.size.x;
        if (cover.bounds.size.y > largestBound)
            largestBound = cover.bounds.size.y;
        if (cover.bounds.size.z > largestBound)
            largestBound = cover.bounds.size.z;
        
        Debug.Log(largestBound);
        
        Vector3 farOppositePoint = closestPoint - normal * largestBound;
        debugFarOfOppositePoint = farOppositePoint;
        

        oppositePoint = cover.ClosestPoint(farOppositePoint);
        
        return cover.ClosestPoint(farOppositePoint);
    }

    public bool PlayerBlockedBetweenTwoPoints(Vector3 start, Vector3 end, Collider cover)
    {
        // Direction and distance between the two points
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);
    
        // CapsuleCast needs the two end points of the capsule's internal spine
        // For a standing capsule, these are top and bottom hemispheres
        Vector3 point1 = start;
        Vector3 point2 = start; // Same position since we want a vertical capsule at the start point
    
        // Actually, for sweeping along a path, use the center point
        Vector3 center = start;
    
        // Cast the capsule from start to end
        RaycastHit[] hits = Physics.CapsuleCastAll(
            point1, 
            point2, 
            playerCollider.radius, 
            direction, 
            distance,
            layerMask
        );
    
        // Check if anything blocks the path (except the cover itself and player)
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null && 
                hit.collider != playerCollider && 
                hit.collider != cover &&
                hit.collider != outlineCollider)
            {
                Debug.Log($"Blocked by: {hit.collider.name}");
                return true;
            }
        }
    
        return false;
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(VaultSequence());
        }
    }
    
    void OnDrawGizmos()
    {
        if (showDebugPoint)
        {
            Gizmos.color = Color.red;
            if (debugOffsetPoint != Vector3.negativeInfinity)
                Gizmos.DrawSphere(debugOffsetPoint, 0.3f);
            
            Gizmos.color = Color.yellow;
            if (debugGroundPoint != Vector3.negativeInfinity)
                Gizmos.DrawCube(debugGroundPoint,  new Vector3(0.1f, 0.1f, 0.1f));
            
            Gizmos.color = Color.green;
            if (debugEntryPoint != Vector3.negativeInfinity)
                Gizmos.DrawSphere(debugEntryPoint, 0.2f);
            
            Gizmos.color = Color.red;
            if (debugNearestPoint != Vector3.negativeInfinity)
                Gizmos.DrawSphere(debugNearestPoint, 0.1f);
            
            Gizmos.color = Color.yellow;
            if (debugLeftOfNearestPoint != Vector3.negativeInfinity)
                Gizmos.DrawSphere(debugLeftOfNearestPoint, 0.1f);
            
            Gizmos.color = Color.cyan;
            if (debugRightOfNearestPoint != Vector3.negativeInfinity)
                Gizmos.DrawSphere(debugRightOfNearestPoint, 0.1f);
            
            Gizmos.color = Color.red;
            if (debugOldNearestPoint != Vector3.negativeInfinity)
                Gizmos.DrawWireSphere(debugOldNearestPoint, 0.1f);
            
            Gizmos.color = Color.yellow;
            if (debugOldLeftOfNearestPoint != Vector3.negativeInfinity)
                Gizmos.DrawWireSphere(debugOldLeftOfNearestPoint, 0.1f);
            
            Gizmos.color = Color.cyan;
            if (debugOldRightOfNearestPoint != Vector3.negativeInfinity)
                Gizmos.DrawWireSphere(debugOldRightOfNearestPoint, 0.1f);
            
            Gizmos.color = Color.magenta;
            if (oppositePoint != Vector3.negativeInfinity)
                Gizmos.DrawWireSphere(oppositePoint, 0.1f);
            
            Gizmos.color = Color.magenta;
            if (debugFarOfOppositePoint != Vector3.negativeInfinity)
                Gizmos.DrawSphere(debugFarOfOppositePoint, 0.1f);
            
            Gizmos.color = Color.green;
            if (debugSweepBegin != Vector3.negativeInfinity)
                Gizmos.DrawSphere(debugSweepBegin, 0.1f);
            
            Gizmos.color = Color.red;
            if (debugSweepEnd != Vector3.negativeInfinity)
                Gizmos.DrawSphere(debugSweepEnd, 0.1f);
            
            Gizmos.color = Color.red;
            if (debugAdjustedFarthestPoint!= Vector3.negativeInfinity)
                Gizmos.DrawWireSphere(debugAdjustedFarthestPoint, 0.1f);
            
            Gizmos.color = Color.green;
            if (debugGroundHitPoint != Vector3.negativeInfinity)
                Gizmos.DrawSphere(debugGroundHitPoint, 0.1f);
            
            Gizmos.color = Color.black;
            if (lookAtPoint != Vector3.negativeInfinity)
                Gizmos.DrawSphere(lookAtPoint, 0.1f);
        }
    }
}
