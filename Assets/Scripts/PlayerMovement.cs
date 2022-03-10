using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

public class PlayerMovement : MonoBehaviour
{
    // Ground movement
    [Header("Ground Movement")]
    private float moveDirection;    // The current movement direction of the player
    [SerializeField]
    private float moveSpeed;        // The movement speed of the player
    [SerializeField]
    private float acceletarion;     // How fast the player accelerates
    [SerializeField]
    private float jumpForce;        // How far the player jumps
    private bool ableToMove;        // Is the player able to execute ground movement?
    private bool inCutscene;        // Is the player in a cutscene? (Non-controllable)
    private bool stunned;           // Is the player stunned?
    private bool debugMode;         // Is the player in debug mode? (Free movement)
    private GameObject debugInfo;   // The Debug text Info object
    private bool fpsUpdate = true;  // Should the FPS count be updated?
    private float fpsTotal, fpsCount, fpsMin, fpsMax;  // FPS values

    // Grappling hook
    [Header("Grappling hook movement")]
    public List<GameObject> hooks
        = new List<GameObject>();   // The objects that the player is hooked to
    public float maxHooks;         // The maximum amount of hooks that the player can shoot at once
    [SerializeField]
    private float hookSpeed;        // How fast the player moves towards the hook
    [SerializeField]
    private float hookDamp;         // The grappling hooks damping ratio
    [SerializeField]
    private float hookForce;        // The maximum force that the hook is able to pull
    private int hooksFiredInAir;    // How many hooks has the player fired without touching the ground?

    // Components
    private Rigidbody2D rb;         // The physics handler of the player
    private Collider2D cl;          // The collider of the player
    private PlayerHookFire hookF;   // The hook shooter
    private SpriteRenderer rend;    // The player renderer
    private Animator anim;          // The player animator
    // Input system
    private PlayerControl playerControl;

    void Awake()
    {
        // Create a new input system for the player
        playerControl = new PlayerControl();
    }

    void Start()
    {
        // Assign the rigidbody-component
        rb = GetComponent<Rigidbody2D>();
        // Assign the collider component
        cl = GetComponent<Collider2D>();
        // Assign the hook fire component
        hookF = GetComponentInChildren<PlayerHookFire>();
        // Assign the renderer component
        rend = GetComponent<SpriteRenderer>();
        // Assign the animator component
        anim = GetComponent<Animator>();
        // Assign the debug info object
        debugInfo = GameObject.Find("Debug");
        debugInfo.SetActive(false);

        // Assign the jump action to the jump input
        playerControl.Movement.Jump.performed += ctx => Jump();
        // Assign the pull hook action to the pull hook input
        playerControl.GrapplingHook.FireHook.performed += ctx => FireHook();

        // Assign FPS values
        fpsMin = Mathf.Infinity;
        fpsMax = 0;

        // Enable the player's horizontal movement
        ableToMove = true;
    }

