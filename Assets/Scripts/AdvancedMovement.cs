using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
public class AdvancedMovement : MonoBehaviour
{
    [Header("Core")]
    public CharacterController controller;
    public Transform cameraTransform;
    public Transform groundCheck;
    public LayerMask groundMask = ~0;  // Default to all layers if not set
    public LayerMask wallMask = ~0;    // Default to all layers if not set

    [Header("Speeds")]
    public float walkSpeed = 12f;
    public float sprintSpeed = 20f;
    public float slideSpeed = 30f;
    public float dashSpeed = 40f;

    [Header("Jump / Gravity")]
    public float jumpHeight = 3f;
    public float gravity = -25f;
    public float terminalVelocity = -53f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.12f;

    [Header("Air Control")]
    [Range(0f,1f)] public float airControl = 0.6f;
    public float momentumLerp = 12f;

    [Header("Slide")]
    public float slideDuration = 0.9f;
    public float slideCooldown = 0.6f;
    public float crouchHeight = 0.9f;
    public KeyCode slideKey = KeyCode.LeftControl;

    [Header("Wall Run")]
    public float wallRunDistance = 0.9f;
    public float wallRunDuration = 1.2f;
    public float wallRunGravity = -4f;
    public float wallRunUpBoost = 2f;
    public float wallJumpAway = 6f;

    [Header("Dash / BunnyHop")]
    public KeyCode dashKey = KeyCode.E;
    public int dashCharges = 2;
    public float dashDuration = 0.18f;
    public float dashRechargeDelay = 0.6f;
    public float dashRechargeRate = 1f;
    public float bunnyHopSpeedBoost = 1.06f;
    public int maxBunnyChain = 5;

    [Header("Land Roll")]
    public float minFallForRoll = 6f;
    public float rollSpeedBurst = 8f;
    public float rollDuration = 0.45f;
    public Transform camHolder;

    [Header("Dash UI Hook")]
    [Tooltip("0-1, 1 = ready, 0 = on cooldown")]
    public UnityEvent<float> OnDashCooldownChange;

    [Header("Mantle")]
    public bool enableMantle = true;
    [Tooltip("Height (from player's feet) to cast forward to detect a ledge")]
    public float mantleCheckHeight = 1.0f;
    [Tooltip("Forward distance to check for a reachable lip")]
    public float mantleForwardDistance = 1.0f;
    [Tooltip("Maximum climbable vertical distance")]
    public float mantleMaxHeight = 1.5f;
    [Tooltip("How long the mantle motion takes")]
    public float mantleDuration = 0.25f;
    [Tooltip("Optional mask for surfaces you can mantle onto. If left empty, groundMask is used.")]
    public LayerMask mantleMask;

    // internal state
    Vector3 velocity;
    Vector3 horizontalVelocity;
    bool isGrounded;
    float coyoteCounter;
    float jumpBufferCounter;

    Vector3 prevHorizontalVelocity;

    // slide
    bool isSliding;
    float slideTimer;
    float slideCooldownTimer;
    float originalControllerHeight;
    Vector3 originalCenter;

    // wallrun
    bool isWallRunning;
    float wallRunTimer;
    Vector3 wallNormal;

    // dash
    int currentDashCharges;
    bool isDashing;
    float dashCooldownTimer;
    public float dashCooldown = 1f;
    [HideInInspector] public float[] dashChargeTimers;

    // mantle
    bool isMantling;

    // bunny hop
    int bunnyChain;
    float lastJumpTime;

    // roll
    bool isRolling;

    // camera shake
    Coroutine shakeCoroutine;

    bool wasGrounded = true;
    float prevVerticalVelocity = 0f;

    void Awake()
    {
        // Get required components
        if (controller == null) controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError($"[{gameObject.name}] AdvancedMovement: Missing CharacterController component!");
            enabled = false;
            return;
        }

        // Cache original controller dimensions
        originalControllerHeight = controller.height;
        originalCenter = controller.center;

