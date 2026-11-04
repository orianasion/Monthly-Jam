using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class BetterPlayerController : MonoBehaviour
{
    private Rigidbody rb;
    private Vector2 moveInput;
    private GameObject currentSnappedTo; ////
    private GameObject canSnapTo; ////
    private bool canSnap = true;
    public float distanceToWall; // at least half as large as the sprite, note that collider boundaries must be slightly greater than this distance
    private HashSet<GameObject> overlappingWalls = new HashSet<GameObject>();

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float jumpForce = 6f;
    private Vector2 moveInputAtSwitch = Vector2.zero;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.6f;

    [Header("Glide")] // Add all of our glide variables
    public InputAction glideButton;
    public float glideTimer = 2f;
    private float glideTime;
    public float glideSpeed = 6f;
    public bool gliding;
    public AnimationCurve gravityFalloff;


    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Enable your glide button
        glideButton.Enable();

        BoxCollider thisCollider = GetComponentInChildren<BoxCollider>();

        foreach (var collider in Physics.OverlapBox(transform.TransformPoint(thisCollider.center), thisCollider.size * 0.5f, transform.rotation, Physics.AllLayers, QueryTriggerInteraction.Collide))
        {
            Debug.Log(collider); // should probably check if there are multiple colliders that overlap with this one

            if (collider.CompareTag("Wall")) // need to create a wall tag
            {
                currentSnappedTo = collider.gameObject;
            }
        }

        if (currentSnappedTo != null)
        {
            ResetDistanceAndDirection();
        }

    }

    void OnMove(InputValue value)
    {
        Vector2 newInput = value.Get<Vector2>();

        if (newInput.y > 0f && moveInput.y <= 0f)
        {
            OnJump();
        }

        moveInput = newInput;
    }

    void OnJump()
    {
        if (IsGrounded())
        {   
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void FixedUpdate()
    {
        // automatically switch walls if in the same direction, but only allow switching again if the movement direction is reversed
        if (Mathf.Abs(moveInput.x) > 0 && (canSnap || moveInput.x * moveInputAtSwitch.x < 0))
        {
            SwitchSnapTo(); // works best for colliders which closely adhere to the walls, null checks are performed in SwitchSnapTo()
        }

        // Move with respect to wall, no y-velocity because snapped to wall
        Vector3 targetVelocity = moveInput.x * moveSpeed * transform.right;

        // glide, use upward motion to initiate
        if (!IsGrounded() && (gliding || rb.linearVelocity.y < 0f) && (glideButton.IsPressed() || moveInput.y > 0f))
        {
            if (!gliding)
            {
                rb.useGravity = false;
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                gliding = true;
            }

            targetVelocity = GlideVelocity();
        }
        else
        {
            glideTime = 0;
            rb.useGravity = true;
            targetVelocity.y = rb.linearVelocity.y;
            gliding = false;
        }

        rb.linearVelocity = targetVelocity;

        moveInputAtSwitch = moveInput;
    }

    Vector3 GlideVelocity()
    {
        Vector3 result = Vector3.zero;

        if (glideTime < glideTimer)
        {
            glideTime += Time.fixedDeltaTime;
        }

        // Implement our glide
        result = moveInput.y * glideSpeed * transform.forward + moveInput.x * glideSpeed * transform.right;

        result.y = rb.linearVelocity.y + gravityFalloff.Evaluate(glideTime / glideTimer) * Physics.gravity.y * Time.deltaTime;

        return result;
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, Physics.AllLayers);
    }

    private void SwitchSnapTo()
    {
        if (currentSnappedTo == null || canSnapTo == null || currentSnappedTo == canSnapTo || !overlappingWalls.Contains(canSnapTo)) return;

        GameObject lastSnapped = currentSnappedTo;
        currentSnappedTo = canSnapTo;
        canSnapTo = lastSnapped;

        canSnap = false;
        moveInputAtSwitch = moveInput;

        ResetDistanceAndDirection();

        if (!overlappingWalls.Contains(canSnapTo))
        {
            canSnapTo = null;
            canSnap = true;
        }
    }

    private void ResetDistanceAndDirection()
    {
        Vector3 wallNormal = currentSnappedTo.transform.up;
    
        transform.up = wallNormal;
        transform.rotation = Quaternion.LookRotation(Vector3.up, wallNormal); //////////

        float currentDistance = Vector3.Dot(transform.position - currentSnappedTo.transform.position, wallNormal);

        transform.position += (distanceToWall - currentDistance) * wallNormal;

    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Wall")) return;
        
        overlappingWalls.Add(other.gameObject);

        if (other.gameObject != currentSnappedTo)
        {
            canSnapTo = other.gameObject;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Wall")) return;

        overlappingWalls.Remove(other.gameObject);

        if (other.gameObject == canSnapTo)
        {
            canSnapTo = null;
            canSnap = true;
        }
    }
}
