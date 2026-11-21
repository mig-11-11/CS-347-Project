using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovingPlatform : MonoBehaviour
{
    public enum MoveMode
    {
        PingPongLoop,   // Moves back and forth continuously (A -> B -> A)
        OneWayTrigger,  // Waits until triggered (or level start), slides once to B, and stops
        StepSequence    // Moves through multiple waypoints in order
    }

    [Header("Movement Mode")]
    [SerializeField] private MoveMode mode = MoveMode.PingPongLoop;
    [SerializeField] private bool moveOnStart = true;

    [Header("Waypoints (Local Offsets from Start Position)")]
    [SerializeField] private Vector2[] localWaypoints = new Vector2[] { new Vector2(0f, 3f) };

    [Header("Timing & Easing")]
    [SerializeField] private float moveDuration = 1.0f;
    [SerializeField] private float pauseAtWaypoints = 0.25f;
    [Tooltip("Snappy curve: SmoothStep gives a crisp mechanical ease-in/out; Linear is constant speed")]
    [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Rigidbody rb;
    private Vector3[] worldPoints;
    private int currentIndex = 0;
    private int direction = 1;
    private float segmentTimer = 0f;
    private float pauseTimer = 0f;
    private bool isMoving = false;
    private bool hasFinishedOneWay = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

        BuildWorldWaypoints();
    }

    private void Start()
    {
        if (moveOnStart && worldPoints.Length > 1)
        {
            isMoving = true;
        }
    }

    private void BuildWorldWaypoints()
    {
        Vector3 startPos = transform.position;
        startPos.z = 0f;

        if (localWaypoints == null || localWaypoints.Length == 0)
        {
            worldPoints = new Vector3[] { startPos };
            return;
        }

        worldPoints = new Vector3[localWaypoints.Length + 1];
        worldPoints[0] = startPos;

        for (int i = 0; i < localWaypoints.Length; i++)
        {
            worldPoints[i + 1] = new Vector3(
                startPos.x + localWaypoints[i].x,
                startPos.y + localWaypoints[i].y,
                0f
            );
        }
    }

    /// <summary>
    /// Call this from a trigger zone, button, or spell event to start a OneWayTrigger platform.
    /// </summary>
    public void ActivatePlatform()
    {
        if (hasFinishedOneWay || worldPoints.Length <= 1) return;
        isMoving = true;
    }

    private void FixedUpdate()
    {
        if (!isMoving || worldPoints.Length <= 1) return;

        // Handle crisp rhythmic pause at each endpoint
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.fixedDeltaTime;
            return;
        }

        int nextIndex = currentIndex + direction;
        if (nextIndex >= worldPoints.Length || nextIndex < 0)
        {
            AdvanceWaypointIndex();
            return;
        }

        float duration = Mathf.Max(0.05f, moveDuration);
        segmentTimer += Time.fixedDeltaTime;
        float rawT = Mathf.Clamp01(segmentTimer / duration);
        float easedT = easingCurve.Evaluate(rawT);

        Vector3 targetPos = Vector3.LerpUnclamped(worldPoints[currentIndex], worldPoints[nextIndex], easedT);
        rb.MovePosition(targetPos);

        if (rawT >= 1f)
        {
            rb.MovePosition(worldPoints[nextIndex]);
            currentIndex = nextIndex;
            segmentTimer = 0f;
            pauseTimer = pauseAtWaypoints;

            AdvanceWaypointIndex();
        }
    }

    private void AdvanceWaypointIndex()
    {
        if (mode == MoveMode.OneWayTrigger)
        {
            if (currentIndex >= worldPoints.Length - 1)
            {
                isMoving = false;
                hasFinishedOneWay = true;
            }
        }
        else if (mode == MoveMode.PingPongLoop)
        {
            if (currentIndex >= worldPoints.Length - 1)
            {
                direction = -1;
            }
            else if (currentIndex <= 0)
            {
                direction = 1;
            }
        }
        else if (mode == MoveMode.StepSequence)
        {
            if (currentIndex >= worldPoints.Length - 1)
            {
                // Snap/loop back to start or reverse cleanly
                currentIndex = 0;
                rb.MovePosition(worldPoints[0]);
            }
        }
    }

    // Visualize the exact movement path and endpoints in the Scene view
    private void OnDrawGizmos()
    {
        Vector3 basePos = Application.isPlaying && worldPoints != null && worldPoints.Length > 0
            ? worldPoints[0]
            : new Vector3(transform.position.x, transform.position.y, 0f);

        if (localWaypoints == null || localWaypoints.Length == 0) return;

        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.9f);
        Vector3 prev = basePos;

        for (int i = 0; i < localWaypoints.Length; i++)
        {
            Vector3 next = new Vector3(basePos.x + localWaypoints[i].x, basePos.y + localWaypoints[i].y, 0f);
            Gizmos.DrawLine(prev, next);
            Gizmos.DrawWireSphere(next, 0.18f);
            prev = next;
        }
    }
}