    void Update()
    {
        // Check if the player is able to move on the ground
        if (!ableToMove || inCutscene)
        {
            // Flip the player's sprite based on the direction
            rend.flipX = rb.velocity.x < 0;

            if (!ableToMove && hooks.Count == 0)
            {
                ableToMove = true;
            }
        }
        // Update the horizontal movement and jumping gravity of the player
        else if (!debugMode)
        {
            HorizontalMovement();
            JumpGravity();
        }
        else
        {
            DebugMovement();
        }

        // Update the player's animation if it's electrocuted
        anim.SetBool("Electric", stunned);

        // Toggle debug mode
        Keyboard kb = Keyboard.current;
        if (kb.pKey.wasPressedThisFrame && Application.isEditor)
        {
            SetDebug(!debugMode);
        }

        // Debug info
        if (debugInfo.activeInHierarchy)
        {
            // FPS
            if (fpsUpdate)
            {
                float fps = Mathf.Round((1.0f / Time.deltaTime) * 100) / 100;
                
                // Calculate average FPS
                fpsTotal += fps;
                fpsCount++;
                float avgFPS = fpsTotal / fpsCount;
                // Calculate min and max FPS
                if (fps < fpsMin)
                    fpsMin = fps;
                if (fps > fpsMax)
                    fpsMax = fps;
                // Set FPS text
                GameObject fpsText = GameObject.Find("DebugFPS");
                fpsText.GetComponent<Text>().text = "FPS: " + fps + " (Avg: " + avgFPS + ", Min: " + fpsMin + " Max: " + fpsMax + ")";
                // Set FPS text color
                Color fpsColor = new Color(0, 1, 0);
                float avgTo0 = fps / avgFPS * 2;
                if (avgTo0 < 2 && avgTo0 > 1)
                    fpsColor.r = 1 - avgTo0 / 2;
                if (avgTo0 < 1)
                {
                    fpsColor.r = 1;
                    fpsColor.g = avgTo0;
                }
                fpsText.GetComponent<Text>().color = fpsColor;

                StartCoroutine(FPSWaitTime());
            }
            // Velocity
            GameObject.Find("DebugVelocity").GetComponent<Text>().text = "Velocity: " + rb.velocity;
            // Hooks
            GameObject.Find("DebugHooks").GetComponent<Text>().text = "Hooks: " + hooks.Count + " (" + GetComponents<TargetJoint2D>().Length + ")";
        }

        // Post processing effects
        ChromaticAberration chromaticAberrationLayer = null;
        PostProcessVolume v = GameObject.Find("Post Processing").GetComponent<PostProcessVolume>();
        v.profile.TryGetSettings(out chromaticAberrationLayer);
        chromaticAberrationLayer.intensity.value = Mathf.Lerp(chromaticAberrationLayer.intensity.value, (Mathf.Max(rb.velocity.x, rb.velocity.y) / 60) + 0.1f, 0.1f);
        Grain grainLayer = null;
        v.profile.TryGetSettings(out grainLayer);
        grainLayer.intensity.value = stunned.GetHashCode();
    }

    private void FixedUpdate()
    {
        // Check for missing items in the grappling hooks
        for (var i = hooks.Count - 1; i > -1; i--)
        {
            if (hooks[i] == null)
                hooks.RemoveAt(i);
        }
    }

    /// <summary>
    /// Move the player horizontally.
    /// </summary>
    void HorizontalMovement()
    {
        // Read the horizontal movement input and double it for more responsive movement
        float horiInput = playerControl.Movement.Horizontal.ReadValue<float>() * 2;

        // Limit the movement input to -1 and 1
        horiInput = Mathf.Clamp(horiInput, -1, 1);

        // Adjust the movement direction based on the input
        moveDirection = Mathf.Lerp(moveDirection, horiInput, acceletarion / 10);

        // Get the position that the player would be in next frame
        Vector3 futurePos = transform.position + Vector3.right * moveDirection * moveSpeed * Time.deltaTime + Vector3.up * 0.2f;
        int groundMask = LayerMask.GetMask("Ground");

        // Check if there is a wall in the player's position next frame
        if (!Physics2D.BoxCast(futurePos, cl.bounds.size, 0, Vector2.zero, 5, groundMask))
        {
            // Move the player
            transform.Translate(Vector2.right * moveDirection * moveSpeed * Time.deltaTime);
        }

        // Flip the player's sprite based on the direction
        rend.flipX = moveDirection < 0;

        // Update the animation accordingly
        anim.SetFloat("Speed", Mathf.Abs(moveDirection));
    }

    /// <summary>
    /// Move the player around freely. (Debug only)
    /// </summary>
    void DebugMovement()
    {
        Keyboard kb = Keyboard.current;

        Vector2 direction = Vector2.zero;

        // Debug movement
        if (kb.wKey.isPressed)
        {
            direction.y = 1;
        }
        if (kb.aKey.isPressed)
        {
            direction.x = -1;
        }
        if (kb.sKey.isPressed)
        {
            direction.y = -1;
        }
        if (kb.dKey.isPressed)
        {
            direction.x = 1;
        }

        // Debug level loading
        if (kb.gKey.wasPressedThisFrame)
        {
            GameManager.instance.LoadLevelInstantly("Level1");
        }
        if (kb.hKey.wasPressedThisFrame)
        {
            GameManager.instance.LoadLevelInstantly("Level2");
        }
        if (kb.jKey.wasPressedThisFrame)
        {
            GameManager.instance.LoadLevelInstantly("Level3");
        }
        if (kb.kKey.wasPressedThisFrame)
        {
            GameManager.instance.LoadLevelInstantly("Level4");
        }

        // Player movement speed
        float speed = 20;
        if (kb.leftShiftKey.isPressed)
        {
            speed = 40;
        }

        // Move player
        transform.Translate(direction * speed * Time.deltaTime);
    }

