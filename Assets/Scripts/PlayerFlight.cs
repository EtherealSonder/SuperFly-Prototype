using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class PlayerFlight : MonoBehaviour
{


    [SerializeField] private CinemachineImpulseSource boostImpulseSource;

    [Header("Thrust Settings")]
    [SerializeField] private float thrust = 10f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float drag = 1f;

    [Header("Rotation Settings")]
    [SerializeField] private float pitchSpeed = 15f;
    [SerializeField] private float yawSpeed = 10f;
    [SerializeField] private float rollSpeed = 10f;
    [SerializeField] private float resetSpeed = 2f;
    [SerializeField] private float maxPitch = 30f;
    [SerializeField] private float maxRoll = 50f;

    [Header("Lift Settings")]
    [SerializeField] private float launchHeight = 5f;
    [SerializeField] private float launchTime = 0.3f;

    [Header("Boost Settings")]
    [SerializeField] private float boostMultiplier = 1.8f;
    [SerializeField] private float boostedMaxSpeed = 40f;
    [SerializeField] private float boostDrag = 0.5f;

   

    private bool isBoosting = false;
    public bool IsBoosting => isBoosting;
    private bool moveHeld = false;

    [Header("Boost Visuals")]
    [SerializeField] private ParticleSystem speedlineEffect;
    [SerializeField] private InputReader inputReader;


    [Header("Flight Drift Settings")]
    [SerializeField] private float driftMultiplier = 5f;
    [SerializeField] private float driftSpeedThreshold = 1f; // only drift if moving
    [SerializeField] private bool enableRollDrift = true;

    public PlayerAnimation playerAnimation;

    private Rigidbody rb;
    private Vector2 moveInput;
    public Vector2 CurrentInput => moveInput;

    private bool isActive = false;
    public void SetActiveState(bool active)
    {
        isActive = active;
        if (!active && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        inputReader.moveEvent += OnMove;
        rb.useGravity = false;
        rb.linearDamping = drag;

        inputReader.moveEvent += OnMove;
        inputReader.sprintEvent += OnBoostPressed;
        inputReader.sprintCancelledEvent += OnBoostReleased;

        if (speedlineEffect != null)
            speedlineEffect.Stop();
    }

    private void OnDisable()
    {
        inputReader.moveEvent -= OnMove;
        rb.useGravity = true;
        rb.linearDamping = 0f;

        inputReader.moveEvent -= OnMove;
        inputReader.sprintEvent -= OnBoostPressed;
        inputReader.sprintCancelledEvent -= OnBoostReleased;
    }

    private void OnMove(Vector2 input)
    {
        moveInput = input;
        moveHeld = input.magnitude > 0.1f;

        if (!moveHeld && isBoosting)
        {
            isBoosting = false;
            rb.linearDamping = drag;
        }
    }
    private void OnBoostPressed()
    {
        if (moveHeld)
        {
            isBoosting = true;
            rb.linearDamping = boostDrag;

            if (boostFXRoutine != null)
                StopCoroutine(boostFXRoutine);

            boostFXRoutine = StartCoroutine(EnableSpeedlinesWithDelay(0.01f));

            if (boostImpulseSource != null)
            {
                boostImpulseSource.GenerateImpulse();
            }

        }
    }

    private void OnBoostReleased()
    {
        isBoosting = false;
        rb.linearDamping = drag;

        if (speedlineEffect != null && speedlineEffect.isPlaying)
            speedlineEffect.Stop();
    }
    private Coroutine boostFXRoutine;

    private IEnumerator EnableSpeedlinesWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isBoosting && speedlineEffect != null)
        {
            speedlineEffect.Play();
        }
    }
    private void FixedUpdate()
    {
        if (!isActive) return;

        // Forward thrust
        if (moveInput != Vector2.zero)
        {
            float currentThrust = isBoosting ? thrust * boostMultiplier : thrust;
            rb.AddForce(transform.forward * currentThrust, ForceMode.Force);
        }

        // Clamp speed based on boost
        float currentMaxSpeed = isBoosting ? boostedMaxSpeed : maxSpeed;
        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, currentMaxSpeed);

        // Rotation logic
        HandleRotation();

        HandleFlightDrift();
    }

    void HandleFlightDrift() 
    {
        if (enableRollDrift && rb.linearVelocity.magnitude > driftSpeedThreshold)
        {
            float rollAngle = transform.eulerAngles.z;
            if (rollAngle > 180f) rollAngle -= 360f;

            float speedFactor = Mathf.Clamp(rb.linearVelocity.magnitude / maxSpeed, 0f, 1f);
            float driftStrength = -Mathf.Sin(rollAngle * Mathf.Deg2Rad) * driftMultiplier * speedFactor;
            // Add lateral force (relative to the roll angle)
            Vector3 lateralForce = transform.right * driftStrength;

            rb.AddForce(lateralForce, ForceMode.Force);
        }
    }
    private void HandleRotation()
    {
        Vector3 currentRotation = transform.eulerAngles;

        // Convert to -180 to +180 range
        float pitch = (currentRotation.x > 180f) ? currentRotation.x - 360f : currentRotation.x;
        float roll = (currentRotation.z > 180f) ? currentRotation.z - 360f : currentRotation.z;
        float yaw = currentRotation.y;

        // Target rotations
        float targetPitch = moveInput.y * maxPitch;
        float targetRoll = -moveInput.x * maxRoll; // roll left/right
        float yawChange = moveInput.x * yawSpeed * Time.fixedDeltaTime;

        // Interpolation
        pitch = Mathf.Lerp(pitch, targetPitch, Time.fixedDeltaTime * pitchSpeed);
        roll = Mathf.Lerp(roll, targetRoll, Time.fixedDeltaTime * rollSpeed);
        yaw += yawChange;

        // Auto-level roll/yaw when not steering
        if (moveInput.x == 0)
        {
            roll = Mathf.Lerp(roll, 0, Time.fixedDeltaTime * resetSpeed);
            yaw = Mathf.Lerp(yaw, currentRotation.y, Time.fixedDeltaTime * resetSpeed * 2f);
        }

        // Clamp values
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
        roll = Mathf.Clamp(roll, -maxRoll, maxRoll);

        // Apply rotation
        transform.rotation = Quaternion.Euler(pitch, yaw, roll);
    }

    public void TriggerLaunchBoost()
    {
        StartCoroutine(PerformHoverLift());
    }

    private IEnumerator PerformHoverLift()
    {
        
        float elapsed = 0f;

        Vector3 start = transform.position;
        Vector3 end = start + Vector3.up * launchHeight;

        rb.linearVelocity = Vector3.zero; // Cancel any falling
        rb.useGravity = false;

        while (elapsed < launchTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / launchTime;
            rb.MovePosition(Vector3.Lerp(start, end, t));
            yield return null;
        }
    }

    /*void HandleLift() 
    {
        float currentSpeed = rb.linearVelocity.magnitude;
        if (currentSpeed > minLiftSpeed )
        {
            float liftForce = Mathf.Clamp(currentSpeed * liftMultiplier, 0, maxLiftForce);
            rb.AddForce(transform.up * liftForce, ForceMode.Force);
        }

    }*/
}