        // Validate required references
        if (cameraTransform == null)
            Debug.LogWarning($"[{gameObject.name}] AdvancedMovement: Missing camera transform reference!");
        if (groundCheck == null)
            Debug.LogWarning($"[{gameObject.name}] AdvancedMovement: Missing ground check transform!");
        if (camHolder == null)
            Debug.LogWarning($"[{gameObject.name}] AdvancedMovement: Missing camera holder transform!");

        // Initialize dash system
        currentDashCharges = dashCharges;
        dashChargeTimers = new float[dashCharges];
        for (int i = 0; i < dashCharges; i++) 
            dashChargeTimers[i] = 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return; // Avoid division by zero in physics calculations

        prevHorizontalVelocity = horizontalVelocity;
        prevVerticalVelocity = velocity.y;

        // Ground check (defensive: if groundCheck not assigned, use transform.position - half height)
        Vector3 groundCheckPos = (groundCheck != null) ? groundCheck.position : (transform.position + Vector3.up * (-originalControllerHeight * 0.5f + 0.1f));
        // Include both ground and wall layers in ground check to treat tops of walls as ground
        LayerMask combinedGroundMask = groundMask | wallMask;
        isGrounded = Physics.CheckSphere(groundCheckPos, 0.15f, combinedGroundMask);
        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            // preserve original behavior: immediate refill while grounded
            dashCooldownTimer = 0f;
            if (currentDashCharges < dashCharges)
                currentDashCharges = dashCharges;
        }
        else
            coyoteCounter -= dt;

        // ---------------- Wall Run Check ----------------
        if (!isGrounded && !isWallRunning && velocity.y < 0f && CheckForWall(out Vector3 wallHitNormal))
        {
            isWallRunning = true;
            wallNormal = wallHitNormal;
            wallRunTimer = wallRunDuration;

            // determine which side wall is on relative to player and tilt away from it
            float side = Vector3.Dot(transform.right, wallNormal);
            // When wall is on right (side > 0), tilt right (-15°)
            // When wall is on left (side < 0), tilt left (+15°)
            float tiltAngle = (side > 0f) ? -15f : 15f;

            if (camHolder != null) StartCoroutine(CameraTilt(tiltAngle, 0.15f));
            MovementEvents.OnWallRunStart?.Invoke();
        }


        // Wall run duration handling
        if (isWallRunning)
        {
            wallRunTimer -= dt;
            if (wallRunTimer <= 0f || isGrounded)
                StopWallRun();
        }

        // Jump buffering
        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= dt;

        // --- Mantle attempt (try before regular airborne jump logic triggers) ---
        if (enableMantle && Input.GetButtonDown("Jump") && !isGrounded && !isWallRunning && !isDashing && !isSliding && !isMantling)
        {
            if (CheckForMantle(out Vector3 mantleTarget))
            {
                MovementEvents.OnMantle?.Invoke();
                StartCoroutine(DoMantle(mantleTarget));
                jumpBufferCounter = 0f; // consume the jump input so we don't double-jump
            }
        }

        // Input
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector3 inputDir = (transform.right * input.x + transform.forward * input.y);
        float inputMag = Mathf.Clamp01(inputDir.magnitude);
        Vector3 inputDirNorm = inputMag > 0.001f ? inputDir.normalized : Vector3.zero;

        // Slide
        bool slideRequested = Input.GetKeyDown(slideKey) && Input.GetKey(KeyCode.W);
        if (slideRequested && isGrounded && slideCooldownTimer <= 0f && !isSliding)
        {
            StartSlide();
            MovementEvents.OnSlideStart?.Invoke();
        }
        if (isSliding)
        {
            slideTimer -= dt;
            if (slideTimer <= 0f) EndSlide();
        }
        else
        {
            if (slideCooldownTimer > 0f) slideCooldownTimer -= dt;
        }

        // Sprint
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && inputMag > 0.1f && isGrounded && !isSliding;
        if (isSprinting) MovementEvents.OnStartSprint?.Invoke();
        else MovementEvents.OnStopSprint?.Invoke();

        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
        if (isSliding) targetSpeed = slideSpeed;

        // Dash input: require not currently dashing, cooldown satisfied and grounded
        if (Input.GetKeyDown(dashKey) && !isDashing && dashCooldownTimer <= 0f && isGrounded)
        {
            StartCoroutine(DoDash(inputDirNorm));
            dashCooldownTimer = dashCooldown;
        }

        // Horizontal movement
        Vector3 desiredHoriz = inputDirNorm * targetSpeed;
        if (isWallRunning)
        {
            Vector3 forwardAlongWall = Vector3.Cross(wallNormal, Vector3.up).normalized;
            if (Vector3.Dot(forwardAlongWall, transform.forward) < 0f) forwardAlongWall *= -1f;
            desiredHoriz = forwardAlongWall * targetSpeed;
            desiredHoriz += transform.right * input.x * (targetSpeed * 0.5f);
        }
        if (isGrounded && !isRolling)
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, desiredHoriz, Mathf.Clamp01(momentumLerp * dt));
        else
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, desiredHoriz, airControl * dt);

        // Jump
        bool doJump = false;
        if (jumpBufferCounter > 0f)
            if (isGrounded || coyoteCounter > 0f || isWallRunning)
                doJump = true;

        if (doJump)
        {
            jumpBufferCounter = 0f;
            if (isWallRunning)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity) + wallRunUpBoost;
                horizontalVelocity += wallNormal * wallJumpAway;
                StopWallRun();
            }
            else
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                float timeSinceLastJump = Time.time - lastJumpTime;
                if (timeSinceLastJump <= 0.35f)
                {
                    bunnyChain = Mathf.Clamp(bunnyChain + 1, 1, maxBunnyChain);
                    horizontalVelocity *= Mathf.Pow(bunnyHopSpeedBoost, bunnyChain);
                }
                else bunnyChain = 0;
                lastJumpTime = Time.time;
            }
        }

        // Gravity
        if (isWallRunning)
        {
            // Smoothly approach a modest downward velocity instead of accumulating an ever-growing downwards speed.
            // Treat wallRunGravity as the target fall speed while wallrunning (e.g. -4).
            velocity.y = Mathf.Lerp(velocity.y, wallRunGravity, 6f * dt);
        }
        else if (isDashing)
            velocity.y += gravity * 0.55f * dt;
        else
            velocity.y += gravity * dt;

        if (velocity.y < terminalVelocity) velocity.y = terminalVelocity;

        // Landing roll
        if (!wasGrounded && isGrounded)
        {
            if (prevVerticalVelocity < -minFallForRoll)
                StartCoroutine(DoLandRoll());
        }

        // Apply movement
        Vector3 totalMove = horizontalVelocity + Vector3.up * velocity.y;
        // Do not call CharacterController.Move if the controller is disabled or we are mantling
        if (!isMantling && controller != null && controller.enabled)
        {
            controller.Move(totalMove * dt);
        }

        // Ceiling collision
        CollisionFlags flags = controller.collisionFlags;
        if ((flags & CollisionFlags.Above) != 0)
            velocity.y = Mathf.Min(0f, velocity.y);

        // Reset vertical velocity when grounded (including tops of walls)
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
            // Also reset wall run state if we're grounded but not actively wall running
            if (isWallRunning && !CheckForWall(out _))
            {
                StopWallRun();
            }
        }

        // Stop wallrun if grounded
        if (isWallRunning && isGrounded) StopWallRun();

        // Restore controller height if not sliding
        if (!isSliding && controller.height != originalControllerHeight)
        {
            controller.height = Mathf.Lerp(controller.height, originalControllerHeight, 8f * dt);
            controller.center = Vector3.Lerp(controller.center, originalCenter, 8f * dt);
        }

        // ---------------- Dash cooldown timer & UI update ----------------
        // Decrement the global dash cooldown timer (fixed to actually count down)
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= dt;
            if (dashCooldownTimer < 0f) dashCooldownTimer = 0f;
        }

        // Update dash charge timers and current charge count
        UpdateDashCharges(dt);

        // update wasGrounded for next frame's landing checks
        wasGrounded = isGrounded;
    }

    // ------------------ Slide ------------------
    void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        slideCooldownTimer = slideCooldown;
        controller.height = crouchHeight;
        controller.center = new Vector3(originalCenter.x, crouchHeight / 2f, originalCenter.z);

        Vector3 dir = prevHorizontalVelocity.sqrMagnitude > 0.01f ? prevHorizontalVelocity.normalized : transform.forward;
        horizontalVelocity += dir * slideSpeed * 0.8f;

        if (camHolder != null) StartCoroutine(CameraTiltPitch(-10f, 0.18f));
    }

    void EndSlide()
    {
        isSliding = false;
        if (camHolder != null) StartCoroutine(CameraTiltPitch(0f, 0.2f));
    }

    // ------------------ Dash ------------------
    IEnumerator DoDash(Vector3 inputDir)
    {
        if (isDashing) yield break;

        // mark as dashing to prevent overlapping dashes
        isDashing = true;

        // Consume first available charge
        for (int i = 0; i < dashCharges; i++)
        {
            if (dashChargeTimers[i] <= 0f)
            {
                dashChargeTimers[i] = dashRechargeDelay;
                break;
            }
        }


        currentDashCharges = Mathf.Max(0, currentDashCharges - 1);
        float t = 0f;
        Vector3 dashDir = inputDir.sqrMagnitude > 0.01f ? inputDir.normalized : transform.forward;
        dashDir = Vector3.ProjectOnPlane(dashDir, Vector3.up).normalized;

        MovementEvents.OnDash?.Invoke();
        float savedY = velocity.y;

        if (camHolder != null)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(CameraShake(0.08f, 0.12f));
        }

        while (t < dashDuration)
        {
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, dashDir * dashSpeed, 20f * Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }

        velocity.y = savedY;
        isDashing = false;
    }

    // ------------------ Landing Roll ------------------
    IEnumerator DoLandRoll()
    {
        if (isRolling) yield break;
        isRolling = true;

        if (camHolder != null) StartCoroutine(CameraShake(0.12f, 0.18f));

        Vector3 dir = prevHorizontalVelocity.sqrMagnitude > 0.01f ? prevHorizontalVelocity.normalized : transform.forward;
        horizontalVelocity += dir * rollSpeedBurst;

        float t = 0f;
        while (t < rollDuration)
        {
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, dir * (walkSpeed + rollSpeedBurst), 2f * Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }

        isRolling = false;
    }

    IEnumerator CameraShake(float magnitude, float time)
    {
        if (camHolder == null) yield break;
        Vector3 originalPos = camHolder.localPosition;
        float elapsed = 0f;
        while (elapsed < time)
        {
            float x = (Random.value * 2f - 1f) * magnitude;
            float y = (Random.value * 2f - 1f) * magnitude;
            camHolder.localPosition = originalPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        camHolder.localPosition = originalPos;
    }

    // Used for wall-running camera roll
    IEnumerator CameraTilt(float targetAngle, float dur)
    {
        if (camHolder == null) yield break;
        float elapsed = 0f;
        Quaternion start = camHolder.localRotation;
        // Z-axis rotation for roll effect (wall running)
        Quaternion end = Quaternion.Euler(0f, 0f, targetAngle);
        while (elapsed < dur)
        {
            camHolder.localRotation = Quaternion.Slerp(start, end, elapsed / dur);
            elapsed += Time.deltaTime;
            yield return null;
        }
        camHolder.localRotation = end;
    }

    // Used for sliding camera pitch, preserves current roll and yaw
    IEnumerator CameraTiltPitch(float targetPitch, float dur)
    {
        if (camHolder == null) yield break;
        float elapsed = 0f;
        Quaternion start = camHolder.localRotation;
        
        // Extract current rotation
        Vector3 currentAngles = start.eulerAngles;
        // Only change X (pitch), preserve Y (yaw) and Z (roll)
        Quaternion end = Quaternion.Euler(targetPitch, currentAngles.y, currentAngles.z);
        
        while (elapsed < dur)
        {
            camHolder.localRotation = Quaternion.Slerp(start, end, elapsed / dur);
            elapsed += Time.deltaTime;
            yield return null;
        }
        camHolder.localRotation = end;
    }

    void StopWallRun()
    {
        if (!isWallRunning) return;
        isWallRunning = false;

        // return camera upright
        if (camHolder != null) StartCoroutine(CameraTilt(0f, 0.15f));
        MovementEvents.OnWallRunStop?.Invoke();

        // --- fix: smooth gravity transition after wall run ---
        // clamp extreme downward velocity so you don’t instantly slam down
        if (velocity.y < -5f)
            velocity.y = -1f;

        // smoothly restore gravity strength
        StartCoroutine(RestoreGravitySmoothly());
    }

    IEnumerator RestoreGravitySmoothly()
    {
        float elapsed = 0f;
        float duration = 0.25f; // blend time
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            velocity.y += gravity * (elapsed / duration) * Time.deltaTime;
            yield return null;
        }
    }



    bool CheckForWall(out Vector3 wallNormal)
    {
        RaycastHit hit;
        Vector3[] directions = { transform.right, -transform.right };

        // start ray a bit above feet to better detect walls at chest/hip height
        Vector3 rayOrigin = transform.position + Vector3.up * (originalControllerHeight * 0.25f);

        foreach (Vector3 dir in directions)
        {
            if (Physics.Raycast(rayOrigin, dir, out hit, wallRunDistance, wallMask))
            {
                // Only consider it a wall if it's steep enough (close to vertical)
                // This helps distinguish between walls we can run on vs surfaces we should stand on
                float angleFromUp = Vector3.Angle(hit.normal, Vector3.up);
                if (angleFromUp > 65f) // Anything more than 65 degrees from up is considered a wall
                {
                    wallNormal = hit.normal;
                    return true;
                }
            }
        }

        wallNormal = Vector3.zero;
        return false;
    }

    // ------------------ Mantle detection & action ------------------
    // Returns true and provides a world position (character center) when mantle is possible.
    bool CheckForMantle(out Vector3 mantleCenter)
    {
        mantleCenter = Vector3.zero;
        LayerMask topMask = (mantleMask.value != 0) ? mantleMask : groundMask;

        // origin slightly above player's feet (use groundCheck if available). Use a defensive fallback if neither exist.
        Vector3 originBase = (groundCheck != null) ? groundCheck.position : (transform.position + Vector3.up * (-originalControllerHeight * 0.5f + 0.2f));
        Vector3 origin = originBase + Vector3.up * mantleCheckHeight;

        float sphereRadius = Mathf.Max(controller.radius * 0.9f, 0.1f);

        // use a spherecast forward to detect a potential lip/edge
        if (!Physics.SphereCast(origin, sphereRadius, transform.forward, out RaycastHit frontHit, mantleForwardDistance, topMask, QueryTriggerInteraction.Ignore))
            return false;

        // cast down from above the hit point to find the top surface
        Vector3 topRayOrigin = frontHit.point + Vector3.up * (mantleMaxHeight + 0.1f);
        if (!Physics.Raycast(topRayOrigin, Vector3.down, out RaycastHit topHit, mantleMaxHeight + 0.2f, topMask, QueryTriggerInteraction.Ignore))
            return false;

        // require reasonably horizontal surface
        if (Vector3.Dot(topHit.normal, Vector3.up) < 0.7f) return false;

        // measure vertical distance from player's feet (use groundCheck if available)
        // measure vertical distance from player's feet (use groundCheck if available)
        float feetY;
        if (groundCheck != null)
            feetY = groundCheck.position.y;
        else
            // transform.position is capsule center; subtract half the standing height to get feet Y
            feetY = transform.position.y - (originalControllerHeight * 0.5f);


        // compute target center position so CharacterController sits on top of the surface
        // use the originalControllerHeight (the standing capsule) so that temporary
        // changes (like crouch/slide) don't make the clearance checks place the
        // final target too low and cause the capsule to end up intersecting geometry.
        float centerY = topHit.point.y + (originalControllerHeight * 0.5f) + 0.05f;
        // move slightly back from the exact hit point along surface plane to avoid placing inside geometry
        Vector3 forwardOnPlane = Vector3.ProjectOnPlane(transform.forward, topHit.normal).normalized;
        Vector3 targetCenter = topHit.point - forwardOnPlane * Mathf.Clamp(controller.radius * 0.5f, 0.1f, 0.5f);
        targetCenter.y = centerY;

        // clearance check: ensure no blocking colliders at the final capsule (ignore the topHit.collider)
        // Use the original controller height and center for the overlap capsule check
        // to avoid false positives/negatives when player is crouched/sliding.
        Vector3 capBottom = targetCenter + Vector3.up * (-originalControllerHeight * 0.5f + 0.1f);
        Vector3 capTop = targetCenter + Vector3.up * (originalControllerHeight * 0.5f - 0.1f);
        float capRadius = Mathf.Max(controller.radius, 0.1f);
        Collider[] hits = Physics.OverlapCapsule(capBottom, capTop, capRadius, topMask, QueryTriggerInteraction.Ignore);
        foreach (var col in hits)
        {
            if (col == topHit.collider) continue;
            // blocked by something else
            return false;
        }

        mantleCenter = targetCenter;
        return true;
    }

    IEnumerator DoMantle(Vector3 targetCenter)
    {
        if (isMantling) yield break;
        isMantling = true;

        // temporarily disable controller to move transform freely
        controller.enabled = false;

        Vector3 start = transform.position;
    // place the transform such that CharacterController capsule center will match targetCenter after re-enabling
    // use the originalCenter (standing center) rather than the possibly modified controller.center
    // controller.center is a local-space offset; convert to world-space offset and subtract it from the desired center
    Vector3 localCenter = (originalCenter == Vector3.zero) ? controller.center : originalCenter;
    Vector3 worldCenterOffset = transform.rotation * localCenter;
    Vector3 end = targetCenter - worldCenterOffset;
        float elapsed = 0f;

        while (elapsed < mantleDuration)
        {
            transform.position = Vector3.Lerp(start, end, elapsed / mantleDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = end;

        // reset movement state
        velocity = Vector3.zero;
        horizontalVelocity = Vector3.zero;

        // re-enable controller and allow physics/grounding to settle
        controller.enabled = true;

        // wait a frame so ground checks update naturally
        yield return null;

        // when mantling finishes, ensure vertical velocity is small/downward to avoid gravity spikes
        // and ensure the grounded flag is consistent for the next Update
        if (isGrounded)
        {
            velocity.y = -2f;
        }

        isMantling = false;
    }

    // Consolidated helper to keep dash timers and charges updated
    private void UpdateDashCharges(float dt)
    {
        // Update cooldown timers for each dash charge
        for (int i = 0; i < dashCharges; i++)
        {
            if (dashChargeTimers[i] > 0f)
            {
                dashChargeTimers[i] -= dt;
                if (dashChargeTimers[i] < 0f) dashChargeTimers[i] = 0f;
            }
        }

        // Update current charges (preserve existing behavior: start full then decrement for active timers)
        currentDashCharges = dashCharges;
        for (int i = 0; i < dashCharges; i++)
            if (dashChargeTimers[i] > 0f)
                currentDashCharges--;
    }


    // Optional events for hooking particles/effects
    public static class MovementEvents
    {
        public static UnityAction OnSlideStart;
        public static UnityAction OnStartSprint;
        public static UnityAction OnStopSprint;
        public static UnityAction OnDash;
        public static UnityAction OnLandRoll;
        public static UnityAction OnWallRunStart;
        public static UnityAction OnWallRunStop;
        public static UnityAction OnMantle;
    }
}