    /// <summary>
    /// Fire the player's grappling hook.
    /// </summary>
    void FireHook()
    {
        // Is the player stunned or in a cutscene?
        if (stunned || inCutscene)
        {
            // If yes, don't do anything
            return;
        }

        // Has the player shot the maximum amount of hooks?
        if (hooks.Count >= maxHooks)
        {
            // If yes, pull back the first hook to make room for the new one
            HookBehaviour hook = hooks[0].GetComponent<HookBehaviour>();
            hook.PullBack();

            // Set the player to be able to move
            ableToMove = true;

            // Don't fire a new hook
            return;
        }

        // Is the player on the ground?
        if (IsOnGround())
            // If yes, reset the amount of hooks fired in the air
            hooksFiredInAir = 0;
        else
            // If not, increase the amount of hooks fired in the air
            hooksFiredInAir++;

        // Has the player fired too many hooks in the air?
        if (14 - hooksFiredInAir <= 0)
            // If yes, don't fire a hook
            return;

        hookF.FireHook(14, rb.velocity);

        SoundManager.instance.PlaySound("hookfire");
    }

    /// <summary>
    /// Add extra gravity to the player when falling.
    /// </summary>
    void JumpGravity()
    {
        // Is the player on the ground?
        if (!IsOnGround() && rb.velocity.y < 3f)
        {
            rb.AddForce(Vector2.down * jumpForce / 1.25f);
        }
    }

    /// <summary>
    /// Force the player to jump regardless of any other statuses.
    /// </summary>
    public void ForceJump()
    {
        // Reset the amount of hooks fired midair
        hooksFiredInAir = 0;

        // Is the player hooked to a grappling hook?
        if (!ableToMove && hooks.Count > 0)
        {
            // If yes, pull back all hooks
            PullBackAllHooks();

            // Reduce the player's velocity
            Vector2 speedCap = new Vector2(hookForce, hookForce);
            rb.velocity = ClampVector2(rb.velocity, -speedCap, speedCap);
        }

        // Bounce the player upwards
        rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
        SoundManager.instance.PlaySound("jump");
    }

    /// <summary>
    /// Make the player jump if it's on the ground.
    /// </summary>
    void Jump()
    {
        bool onGround = IsOnGround();

        // Don't do anything if the player is in a cutscene
        if (inCutscene) return;

        // Is the player stunned?
        if (stunned)
        {
            // If yes, do a short useless hop if on the ground
            if (onGround)
            {
                rb.AddForce(new Vector2(0, 3), ForceMode2D.Impulse);
            }
            // Don't do anything else
            return;
        }

        // Is the player on the ground?
        if (onGround)
            // If yes, then reset the amohnt of hooks fired midair
            hooksFiredInAir = 0;

        // Is the player hooked to a grappling hook?
        if (!ableToMove && hooks.Count > 0)
        {
            // If yes, pull back all hooks
            PullBackAllHooks();

            // Is the player moving towards the hook?
            if (rb.velocity.magnitude < 6)
            {
                // If no, set the player to be on the ground so it can jump
                onGround = true;
            }
            else
            {
                //If yes, reduce the player's velocity
                Vector2 speedCap = new Vector2(hookForce, hookForce);
                rb.velocity = ClampVector2(rb.velocity, -speedCap, speedCap);
                SoundManager.instance.PlaySound("whoosh");
            }
        }

        // Is the player on the ground?
        if (onGround)
        {
            // If yes, bounce the player upwards
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
            SoundManager.instance.PlaySound("jump");
        }
    }

    Vector2 ClampVector2(Vector2 value, Vector2 min, Vector2 max)
    {
        value.x = Mathf.Clamp(value.x, min.x, max.x);
        value.y = Mathf.Clamp(value.y, min.y, max.y);
        return value;
    }

    /// <summary>
    /// Check if the player is on the ground.
    /// </summary>
    /// <returns></returns>
    public bool IsOnGround()
    {
        // Determine what counts as "ground"
        LayerMask ground = LayerMask.GetMask("Ground");
        // Get the position of the player's feet
        Vector2 groundCheckPos = new Vector2(transform.position.x, transform.position.y - cl.bounds.extents.y);
        // Check if there is any ground at the player's feet
        return Physics2D.OverlapCircle(groundCheckPos, 0.3f, ground);
    }

