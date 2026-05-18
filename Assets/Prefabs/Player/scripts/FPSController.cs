using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    public float speed = 5f;
    public float mouseSensitivity = 2f;
    public bool allowMouseLookY = true;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    public float fallGravityMultiplier = 2f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;
    public GameObject jumpParticlesPrefab;
    public GameObject runParticlesPrefab;
    public AudioClip[] footstepClips;
    public float footstepInterval = 0.4f;
    public Vector2 footstepVolumeRange = new Vector2(0.9f, 1f);
    public AudioClip[] jumpClips;
    public Vector2 jumpVolumeRange = new Vector2(0.95f, 1f);

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;
    private bool isGrounded;
    private bool wasGrounded;
    private bool wasRunning;
    private bool canDoubleJump;
    private float footstepTimer;
    private int lastFootstepIndex = -1;
    private int lastJumpClipIndex = -1;
    private readonly Collider[] groundHits = new Collider[8];

    private Animator animator;
    private AudioSource audioSource;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // --- Rotación con el mouse ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (allowMouseLookY)
        {
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        transform.Rotate(Vector3.up * mouseX);

        // --- Movimiento con teclado ---
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * speed * Time.deltaTime);

        Collider detectedGround = null;
        isGrounded = TryGetGroundCollider(out detectedGround);
        if (isGrounded != wasGrounded)
        {
            if (isGrounded && detectedGround != null)
            {
                Debug.Log($"Toco suelo: {detectedGround.name} | Layer: {LayerMask.LayerToName(detectedGround.gameObject.layer)}");
                canDoubleJump = true;
                animator.SetBool("jumping", false);
                animator.SetBool("doubleJumping", false);
            }
            else
            {
                Debug.Log("Dejo de tocar suelo");
            }

            wasGrounded = isGrounded;
        }

        // --- Animaciones de movimiento ---
        bool isMoving = move.magnitude > 0.1f;
        bool isRunning = isGrounded && isMoving;
        animator.SetBool("running", isRunning);

        if (isRunning && !wasRunning)
        {
            SpawnRunParticles();
            footstepTimer = 0f;
        }

        HandleFootsteps(isRunning);

        wasRunning = isRunning;

        bool jumpPressed = Input.GetKeyDown(KeyCode.Space);
        if (jumpPressed && isGrounded)
        {
            if (velocity.y < 0)
                velocity.y = -2f;

            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetBool("jumping", true);
            animator.SetBool("doubleJumping", false);
            PlayRandomJumpSound();
            SpawnJumpParticles();
        }
        else if (jumpPressed && !isGrounded && canDoubleJump)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            canDoubleJump = false;
            animator.SetBool("jumping", true);
            animator.SetBool("doubleJumping", true);
            PlayRandomJumpSound();
            SpawnJumpParticles();
        }

        // --- Aplicar gravedad ---
        float currentGravity = velocity.y < 0f ? gravity * fallGravityMultiplier : gravity;
        velocity.y += currentGravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // --- Estado Idle ---
        if (!isMoving && !animator.GetBool("jumping"))
        {
            animator.SetBool("running", false);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

    bool TryGetGroundCollider(out Collider detectedGround)
    {
        detectedGround = null;

        if (groundCheck == null)
            return false;

        int hitCount = Physics.OverlapSphereNonAlloc(
            groundCheck.position,
            groundCheckRadius,
            groundHits,
            groundLayer,
            QueryTriggerInteraction.Ignore);

        if (hitCount <= 0)
            return false;

        detectedGround = groundHits[0];
        return true;
    }

    void SpawnJumpParticles()
    {
        SpawnParticlesPrefab(jumpParticlesPrefab);
    }

    void SpawnRunParticles()
    {
        SpawnParticlesPrefab(runParticlesPrefab);
    }

    void SpawnParticlesPrefab(GameObject particlesPrefab)
    {
        if (particlesPrefab == null)
            return;

        Vector3 spawnPosition = groundCheck != null ? groundCheck.position : transform.position;
        GameObject effectInstance = Instantiate(particlesPrefab, spawnPosition, particlesPrefab.transform.rotation);
        CubeParticles particles = effectInstance.GetComponent<CubeParticles>();

        if (particles != null && !particles.spawnOnStart)
        {
            particles.SpawnParticles();
        }
    }

    void HandleFootsteps(bool isRunning)
    {
        if (!isRunning || audioSource == null || footstepClips == null || footstepClips.Length == 0)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= Time.deltaTime;
        if (footstepTimer > 0f)
            return;

        int clipIndex = GetRandomFootstepIndex();
        AudioClip footstepClip = footstepClips[clipIndex];
        if (footstepClip == null)
            return;

        audioSource.PlayOneShot(footstepClip, Random.Range(footstepVolumeRange.x, footstepVolumeRange.y));
        lastFootstepIndex = clipIndex;
        footstepTimer = footstepInterval;
    }

    int GetRandomFootstepIndex()
    {
        if (footstepClips.Length == 1)
            return 0;

        int clipIndex = Random.Range(0, footstepClips.Length);
        while (clipIndex == lastFootstepIndex)
        {
            clipIndex = Random.Range(0, footstepClips.Length);
        }

        return clipIndex;
    }

    void PlayRandomJumpSound()
    {
        if (audioSource == null || jumpClips == null || jumpClips.Length == 0)
            return;

        int clipIndex = GetRandomClipIndex(jumpClips.Length, lastJumpClipIndex);
        AudioClip jumpClip = jumpClips[clipIndex];
        if (jumpClip == null)
            return;

        audioSource.PlayOneShot(jumpClip, Random.Range(jumpVolumeRange.x, jumpVolumeRange.y));
        lastJumpClipIndex = clipIndex;
    }

    int GetRandomClipIndex(int clipCount, int lastIndex)
    {
        if (clipCount <= 1)
            return 0;

        int clipIndex = Random.Range(0, clipCount);
        while (clipIndex == lastIndex)
        {
            clipIndex = Random.Range(0, clipCount);
        }

        return clipIndex;
    }
}