    /// <summary>
    /// Pull the player towards a grappling hook.
    /// </summary>
    /// <param name="hook"></param>
    public void PullTowardsHook(GameObject hook)
    {
        // Disable ground movement
        ableToMove = false;

        // Add a target joint to the player that is used to pull it towards the hook
        TargetJoint2D joint = gameObject.AddComponent<TargetJoint2D>();

        // Set the joint's target to the hook
        joint.autoConfigureTarget = false;
        joint.target = hook.transform.position;
        joint.frequency = hookSpeed;
        joint.dampingRatio = hookDamp;
        joint.maxForce = hookForce;

        // Reduce the player's velocity
        rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y / 2.5f);

        // Get the hook's behaviour and assign the joint
        hook.GetComponent<HookBehaviour>().joint = joint;
    }

    /// <summary>
    /// Pulls back all the hooks attached to the player.
    /// </summary>
    public void PullBackAllHooks()
    {
        // Pull back all the attached hooks
        foreach (GameObject h in hooks)
        {
            HookBehaviour beh = h.GetComponent<HookBehaviour>();
            if (h.GetComponent<Rigidbody2D>().gravityScale == 0)
            {
                beh.PullBack();
            }
        }

        // Clear the attached hooks from the list
        hooks.Clear();

        // Destroy any unneccecary joints
        if (GetComponents<TargetJoint2D>().Length != 0)
        {
            TargetJoint2D[] joints = GetComponents<TargetJoint2D>();
            foreach (TargetJoint2D j in joints)
            {
                Destroy(j);
            }
        }

        rb.gravityScale = 1;

        // Set the player to be able to move
        ableToMove = true;
    }

    /// <summary>
    /// Stuns the player for a short while, limiting the player's movement options.
    /// </summary>
    /// <returns></returns>
    public IEnumerator StunPlayer(float duration)
    {
        // Store the normal speed of the player
        float normalSpeed = moveSpeed;

        // Cut the player's movement speed to a quarter
        moveSpeed /= 4;

        // Set the player to be stunned, unable to jump or fire hooks
        stunned = true;

        // Cut the player's velocity to a quarter
        rb.velocity /= 4;

        // Pull back all the hooks attached to the player
        PullBackAllHooks();

        SoundManager.instance.PlaySound("glitch", 0.3f, 3 / duration, false, false);

        // Wait the specifed stun duration
        yield return new WaitForSeconds(duration);

        // Set the player to be unstunned
        stunned = false;

        // Set the move speed back to normal
        moveSpeed = normalSpeed;
    }

    /// <summary>
    /// Is the player stunned?
    /// </summary>
    /// <returns></returns>
    public bool IsStunned()
    {
        return stunned;
    }

    /// <summary>
    /// Adds a specified amount of knockback to the player.
    /// </summary>
    /// <param name="direction"></param>
    public void KnockBack(Vector2 direction, float amount)
    {
        // Add knockback to the player
        rb.AddForce(direction * amount, ForceMode2D.Impulse);
    }

    /// <summary>
    /// Modifies the player's cutscene state.
    /// </summary>
    public void SetCutscene(bool value)
    {
        // Set the player's cutscene state.
        if (value)
        {
            inCutscene = true;
        }
        else
        {
            inCutscene = false;
        }
    }

    /// <summary>
    /// Sets the player debug mode.
    /// </summary>
    /// <param name="value"></param>
    void SetDebug(bool value)
    {
        if (value)
        {
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
            debugInfo.SetActive(true);
        }
        else
        {
            rb.gravityScale = 1;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        rb.velocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = !value;

        debugMode = value;
    }

    public bool IsInCutscene()
    {
        return inCutscene;
    }

    private IEnumerator FPSWaitTime()
    {
        fpsUpdate = false;
        yield return new WaitForSecondsRealtime(0.5f);
        fpsUpdate = true;
    }

    void OnEnable()
    {
        // Enable the control system upon enabling the player
        playerControl.Enable();
    }

    void OnDisable()
    {
        // Disable the control system upon disabling the player
        playerControl.Disable();
    }
}
